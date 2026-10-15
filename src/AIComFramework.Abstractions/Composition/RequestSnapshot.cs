using System.Collections.Immutable;

namespace AIComFramework.Abstractions.Composition;

/// <summary>Immutable framework composition before any SDK/provider enrichment; Messages exposes sensitive content.</summary>
/// <param name="Configuration">Selected definition metadata.</param>
/// <param name="Messages">Framework-rendered messages in transmission order; SDK structured-output instructions may be added later.</param>
/// <param name="EligibleTurnIds">Turns permitted by domain and explicit sharing.</param>
/// <param name="TransmittedTurnIds">Eligible turns selected for transmission.</param>
public sealed record RequestSnapshot(ConfigurationSnapshot Configuration, ImmutableArray<MessageSnapshot> Messages,
    ImmutableArray<long> EligibleTurnIds, ImmutableArray<long> TransmittedTurnIds)
{
    /// <summary>Returns counts only; does not log message text.</summary>
    public override string ToString() => $"Request ({Messages.Length} messages, {TransmittedTurnIds.Length} history turns)";
}
