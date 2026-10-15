using System.Collections.Immutable;

namespace AIComFramework.Abstractions.Conversations;

/// <summary>Selects a subset of eligible completed turns for an individual request.</summary>
public interface IHistorySelector
{
    /// <summary>Returns unique eligible turn IDs in chronological order. Must be safe for concurrent consumers.</summary>
    ImmutableArray<long> Select(ImmutableArray<ConversationTurn> eligibleTurns);
}
