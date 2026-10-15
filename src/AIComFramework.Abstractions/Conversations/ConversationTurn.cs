using System.Collections.Immutable;
using AIComFramework.Abstractions.Composition;

namespace AIComFramework.Abstractions.Conversations;

/// <summary>A completed interaction with its immutable configuration and actual request.</summary>
/// <param name="Id">Monotonically increasing conversation-local identifier.</param>
/// <param name="Request">Effective request and selection audit.</param>
/// <param name="Exchange">Only the original rendered user prompt and assistant replies, eligible for future sharing.</param>
public sealed record ConversationTurn(long Id, RequestSnapshot Request, ImmutableArray<MessageSnapshot> Exchange)
{
    /// <summary>Returns the turn ID without message content.</summary>
    public override string ToString() => $"Turn {Id}";
}
