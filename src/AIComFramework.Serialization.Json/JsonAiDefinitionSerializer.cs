using System.Text.Json;
using System.Text.Json.Serialization;
using AIComFramework.Abstractions.Serialization;

namespace AIComFramework.Serialization.Json;

/// <summary>Strict JSON adapter for declarative definitions; never resolves or constructs CLR application types.</summary>
public sealed class JsonAiDefinitionSerializer : IAiDefinitionSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
        AllowDuplicateProperties = false,
        WriteIndented = true
    };

    /// <summary>Creates the stateless JSON adapter.</summary>
    public JsonAiDefinitionSerializer() { }

    /// <inheritdoc />
    public string Serialize(AiDefinitionSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        snapshot.Validate();
        return JsonSerializer.Serialize(snapshot, Options);
    }

    /// <inheritdoc />
    /// <exception cref="JsonException">JSON or the declarative structure is invalid or unsupported.</exception>
    public AiDefinitionSnapshot Deserialize(string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        var snapshot = JsonSerializer.Deserialize<AiDefinitionSnapshot>(content, Options)
            ?? throw new JsonException("Expected a definition snapshot object.");
        try { snapshot.Validate(); }
        catch (ArgumentException exception)
        {
            throw new JsonException($"Invalid definition snapshot: {exception.Message}", exception);
        }
        return snapshot;
    }
}
