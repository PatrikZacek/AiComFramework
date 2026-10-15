using AIComFramework.Core.Conversations;
using AIComFramework.Core.Trust;
using AIComFramework.Tests.Fixtures;
using Microsoft.Extensions.AI;

namespace AIComFramework.Tests.Integration.Conversations;

/// <summary>Deterministically tests in-flight operations using completion signals rather than delays.</summary>
public sealed class ConcurrencyTests
{
    /// <summary>Overlapping operations are rejected and captured requests remain unchanged.</summary>
    [Fact]
    public async Task InFlightSendRejectsSendAndConfigure()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<ChatResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        var client = new TestChatClient { Respond = (_, _, _) => { entered.SetResult(); return release.Task; } };
        var context = new SupportContext();
        var conversation = new AiConversationFactory(client, new JsonUntrustedRenderer()).Create(context,
            config => config.WithRole(context.Agent));
        var pending = conversation.SendAsync("first");
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => conversation.SendAsync("second"));
            Assert.Throws<InvalidOperationException>(() => conversation.Configure(config => config.WithRole(context.Specialist)));
            Assert.Empty(conversation.GetState().Turns);
        }
        finally { release.TrySetResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "done"))); }
        await pending;
        Assert.Equal("support", conversation.GetState().Turns[0].Request.Configuration.RoleId);
        conversation.Configure(config => config.WithRole(context.Specialist));
        Assert.Equal("specialist", conversation.GetState().Configuration.RoleId);
    }

    /// <summary>Configuration reentrancy cannot start another operation.</summary>
    [Fact]
    public async Task ConfigurationCallbackRejectsNestedOperations()
    {
        var conversation = new AiConversationFactory(new TestChatClient(), new JsonUntrustedRenderer()).Create(new SupportContext());
        Task<ChatResponse>? nested = null;
        conversation.Configure(_ =>
        {
            Assert.Throws<InvalidOperationException>(() => conversation.Configure(_ => { }));
            nested = conversation.SendAsync("nested");
        });
        await Assert.ThrowsAsync<InvalidOperationException>(() => nested!);
        await conversation.SendAsync("normal");
        Assert.Single(conversation.GetState().Turns);
    }

    /// <summary>Cancellation of an in-flight provider request does not commit a partial turn.</summary>
    [Fact]
    public async Task CancellationReleasesOperationGate()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var client = new TestChatClient { Respond = async (_, _, token) =>
        {
            entered.SetResult();
            await Task.Delay(Timeout.Infinite, token);
            throw new InvalidOperationException("Unreachable");
        } };
        var conversation = new AiConversationFactory(client, new JsonUntrustedRenderer()).Create(new SupportContext());
        using var cancellation = new CancellationTokenSource();
        var pending = conversation.SendAsync("cancel", cancellation.Token);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        Assert.Equal(cancellation.Token, client.LastToken);
        Assert.Empty(conversation.GetState().Turns);
        client.Respond = (_, _, _) => Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok")));
        await conversation.SendAsync("retry");
        Assert.Equal(1, conversation.GetState().Turns[0].Id);
    }
}
