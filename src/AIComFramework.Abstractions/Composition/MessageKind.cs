namespace AIComFramework.Abstractions.Composition;

/// <summary>The text message roles supported by composition.</summary>
public enum MessageKind
{
    /// <summary>Application instructions.</summary>
    System,
    /// <summary>Untrusted runtime input.</summary>
    User,
    /// <summary>Model-generated response.</summary>
    Assistant
}
