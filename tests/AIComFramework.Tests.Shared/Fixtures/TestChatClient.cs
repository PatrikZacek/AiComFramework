using Microsoft.Extensions.AI;

namespace AIComFramework.Tests.Shared.Fixtures;

internal sealed class TestChatClient : IChatClient
{
    internal Func<ChatMessage[], ChatOptions?, CancellationToken, Task<ChatResponse>> Respond { get; set; } =
        (_, _, token) => { token.ThrowIfCancellationRequested(); return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "reply"))); };
    internal List<ChatMessage[]> Requests { get; } = [];
    internal ChatOptions? LastOptions { get; private set; }
    internal CancellationToken LastToken { get; private set; }
    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var request = messages.ToArray();
        Requests.Add(request);
        LastOptions = options;
        LastToken = cancellationToken;
        return Respond(request, options, cancellationToken);
    }
    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public object? GetService(Type serviceType, object? serviceKey = null) => serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;
    public void Dispose() { }
}
