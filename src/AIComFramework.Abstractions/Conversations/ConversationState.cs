using System.Collections.Immutable;
using AIComFramework.Abstractions.Composition;

namespace AIComFramework.Abstractions.Conversations;

/// <summary>In-memory export of committed state; does not implement durable storage or restoration.</summary>
/// <param name="Id">Conversation identity.</param>
/// <param name="Configuration">Current active definitions.</param>
/// <param name="Turns">All completed turns, including sensitive request content.</param>
public sealed record ConversationState(Guid Id, ConfigurationSnapshot Configuration, ImmutableArray<ConversationTurn> Turns)
{
    /// <summary>Returns identity and count only.</summary>
    public override string ToString() => $"Conversation {Id} ({Turns.Length} turns)";
}
