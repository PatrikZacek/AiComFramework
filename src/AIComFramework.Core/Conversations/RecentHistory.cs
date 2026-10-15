using AIComFramework.Abstractions.Conversations;

namespace AIComFramework.Core.Conversations;

/// <summary>Selects the newest eligible completed turns, retaining chronological order.</summary>
public sealed class RecentHistory : IHistorySelector
{
    /// <summary>Creates a turn-count window. Zero disables history transmission.</summary>
    public RecentHistory(int maximumTurns = 20)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maximumTurns);
        MaximumTurns = maximumTurns;
    }

    /// <summary>Gets the maximum number of complete exchanges selected.</summary>
    public int MaximumTurns { get; }

    /// <inheritdoc />
    public ImmutableArray<long> Select(ImmutableArray<ConversationTurn> eligibleTurns) =>
        eligibleTurns.TakeLast(MaximumTurns).Select(turn => turn.Id).ToImmutableArray();
}
