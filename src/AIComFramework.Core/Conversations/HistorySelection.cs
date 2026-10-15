using AIComFramework.Abstractions.Conversations;

namespace AIComFramework.Core.Conversations;

internal sealed record HistorySelection(ImmutableArray<long> Eligible, ImmutableArray<long> Transmitted,
    ImmutableArray<MessageSnapshot> Messages)
{
    internal static HistorySelection Create(ImmutableArray<ConversationTurn> turns, string domainId, HistoryOptions options)
    {
        var recordedIds = turns.Select(turn => turn.Id).ToHashSet();
        if (!options.SharedTurnIds.IsSubsetOf(recordedIds))
            throw new ArgumentException("A shared turn ID does not exist in this conversation.", nameof(options));
        var eligible = turns.Where(turn => turn.Request.Configuration.DomainId == domainId || options.SharedTurnIds.Contains(turn.Id))
            .ToImmutableArray();
        var selected = options.Selector.Select(eligible);
        var eligibleIds = eligible.Select(turn => turn.Id).ToImmutableArray();
        if (selected.IsDefault)
            throw new InvalidOperationException("History selector must return unique eligible IDs in chronological order.");
        var selectedIds = selected.ToHashSet();
        if (!selected.SequenceEqual(eligibleIds.Where(selectedIds.Contains)))
            throw new InvalidOperationException("History selector must return unique eligible IDs in chronological order.");
        return new(eligibleIds, selected, eligible.Where(turn => selectedIds.Contains(turn.Id))
            .SelectMany(turn => turn.Exchange).ToImmutableArray());
    }
}
