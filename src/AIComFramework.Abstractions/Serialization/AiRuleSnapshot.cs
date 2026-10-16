namespace AIComFramework.Abstractions.Serialization;

/// <summary>Text and structural metadata for a registered instruction rule.</summary>
/// <param name="Id">Stable rule identity.</param>
/// <param name="Instructions">Plain-text instructions.</param>
/// <param name="ExclusiveGroup">Application-defined exclusivity contract, which external content cannot change.</param>
public sealed record AiRuleSnapshot(string Id, string Instructions, string? ExclusiveGroup = null);
