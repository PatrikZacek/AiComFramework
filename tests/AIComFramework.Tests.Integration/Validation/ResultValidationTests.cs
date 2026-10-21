using System.Text.Json;
using AIComFramework.Abstractions.Results;
using AIComFramework.Abstractions.Validation;
using AIComFramework.Core.Conversations;
using AIComFramework.Core.DependencyInjection;
using AIComFramework.Core.Trust;
using AIComFramework.Core.Validation;
using AIComFramework.Tests.Shared.Fixtures;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace AIComFramework.Tests.Integration.Validation;

/// <summary>Exercises real SDK parsing, domain checks, DI scopes and conversation boundaries.</summary>
public sealed class ResultValidationTests
{
    private static readonly AiResultContract<InvoiceResult> Contract = new AiResultContract<InvoiceResult>()
        .ValidateWith<InvoiceAmountValidator>().ValidateWith<InvoiceCurrencyValidator>();

    private static ServiceProvider Services()
    {
        var services = new ServiceCollection();
        services.AddAIComFramework()
            .AddAiResultValidator<InvoiceResult, InvoiceAmountValidator>()
            .AddAiResultValidator<InvoiceResult, InvoiceCurrencyValidator>()
            .AddAiResultValidator<InvoiceResult, CallbackValidator>();
        services.AddSingleton<InvoiceLedger>();
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private static TestChatClient Client(string json = """{"Id":"invoice-1","Total":120,"Currency":"EUR"}""") =>
        new() { Respond = (_, _, _) => Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, json))) };

    /// <summary>Unexpected serializer code failures are not hidden as malformed model output.</summary>
    [Fact]
    public async Task ConverterProgrammingFailurePropagates()
    {
        var converter = new ThrowingInvoiceConverter();
        var options = new JsonSerializerOptions(JsonSerializerOptions.Default);
        options.Converters.Add(converter);
        Assert.Same(converter.Failure, await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new SupportContext().Compose(Client("""{"data":{"Id":"invoice-1","Total":120,"Currency":"EUR"}}""")).Prompt("extract").Expect<InvoiceResult>()
                .SendAsync(serializerOptions: options, useJsonSchema: false)));
    }

    /// <summary>An indeterminate finding does not stop later checks; selection order changes only diagnostics order.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AggregationIsIndependentOfSelectionOrder(bool reverse)
    {
        using var provider = Services();
        using var scope = provider.CreateScope();
        var contract = reverse
            ? new AiResultContract<InvoiceResult>().ValidateWith<InvoiceCurrencyValidator>().ValidateWith<InvoiceAmountValidator>()
            : Contract;
        var result = await new SupportContext().Compose(Client("""{"Id":"missing","Total":120,"Currency":"USD"}"""))
            .Prompt("extract").Expect(contract, scope.ServiceProvider.GetRequiredService<AiValidationPipeline>()).SendAsync();
        Assert.Equal(AiValidationStatus.Invalid, result.Validation.Status);
        Assert.Equal(reverse ? new[] { "currency", "amount" } : new[] { "amount", "currency" },
            result.Validation.Findings.Select(item => item.Finding.Code));
    }

    /// <summary>Typed parsing alone is explicitly unvalidated and cannot use the guarded accessor.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TypedWithoutValidatorsIsNotValidated(bool schema)
    {
        var result = await new SupportContext().Compose(Client()).Prompt("extract").Expect<InvoiceResult>().SendAsync(useJsonSchema: schema);
        Assert.True(result.HasResult);
        Assert.False(result.ValidationRequested);
        Assert.Null(result.StructuralError);
        Assert.Equal(120m, result.Candidate!.Total);
        Assert.Equal(AiValidationStatus.NotValidated, result.Validation.Status);
        Assert.Throws<InvalidOperationException>(() => result.GetValidatedValue());
    }

    /// <summary>Authoritative evidence and independent checks aggregate deterministically without retries.</summary>
    [Theory]
    [InlineData("invoice-1", 120, "EUR", AiValidationStatus.Valid)]
    [InlineData("invoice-1", 121, "EUR", AiValidationStatus.Invalid)]
    [InlineData("missing", 120, "EUR", AiValidationStatus.Unverifiable)]
    [InlineData("missing", 120, "USD", AiValidationStatus.Invalid)]
    [InlineData("invoice-1", 121, "USD", AiValidationStatus.Invalid)]
    public async Task RequiredChecksAggregate(string id, int total, string currency, AiValidationStatus expected)
    {
        using var provider = Services();
        using var scope = provider.CreateScope();
        var client = Client(JsonSerializer.Serialize(new InvoiceResult { Id = id, Total = total, Currency = currency }));
        var pipeline = scope.ServiceProvider.GetRequiredService<AiValidationPipeline>();
        var result = await new SupportContext().Compose(client).Prompt("extract")
            .Expect<InvoiceResult>(pipeline: pipeline).ValidateWith<InvoiceAmountValidator>()
            .ValidateWith<InvoiceCurrencyValidator>().SendAsync();
        Assert.True(result.HasResult);
        Assert.True(result.ValidationRequested);
        Assert.Equal(expected, result.Validation.Status);
        Assert.Equal(new[] { "amount", "currency" }, result.Validation.Findings.Select(item => item.Finding.Code));
        Assert.All(result.Validation.Findings, item => Assert.NotEmpty(item.Validator));
        Assert.Single(client.Requests);
        if (expected == AiValidationStatus.Valid) Assert.Same(result.Candidate, result.GetValidatedValue());
        else Assert.Throws<InvalidOperationException>(() => result.GetValidatedValue());
    }

    /// <summary>Parsing errors and required-member failures stay separate from domain findings.</summary>
    [Theory]
    [InlineData("not JSON")]
    [InlineData("{")]
    [InlineData("{}")]
    [InlineData("""{"Id":"invoice-1","Total":"wrong","Currency":"EUR"}""")]
    [InlineData("null")]
    public async Task StructuralFailureSkipsValidators(string json)
    {
        using var provider = Services();
        using var scope = provider.CreateScope();
        var validator = scope.ServiceProvider.GetRequiredService<CallbackValidator>();
        validator.Callback = (_, _, _) => throw new InvalidOperationException("Must not execute.");
        var pipeline = scope.ServiceProvider.GetRequiredService<AiValidationPipeline>();
        var result = await new SupportContext().Compose(Client(json)).Prompt("extract")
            .Expect<InvoiceResult>(pipeline: pipeline).ValidateWith<CallbackValidator>().SendAsync();
        Assert.False(result.HasResult);
        Assert.NotNull(result.StructuralError);
        Assert.Equal(json, result.RawText);
        Assert.Equal(AiValidationStatus.Unverifiable, result.Validation.Status);
        Assert.Empty(result.Validation.Findings);
        Assert.Throws<InvalidOperationException>(() => result.GetValidatedValue());
    }

    /// <summary>Metadata is captured and reference data is explicit; cancellation reaches validators.</summary>
    [Fact]
    public async Task MetadataAndCancellationAreExplicit()
    {
        using var provider = Services();
        using var scope = provider.CreateScope();
        using var cancellation = new CancellationTokenSource();
        var context = new SupportContext();
        var contract = new AiResultContract<InvoiceResult>().ValidateWith<CallbackValidator>().WithReference("record", "invoice-1");
        var calls = 0;
        scope.ServiceProvider.GetRequiredService<CallbackValidator>().Callback = (_, metadata, token) =>
        {
            calls++;
            Assert.Equal(cancellation.Token, token);
            Assert.Equal(context.DomainId, metadata.Configuration.DomainId);
            Assert.Equal("invoice-1", metadata.ReferenceData["record"]);
            Assert.NotEqual(Guid.Empty, metadata.CorrelationId);
            return ValueTask.FromResult(new AiValidationFinding(AiValidationStatus.Valid, "metadata", "Checked."));
        };
        await context.Compose(Client()).Prompt("private prompt").Expect(contract, scope.ServiceProvider.GetRequiredService<AiValidationPipeline>())
            .SendAsync(cancellation.Token);
        Assert.Equal(1, calls);
        Assert.DoesNotContain(typeof(AiValidationContext).GetProperties(), property => property.Name is "Messages" or "History");
    }

    /// <summary>Operational failures preserve exception identity and never become domain-invalid responses.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task OperationalFailuresPropagate(bool providerFailure)
    {
        using var provider = Services();
        using var scope = provider.CreateScope();
        var expected = new InvalidOperationException("dependency offline");
        var client = Client();
        if (providerFailure) client.Respond = (_, _, _) => throw expected;
        else scope.ServiceProvider.GetRequiredService<CallbackValidator>().Callback = (_, _, _) => throw expected;
        var conversation = new AiConversationFactory(client, new JsonUntrustedRenderer()).Create(new SupportContext());
        Assert.Same(expected, await Assert.ThrowsAsync<InvalidOperationException>(() => conversation.SendValidatedAsync(
            "extract", new AiResultContract<InvoiceResult>().ValidateWith<CallbackValidator>(),
            scope.ServiceProvider.GetRequiredService<AiValidationPipeline>())));
        Assert.Empty(conversation.GetState().Turns);
        conversation.Configure(config => config.ClearRole());
        Assert.Single(client.Requests);
    }

    /// <summary>Pre-cancellation, late provider cancellation and late validator cancellation prevent commits.</summary>
    [Theory]
    [InlineData("before")]
    [InlineData("provider")]
    [InlineData("validator")]
    public async Task CancellationDoesNotCommit(string phase)
    {
        using var provider = Services();
        using var scope = provider.CreateScope();
        using var cancellation = new CancellationTokenSource();
        var client = Client();
        if (phase == "before") cancellation.Cancel();
        if (phase == "provider") client.Respond = (_, _, _) =>
        {
            cancellation.Cancel();
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, """{"Id":"invoice-1","Total":120,"Currency":"EUR"}""")));
        };
        if (phase == "validator") scope.ServiceProvider.GetRequiredService<CallbackValidator>().Callback = (_, _, _) =>
        {
            cancellation.Cancel();
            return ValueTask.FromResult(new AiValidationFinding(AiValidationStatus.Valid, "late", "Late return."));
        };
        var conversation = new AiConversationFactory(client, new JsonUntrustedRenderer()).Create(new SupportContext());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => conversation.SendValidatedAsync("extract",
            new AiResultContract<InvoiceResult>().ValidateWith<CallbackValidator>(),
            scope.ServiceProvider.GetRequiredService<AiValidationPipeline>(), cancellation.Token));
        Assert.Empty(conversation.GetState().Turns);
        Assert.Equal(phase == "before" ? 0 : 1, client.Requests.Count);
    }

    /// <summary>Only accepted results commit; rejected output cannot enter future history.</summary>
    [Theory]
    [InlineData("invoice-1", 120, AiValidationStatus.Valid, 1)]
    [InlineData("invoice-1", 999, AiValidationStatus.Invalid, 0)]
    [InlineData("missing", 120, AiValidationStatus.Unverifiable, 0)]
    public async Task ConversationCommitPolicy(string id, int total, AiValidationStatus status, int count)
    {
        using var provider = Services();
        using var scope = provider.CreateScope();
        var client = Client(JsonSerializer.Serialize(new InvoiceResult { Id = id, Total = total, Currency = "EUR" }));
        var conversation = new AiConversationFactory(client, new JsonUntrustedRenderer()).Create(new SupportContext());
        var result = await conversation.SendValidatedAsync("extract", Contract, scope.ServiceProvider.GetRequiredService<AiValidationPipeline>());
        Assert.Equal(status, result.Validation.Status);
        Assert.Equal(count, conversation.GetState().Turns.Length);
        Assert.Equal(count, conversation.Preview("next").TransmittedTurnIds.Length);
        if (count == 1) Assert.Equal(result.Validation, conversation.GetState().Turns[0].Validation);
        else await Assert.ThrowsAsync<ArgumentException>(() => conversation.SendAsync("share", history: new HistoryOptions(sharedTurnIds: [1])));
    }

    /// <summary>Legacy and explicit unvalidated conversations record NotValidated; structural failures never commit.</summary>
    [Fact]
    public async Task UnvalidatedAndMalformedConversationOutcomes()
    {
        var client = Client();
        var conversation = new AiConversationFactory(client, new JsonUntrustedRenderer()).Create(new SupportContext());
        await conversation.SendAsync("ordinary");
        await conversation.SendValidatedAsync<InvoiceResult>("typed");
        Assert.All(conversation.GetState().Turns, turn => Assert.Equal(AiValidationStatus.NotValidated, turn.Validation.Status));
        client.Respond = (_, _, _) => Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "{}")));
        var result = await conversation.SendValidatedAsync<InvoiceResult>("malformed");
        Assert.False(result.HasResult);
        Assert.Equal(AiValidationStatus.NotValidated, result.Validation.Status);
        Assert.Equal(2, conversation.GetState().Turns.Length);
    }

    /// <summary>Validator configuration mistakes fail before spending a model request.</summary>
    [Fact]
    public async Task MissingPipelineOrRegistrationFailsBeforeDispatch()
    {
        var client = Client();
        var request = new SupportContext().Compose(client).Prompt("extract");
        await Assert.ThrowsAsync<InvalidOperationException>(() => request.Expect(Contract).SendAsync());
        using var provider = new ServiceCollection().BuildServiceProvider();
        await Assert.ThrowsAsync<InvalidOperationException>(() => request.Expect(Contract, new AiValidationPipeline(provider)).SendAsync());
        Assert.Empty(client.Requests);
    }

    /// <summary>Typed request capture and immutable contracts prevent later builder changes from leaking.</summary>
    [Fact]
    public async Task TypedRequestCapturesBeforeMutation()
    {
        var context = new SupportContext();
        var client = Client();
        var composer = context.Compose(client).Prompt("original");
        var typed = composer.Expect<InvoiceResult>();
        composer.Prompt("changed");
        var result = await typed.SendAsync();
        Assert.Contains("original", result.Request.Messages[^1].Text);
        var empty = new AiResultContract<InvoiceResult>();
        Assert.Empty(empty.ValidatorTypes);
        Assert.Single(empty.ValidateWith<CallbackValidator>().ValidateWith<CallbackValidator>().ValidatorTypes);
        Assert.Empty(empty.ReferenceData);
        Assert.Single(empty.WithReference("key", "value").ReferenceData);
    }

    /// <summary>Separate conversations may validate concurrently, with independent metadata and snapshots.</summary>
    [Fact]
    public async Task ConcurrentConversationsKeepSnapshotsAndGateThroughValidation()
    {
        using var provider = Services();
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        AiValidationContext? firstMetadata = null;
        AiValidationContext? secondMetadata = null;
        firstScope.ServiceProvider.GetRequiredService<CallbackValidator>().Callback = async (_, context, token) =>
        {
            firstMetadata = context;
            entered.SetResult();
            await release.Task.WaitAsync(token);
            return new(AiValidationStatus.Valid, "first", "Passed.");
        };
        secondScope.ServiceProvider.GetRequiredService<CallbackValidator>().Callback = (_, context, _) =>
        {
            secondMetadata = context;
            return ValueTask.FromResult(new AiValidationFinding(AiValidationStatus.Valid, "second", "Passed."));
        };
        var support = new SupportContext();
        var billing = new BillingContext();
        var first = new AiConversationFactory(Client(), new JsonUntrustedRenderer()).Create(support);
        var second = new AiConversationFactory(Client(), new JsonUntrustedRenderer()).Create(billing);
        var contract = new AiResultContract<InvoiceResult>().ValidateWith<CallbackValidator>();
        var pending = first.SendValidatedAsync("first", contract, firstScope.ServiceProvider.GetRequiredService<AiValidationPipeline>());
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        try
        {
            Assert.Throws<InvalidOperationException>(() => first.Configure(config => config.UseContext(billing)));
            Assert.Throws<InvalidOperationException>(() => first.Preview("overlap"));
            await Assert.ThrowsAsync<InvalidOperationException>(() => first.SendValidatedAsync<InvoiceResult>("overlap"));
            Assert.Empty(first.GetState().Turns);
            await second.SendValidatedAsync("second", contract, secondScope.ServiceProvider.GetRequiredService<AiValidationPipeline>());
            Assert.Equal(support.DomainId, firstMetadata!.Configuration.DomainId);
            Assert.Equal(billing.DomainId, secondMetadata!.Configuration.DomainId);
            Assert.NotEqual(firstMetadata.CorrelationId, secondMetadata.CorrelationId);
        }
        finally { release.TrySetResult(); }
        var result = await pending;
        first.Configure(config => config.UseContext(billing));
        Assert.Equal(support.DomainId, result.Request.Configuration.DomainId);
        Assert.Equal(support.DomainId, first.GetState().Turns[0].Request.Configuration.DomainId);
        Assert.Empty(first.Preview("billing").TransmittedTurnIds);
    }
}
