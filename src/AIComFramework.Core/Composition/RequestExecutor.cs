using System.Text.Json;
using Microsoft.Extensions.AI;

namespace AIComFramework.Core.Composition;

internal sealed class RequestExecutor(IChatClient client)
{
    internal async Task<ChatResponse> SendAsync(RequestSnapshot request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var response = await client.GetResponseAsync(ToMessages(request), cancellationToken: cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return response;
    }

    internal async Task<ChatResponse<T>> SendAsync<T>(RequestSnapshot request, JsonSerializerOptions serializerOptions,
        bool useJsonSchema, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var response = await client.GetResponseAsync<T>(ToMessages(request), serializerOptions,
            useJsonSchemaResponseFormat: useJsonSchema, cancellationToken: cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        // The SDK deserializes lazily. Validate before a conversation can commit a typed turn.
        _ = response.Result;
        return response;
    }

    private static ChatMessage[] ToMessages(RequestSnapshot request) => request.Messages.Select(message =>
        new ChatMessage(message.Kind switch
        {
            MessageKind.System => ChatRole.System,
            MessageKind.User => ChatRole.User,
            MessageKind.Assistant => ChatRole.Assistant,
            _ => throw new InvalidOperationException("Unsupported message role.")
        }, message.Text)).ToArray();
}
