using System.Text.Json;
using AIComFramework.Abstractions.Trust;

namespace AIComFramework.Core.Trust;

/// <summary>Encodes runtime text and provenance as a JSON data envelope; not injection-proof.</summary>
public sealed class JsonUntrustedRenderer : IUntrustedRenderer
{
    /// <summary>Creates a stateless renderer using standard JSON escaping.</summary>
    public JsonUntrustedRenderer() { }

    /// <inheritdoc />
    public string Render(Untrusted value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return JsonSerializer.Serialize(new { kind = "untrusted-data", source = value.Source, text = value.Text });
    }
}
