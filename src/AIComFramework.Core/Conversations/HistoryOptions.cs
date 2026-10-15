using AIComFramework.Abstractions.Conversations;

namespace AIComFramework.Core.Conversations;

/// <summary>Per-request history policy. Cross-domain disclosure requires explicit turn IDs.</summary>
public sealed class HistoryOptions
{
    /// <summary>Copies sharing IDs and selects a default window of twenty eligible turns.</summary>
    public HistoryOptions(IHistorySelector? selector = null, IEnumerable<long>? sharedTurnIds = null)
    {
        Selector = selector ?? new RecentHistory();
        SharedTurnIds = (sharedTurnIds ?? []).ToImmutableHashSet();
        if (SharedTurnIds.Any(id => id <= 0)) throw new ArgumentOutOfRangeException(nameof(sharedTurnIds));
    }

    /// <summary>Gets the trusted application history strategy.</summary>
    public IHistorySelector Selector { get; }

    /// <summary>Gets explicit cross-domain eligibility grants for this request only.</summary>
    public ImmutableHashSet<long> SharedTurnIds { get; }
}
