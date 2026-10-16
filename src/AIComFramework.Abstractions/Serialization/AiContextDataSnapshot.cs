namespace AIComFramework.Abstractions.Serialization;

/// <summary>Reusable data that always remains untrusted, including its provenance.</summary>
/// <param name="Id">Stable data slot identity.</param>
/// <param name="Text">Plain-text data, never system instructions.</param>
/// <param name="Source">Optional untrusted provenance label.</param>
public sealed record AiContextDataSnapshot(string Id, string Text, string? Source = null);
