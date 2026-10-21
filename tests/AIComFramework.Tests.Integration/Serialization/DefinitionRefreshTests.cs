using System.Collections.Immutable;
using AIComFramework.Abstractions.Serialization;
using AIComFramework.Core.Conversations;
using AIComFramework.Core.Trust;
using AIComFramework.Tests.Shared.Fixtures;

namespace AIComFramework.Tests.Integration.Serialization;

/// <summary>Verifies refresh boundaries across successive updates and request-local selections.</summary>
public sealed class DefinitionRefreshTests
{
    /// <summary>A composer captures content before selections, and failed updates preserve the last successful content.</summary>
    [Fact]
    public void LateSelectionsAndFailedUpdatesPreserveCapturedContent()
    {
        var context = new SerializableSupportContext();
        using var client = new TestChatClient();
        var original = context.Compose(client);
        var first = Update(context.ExportDefinitions(), "first");
        context.ApplyDefinitions(first);
        var firstComposer = context.Compose(client);
        context.ApplyDefinitions(Update(first, "second"));
        Assert.Throws<ArgumentException>(() => context.ApplyDefinitions(first with { Rules = [] }));

        Assert.Contains("Original instructions", original.WithRole<SerializableSupportRole>().Prompt("hi").Build().Messages[0].Text);
        var firstRequest = firstComposer.WithRole<SerializableSupportRole>().WithRules<SerializableSupportRules>()
            .WithContext(context.Data).Prompt("hi").Build();
        Assert.Equal("first role\n\nfirst rule\n\nfirst rule", firstRequest.Messages[0].Text);
        Assert.Contains("first data", firstRequest.Messages[1].Text);
        Assert.Contains("second role", context.Compose(client).WithRole(context.Agent).Prompt("hi").Build().Messages[0].Text);
    }

    /// <summary>An update during a request callback cannot mix content; switching back to a domain captures its latest content.</summary>
    [Fact]
    public async Task RequestOverridesAndDomainSwitchesRespectUpdateBoundaries()
    {
        var context = new SerializableSupportContext();
        using var client = new TestChatClient();
        var conversation = new AiConversationFactory(client, new JsonUntrustedRenderer()).Create(context);
        await conversation.SendAsync("first", configureRequest: request =>
        {
            context.ApplyDefinitions(Update(context.ExportDefinitions(), "updated"));
            request.WithRole<SerializableSupportRole>().WithRules<SerializableSupportRules>();
        });
        Assert.Contains("Original instructions", conversation.GetState().Turns[0].Request.Messages[0].Text);
        Assert.Null(conversation.GetState().Configuration.RoleId);

        conversation.Configure(config => config.UseContext(new BillingContext()));
        conversation.Configure(config => config.UseContext(context).WithRole<SerializableSupportRole>());
        var next = conversation.Preview("back to support");
        Assert.Equal("updated role", next.Messages[0].Text);
        Assert.Equal(new long[] { 1 }, next.TransmittedTurnIds.ToArray());

        Assert.Throws<InvalidOperationException>(() => conversation.Configure(config =>
        {
            config.RefreshDefinitions(context).ClearRole();
            throw new InvalidOperationException("Rollback the configuration callback.");
        }));
        Assert.Equal("updated role", conversation.Preview("unchanged").Messages[0].Text);
    }

    private static AiDefinitionSnapshot Update(AiDefinitionSnapshot snapshot, string text) => snapshot with
    {
        Roles = snapshot.Roles.Select(role => role with { Instructions = $"{text} role" }).ToImmutableArray(),
        Rules = snapshot.Rules.Select(rule => rule with { Instructions = $"{text} rule" }).ToImmutableArray(),
        Contexts = snapshot.Contexts.Select(context => context with { Text = $"{text} data" }).ToImmutableArray()
    };
}
