using AIComFramework.Abstractions.Contexts;
using AIComFramework.Abstractions.Trust;
using AIComFramework.Core.Composition;
using AIComFramework.Core.Conversations;
using AIComFramework.Core.Trust;
using AIComFramework.Tests.Fixtures;

namespace AIComFramework.Tests.Integration.Conversations;

/// <summary>Checks the capability and lifetime boundaries of request-local overrides.</summary>
public sealed class RequestOverrideTests
{
    /// <summary>Alternatives can replace conflicting active rules without changing subsequent turns.</summary>
    [Fact]
    public async Task RequestCanRemoveActiveRoleAndReplaceRules()
    {
        var context = new SupportContext();
        var conversation = new AiConversationFactory(new TestChatClient(), new JsonUntrustedRenderer()).Create(context,
            config => config.WithRole(context.Agent).WithRules(context.Standard).WithRule(context.Short));

        await conversation.SendAsync("long answer", configureRequest: request => request.ClearRole().ClearRules().WithRule(context.Long));
        await conversation.SendAsync("normal answer");

        var turns = conversation.GetState().Turns;
        Assert.Null(turns[0].Request.Configuration.RoleId);
        Assert.Empty(turns[0].Request.Configuration.RuleSetIds);
        Assert.Equal<string>(["long"], turns[0].Request.Configuration.RuleIds);
        Assert.Equal("support", turns[1].Request.Configuration.RoleId);
        Assert.Equal<string>(["baseline", "short"], turns[1].Request.Configuration.RuleIds);
    }

    /// <summary>Clearing contexts removes reusable and runtime data only for the current request.</summary>
    [Fact]
    public async Task RequestCanOmitContextWithoutMutatingActiveConfiguration()
    {
        var data = new AiContextDefinition("reference", Untrusted.From("private reusable facts"));
        var context = new CallbackContext(builder => builder.AddContext(data));
        var conversation = new AiConversationFactory(new TestChatClient(), new JsonUntrustedRenderer()).Create(context,
            config => config.WithContext(data));

        await conversation.SendAsync("omit", configureRequest: request => request
            .AddContext(Trusted.From("private instruction")).AddContext(Untrusted.From("private runtime facts")).ClearContexts());
        await conversation.SendAsync("include");

        Assert.Empty(conversation.GetState().Turns[0].Request.Configuration.ContextIds);
        Assert.DoesNotContain(conversation.GetState().Turns[0].Request.Messages, message => message.Text.Contains("private"));
        Assert.Equal<string>(["reference"], conversation.GetState().Turns[1].Request.Configuration.ContextIds);
    }

    /// <summary>The callback has no execution surface, and an escaped configuration cannot mutate a completed snapshot.</summary>
    [Fact]
    public async Task CallbackCannotExecuteOrChangeRecordedRequest()
    {
        var client = new TestChatClient();
        var context = new SupportContext();
        var conversation = new AiConversationFactory(client, new JsonUntrustedRenderer()).Create(context);
        AiRequestConfiguration? escaped = null;
        var preview = conversation.Preview("preview", configureRequest: request => escaped = request.WithRole(context.Agent));

        Assert.DoesNotContain(typeof(AiRequestConfiguration).GetMethods(), method => method.Name is "SendAsync" or "Prompt" or "Build");
        Assert.Empty(client.Requests);
        escaped!.ClearRole();
        Assert.Equal("support", preview.Configuration.RoleId);
        await conversation.SendAsync("one send", configureRequest: request => escaped = request.WithRole(context.Agent));
        escaped!.ClearRole();
        Assert.Single(client.Requests);
        Assert.Equal("support", conversation.GetState().Turns[0].Request.Configuration.RoleId);
    }

    /// <summary>Callback errors release the gate without sending or recording a turn.</summary>
    [Fact]
    public async Task FailedOverrideLeavesConversationUsable()
    {
        var client = new TestChatClient();
        var conversation = new AiConversationFactory(client, new JsonUntrustedRenderer()).Create(new SupportContext());
        await Assert.ThrowsAsync<ArgumentException>(() => conversation.SendAsync("invalid",
            configureRequest: request => request.WithRole(new BillingContext().Agent)));
        Assert.Empty(client.Requests);
        Assert.Empty(conversation.GetState().Turns);
        await conversation.SendAsync("valid");
        Assert.Single(conversation.GetState().Turns);
    }
}
