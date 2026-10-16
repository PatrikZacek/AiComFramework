using AIComFramework.Abstractions.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AIComFramework.Serialization.Json.DependencyInjection;

/// <summary>Optional JSON registration independent of Core and application storage.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers a singleton, stateless definition serializer without adding storage services.</summary>
    public static IServiceCollection AddAiComJsonSerialization(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<IAiDefinitionSerializer, JsonAiDefinitionSerializer>();
        return services;
    }
}
