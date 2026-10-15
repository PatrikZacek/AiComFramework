namespace AIComFramework.Abstractions.Trust;

/// <summary>Explicit developer-approved instructions. Never wrap unchecked user or model text.</summary>
public sealed class Trusted
{
    private Trusted(string text) => Text = text;

    /// <summary>Gets the instruction text; reading it may expose sensitive data.</summary>
    public string Text { get; }

    /// <summary>Explicitly classifies text as application-controlled instructions.</summary>
    public static Trusted From(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        return new(text);
    }

    /// <summary>Returns a content-free diagnostic description.</summary>
    public override string ToString() => nameof(Trusted);
}
