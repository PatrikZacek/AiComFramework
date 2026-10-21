using System.Collections.Immutable;
using AIComFramework.Abstractions.Conversations;

namespace AIComFramework.Tests.Shared.Fixtures;

internal sealed class FixedHistorySelector(params long[] ids) : IHistorySelector
{
    /// <inheritdoc />
    public ImmutableArray<long> Select(ImmutableArray<ConversationTurn> eligibleTurns) => ids.ToImmutableArray();
}
