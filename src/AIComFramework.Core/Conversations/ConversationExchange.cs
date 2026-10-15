using Microsoft.Extensions.AI;

namespace AIComFramework.Core.Conversations;

internal static class ConversationExchange
{
    internal static ImmutableArray<MessageSnapshot> Capture(RequestSnapshot request, ChatResponse response)
    {
        var exchange = ImmutableArray.CreateBuilder<MessageSnapshot>();
        exchange.Add(request.Messages[^1]);
        foreach (var message in response.Messages)
        {
            // Fail explicitly rather than silently losing tools, images or elevated message roles in history.
            if (message.Role != ChatRole.Assistant || message.Contents.Any(content => content is not TextContent))
                throw new NotSupportedException("Conversation history supports assistant text responses only.");
            exchange.Add(new(MessageKind.Assistant, message.Text));
        }
        return exchange.ToImmutable();
    }
}
