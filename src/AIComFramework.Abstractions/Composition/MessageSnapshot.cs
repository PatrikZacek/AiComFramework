namespace AIComFramework.Abstractions.Composition;

/// <summary>Immutable text message, detached from mutable provider objects.</summary>
/// <param name="Kind">The message role.</param>
/// <param name="Text">Sensitive rendered content.</param>
public sealed record MessageSnapshot(MessageKind Kind, string Text)
{
    /// <summary>Returns role information without message content.</summary>
    public override string ToString() => $"Message ({Kind})";
}
