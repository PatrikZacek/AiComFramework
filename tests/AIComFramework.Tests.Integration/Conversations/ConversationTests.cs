using AIComFramework.Abstractions.Composition;
using AIComFramework.Abstractions.Trust;
using AIComFramework.Core.Conversations;
using AIComFramework.Core.Trust;
using AIComFramework.Tests.Fixtures;

namespace AIComFramework.Tests.Integration.Conversations;

/// <summary>Exercises dynamic configuration, history boundaries and committed state.</summary>
public sealed class ConversationTests
{
    /// <summary>Preview captures effective content without executing or committing and remains immutable.</summary>
    [Fact]
    public async Task PreviewIsReadOnlyAndDetached()
    {
        var client = new TestChatClient();
        var context = new SupportContext();
        var conversation = new AiConversationFactory(client, new JsonUntrustedRenderer()).Create(context,
            config => config.WithRole(context.Agent));
        await conversation.SendAsync("first");
        var preview = conversation.Preview("next", configureRequest: composer => composer.WithRule(context.Short));
        Assert.Single(client.Requests);
        Assert.Single(conversation.GetState().Turns);
        Assert.Equal<long>([1L], preview.TransmittedTurnIds);
        Assert.Equal<string>(["short"], preview.Configuration.RuleIds);
        conversation.Configure(config => config.WithRole(context.Specialist));
        Assert.Equal("support", preview.Configuration.RoleId);
        Assert.Empty(conversation.GetState().Configuration.RuleIds);
    }

    /// <summary>Domain transitions preserve full history without implicit disclosure.</summary>
    [Fact]
    public async Task SwitchingDomainsSeparatesRecordedEligibleAndTransmittedHistory()
    {
        var support = new SupportContext();
        var billing = new BillingContext();
        var client = new TestChatClient();
        var conversation = new AiConversationFactory(client, new JsonUntrustedRenderer()).Create(support,
            config => config.WithRole(support.Agent).WithRules(support.Standard));
        await conversation.SendAsync("support secret", configureRequest: composer => composer.AddContext(Untrusted.From("private record")));
        await conversation.SendAsync("follow-up");
        var beforeSwitch = conversation.GetState();
        Assert.Equal<long>([1L], beforeSwitch.Turns[1].Request.TransmittedTurnIds);

        conversation.Configure(config => config.UseContext(billing).WithRole(billing.Agent).WithRules(billing.Invoices));
        await conversation.SendAsync("invoice");
        var billingRequest = conversation.GetState().Turns[2].Request;
        Assert.Empty(billingRequest.EligibleTurnIds);
        Assert.Empty(billingRequest.TransmittedTurnIds);
        Assert.DoesNotContain(client.Requests[2], message => message.Text.Contains("support secret") || message.Text.Contains("Support instructions"));
        Assert.Equal(3, conversation.GetState().Turns.Length);
        Assert.Equal(2, beforeSwitch.Turns.Length);

        await conversation.SendAsync("share selected", history: new HistoryOptions(sharedTurnIds: [1]));
        var shared = conversation.GetState().Turns[3].Request;
        Assert.Equal<long>([1L, 3L], shared.EligibleTurnIds);
        Assert.Equal<long>([1L, 3L], shared.TransmittedTurnIds);
        Assert.Contains(shared.Messages, message => message.Text.Contains("support secret"));
        Assert.DoesNotContain(shared.Messages, message => message.Text.Contains("private record") || message.Text.Contains("Support instructions"));

        await conversation.SendAsync("no carry-over grant");
        var next = conversation.GetState().Turns[4].Request;
        Assert.Equal<long>([3L, 4L], next.EligibleTurnIds);
        Assert.DoesNotContain(next.Messages, message => message.Text.Contains("support secret"));
    }

    /// <summary>Role/rule changes and per-request overrides have distinct lifetimes.</summary>
    [Fact]
    public async Task ReconfigurationAndOverridesKeepConsistentSnapshots()
    {
        var context = new SupportContext();
        var conversation = new AiConversationFactory(new TestChatClient(), new JsonUntrustedRenderer()).Create(context,
            config => config.WithRole(context.Agent).WithRules(context.Standard));
        await conversation.SendAsync("first");
        conversation.Configure(config => config.WithRole(context.Specialist).ClearRules().WithRules(context.ReturnRules));
        await conversation.SendAsync("second", configureRequest: composer => composer.WithRole(context.Agent).WithRule(context.Short));
        await conversation.SendAsync("third");
        var state = conversation.GetState();
        Assert.Equal("support", state.Turns[0].Request.Configuration.RoleId);
        Assert.Equal<string>(["baseline"], state.Turns[0].Request.Configuration.RuleIds);
        Assert.Equal("support", state.Turns[1].Request.Configuration.RoleId);
        Assert.Contains("short", state.Turns[1].Request.Configuration.RuleIds);
        Assert.Equal("specialist", state.Turns[2].Request.Configuration.RoleId);
        Assert.DoesNotContain("short", state.Turns[2].Request.Configuration.RuleIds);
        Assert.Equal(6, state.Turns.Sum(turn => turn.Exchange.Length));
    }

    /// <summary>Failed configuration leaves active state intact and escaped builders cannot mutate it.</summary>
    [Fact]
    public void ConfigurationIsTransactional()
    {
        var context = new SupportContext();
        var conversation = new AiConversationFactory(new TestChatClient(), new JsonUntrustedRenderer()).Create(context,
            config => config.WithRole(context.Agent));
        Assert.Throws<ArgumentException>(() => conversation.Configure(config => config.WithRole(context.Specialist).WithRule(context.Short).WithRule(context.Long)));
        Assert.Equal("support", conversation.GetState().Configuration.RoleId);
        ConversationConfigurationBuilder? escaped = null;
        conversation.Configure(config => escaped = config);
        escaped!.ClearRole();
        Assert.Equal("support", conversation.GetState().Configuration.RoleId);
        conversation.Configure(config => config.UseContext(new BillingContext()));
        Assert.Null(conversation.GetState().Configuration.RoleId);
        Assert.Empty(conversation.GetState().Configuration.RuleIds);
    }

    /// <summary>A window changes actual transmission without changing eligibility or full history.</summary>
    [Fact]
    public async Task HistoryWindowIsDistinctFromEligibility()
    {
        var conversation = new AiConversationFactory(new TestChatClient(), new JsonUntrustedRenderer()).Create(new SupportContext());
        await conversation.SendAsync("one");
        await conversation.SendAsync("two");
        await conversation.SendAsync("three", history: new(new RecentHistory(1)));
        var request = conversation.GetState().Turns[2].Request;
        Assert.Equal<long>([1L, 2L], request.EligibleTurnIds);
        Assert.Equal<long>([2L], request.TransmittedTurnIds);
        Assert.DoesNotContain(request.Messages, message => message.Text.Contains("one"));
        await conversation.SendAsync("four", history: new(new RecentHistory(0)));
        Assert.Empty(conversation.GetState().Turns[3].Request.TransmittedTurnIds);
    }

    /// <summary>Independent conversations never share runtime history.</summary>
    [Fact]
    public async Task ConversationsAreIsolated()
    {
        var factory = new AiConversationFactory(new TestChatClient(), new JsonUntrustedRenderer());
        var context = new SupportContext();
        var first = factory.Create(context);
        var second = factory.Create(context);
        await first.SendAsync("first secret");
        await second.SendAsync("second secret");
        Assert.NotEqual(first.Id, second.Id);
        Assert.Single(first.GetState().Turns);
        Assert.Empty(second.GetState().Turns[0].Request.EligibleTurnIds);
        Assert.DoesNotContain(second.GetState().Turns[0].Request.Messages, message => message.Text.Contains("first secret"));
        Assert.All(second.GetState().Turns[0].Exchange, message => Assert.NotEqual(MessageKind.System, message.Kind));
    }
}
