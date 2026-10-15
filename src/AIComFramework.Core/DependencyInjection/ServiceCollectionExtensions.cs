using AIComFramework.Abstractions.Trust;
using AIComFramework.Core.Contexts;
using AIComFramework.Core.Conversations;
using AIComFramework.Core.Trust;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AIComFramework.Core.DependencyInjection;

/// <summary>Registers reusable catalogs and scoped runtime factories with standard .NET DI.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers default rendering and a scoped factory; the application must register IChatClient.</summary>
    public static IServiceCollection AddAIComFramework(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<IUntrustedRenderer, JsonUntrustedRenderer>();
        services.TryAddScoped<AiConversationFactory>();
        return services;
    }

    /// <summary>Registers a singleton domain catalog; its dependencies must be singleton-safe.</summary>
    public static IServiceCollection AddAiContext<TContext>(this IServiceCollection services) where TContext : AiContext
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<TContext>();
        return services;
    }
}
