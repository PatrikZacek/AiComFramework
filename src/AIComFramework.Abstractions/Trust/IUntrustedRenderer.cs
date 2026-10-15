namespace AIComFramework.Abstractions.Trust;

/// <summary>A trusted application strategy for encoding runtime data, not a security sandbox.</summary>
public interface IUntrustedRenderer
{
    /// <summary>Returns a deterministic data representation preserving provenance and boundaries.</summary>
    string Render(Untrusted value);
}
