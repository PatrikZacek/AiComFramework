namespace AIComFramework.Abstractions.Trust;

/// <summary>Runtime data with optional provenance; carries no instruction authority.</summary>
public sealed class Untrusted
{
    private Untrusted(string text, string? source) { Text = text; Source = source; }

    /// <summary>Gets the sensitive runtime text.</summary>
    public string Text { get; }

    /// <summary>Gets the supplied provenance label, which is also untrusted data.</summary>
    public string? Source { get; }

    /// <summary>Classifies runtime text without elevating its authority.</summary>
    public static Untrusted From(string text, string? source = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        return new(text, source);
    }

    /// <summary>Returns a description without content or provenance.</summary>
    public override string ToString() => nameof(Untrusted);
}
