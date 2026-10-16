namespace AIComFramework.Abstractions.Serialization;

/// <summary>Converts declarative definitions to portable content without owning persistence or authorization.</summary>
public interface IAiDefinitionSerializer
{
    /// <summary>Serializes a validated definition snapshot.</summary>
    string Serialize(AiDefinitionSnapshot snapshot);

    /// <summary>Reads and validates data without activating it or granting instruction authority.</summary>
    AiDefinitionSnapshot Deserialize(string content);
}
