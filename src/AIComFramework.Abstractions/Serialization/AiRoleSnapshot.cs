namespace AIComFramework.Abstractions.Serialization;

/// <summary>Text for a registered instruction slot; application authorization is required before applying it.</summary>
/// <param name="Id">Stable role identity.</param>
/// <param name="Instructions">Plain-text instructions.</param>
public sealed record AiRoleSnapshot(string Id, string Instructions);
