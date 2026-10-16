using AIComFramework.Abstractions.Serialization;

namespace AIComFramework.Samples.Console.Persistence;

// Storage and source authorization belong to the consumer, not the serialization adapter.
internal sealed class InMemoryDefinitionRepository(IAiDefinitionSerializer serializer)
{
    private string? _content;

    internal void Save(AiDefinitionSnapshot snapshot) => _content = serializer.Serialize(snapshot);

    internal AiDefinitionSnapshot Load() => serializer.Deserialize(
        _content ?? throw new InvalidOperationException("No definitions have been saved."));
}
