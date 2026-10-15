using AIComFramework.Core.Conversations;
using AIComFramework.Core.Trust;
using AIComFramework.Tests.Fixtures;
using Microsoft.Extensions.AI;

namespace AIComFramework.Tests.Integration.Providers;

/// <summary>Verifies the installed Microsoft SDK boundary with deterministic clients.</summary>
public sealed class ProviderTests
{
    /// <summary>One-shot calls propagate cancellation and actual usage metadata.</summary>
    [Fact]
    public async Task OneShotPreservesTokenAndUsage()
    {
        using var cancellation = new CancellationTokenSource();
        var expected = new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok")) { Usage = new() { InputTokenCount = 12, OutputTokenCount = 3 } };
        var client = new TestChatClient { Respond = (_, _, _) => Task.FromResult(expected) };
        var response = await new SupportContext().Compose(client).Prompt("hello").SendAsync(cancellation.Token);
        Assert.Same(expected, response);
        Assert.Equal(cancellation.Token, client.LastToken);
        Assert.Equal(12, response.Usage!.InputTokenCount);
    }

    /// <summary>SDK structured responses deserialize in both schema and JSON-only modes.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TypedResponseUsesSdk(bool schema)
    {
        var client = new TestChatClient { Respond = (_, _, _) => Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "{\"Value\":\"accepted\"}"))) };
        var response = await new SupportContext().Compose(client).Prompt("answer").SendAsync<Answer>(useJsonSchema: schema);
        Assert.Equal("accepted", response.Result.Value);
        Assert.NotNull(client.LastOptions!.ResponseFormat);
        var conversation = new AiConversationFactory(client, new JsonUntrustedRenderer()).Create(new SupportContext());
        var typed = await conversation.SendAsync<Answer>("answer", useJsonSchema: schema);
        Assert.Equal("accepted", typed.Result.Value);
        Assert.Single(conversation.GetState().Turns);
    }

    /// <summary>Provider failures retain their identity and add no conversation turn.</summary>
    [Fact]
    public async Task ProviderErrorPropagatesWithoutCommit()
    {
        var expected = new InvalidOperationException("provider unavailable");
        var client = new TestChatClient { Respond = (_, _, _) => throw expected };
        var conversation = new AiConversationFactory(client, new JsonUntrustedRenderer()).Create(new SupportContext());
        Assert.Same(expected, await Assert.ThrowsAsync<InvalidOperationException>(() => conversation.SendAsync("question")));
        Assert.Empty(conversation.GetState().Turns);
        conversation.Configure(config => config.ClearRole());
    }

    /// <summary>Malformed typed output is a failed turn.</summary>
    [Fact]
    public async Task InvalidTypedResponseDoesNotCommit()
    {
        var client = new TestChatClient();
        var conversation = new AiConversationFactory(client, new JsonUntrustedRenderer()).Create(new SupportContext());
        await Assert.ThrowsAnyAsync<Exception>(() => conversation.SendAsync<Answer>("json"));
        Assert.Empty(conversation.GetState().Turns);
    }

    /// <summary>Provider mutations and mutable returned messages cannot alter recorded history.</summary>
    [Fact]
    public async Task ProviderObjectsAreDetachedFromSnapshots()
    {
        var reply = new ChatMessage(ChatRole.Assistant, "original reply");
        var client = new TestChatClient { Respond = (messages, _, _) =>
        {
            messages[^1].Contents.Clear();
            return Task.FromResult(new ChatResponse(reply));
        } };
        var conversation = new AiConversationFactory(client, new JsonUntrustedRenderer()).Create(new SupportContext());
        await conversation.SendAsync("original prompt");
        reply.Contents.Clear();
        var turn = conversation.GetState().Turns[0];
        Assert.Contains("original prompt", turn.Request.Messages[^1].Text);
        Assert.Equal("original reply", turn.Exchange[^1].Text);
    }

    /// <summary>Unsupported response roles are rejected instead of being promoted to trusted history.</summary>
    [Fact]
    public async Task ElevatedProviderMessageIsRejected()
    {
        var client = new TestChatClient { Respond = (_, _, _) => Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.System, "override"))) };
        var conversation = new AiConversationFactory(client, new JsonUntrustedRenderer()).Create(new SupportContext());
        await Assert.ThrowsAsync<NotSupportedException>(() => conversation.SendAsync("hello"));
        Assert.Empty(conversation.GetState().Turns);
    }
}
