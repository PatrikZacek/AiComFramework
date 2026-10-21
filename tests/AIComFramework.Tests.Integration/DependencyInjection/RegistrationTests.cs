using AIComFramework.Abstractions.Trust;
using AIComFramework.Core.Conversations;
using AIComFramework.Core.DependencyInjection;
using AIComFramework.Tests.Shared.Fixtures;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace AIComFramework.Tests.Integration.DependencyInjection;

/// <summary>Verifies actual service resolution and lifetimes.</summary>
public sealed class RegistrationTests
{
    /// <summary>Registration is idempotent; catalogs are shared while factories honor scopes.</summary>
    [Fact]
    public async Task ServicesResolveWithScopeValidation()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IChatClient>(new TestChatClient());
        services.AddAIComFramework().AddAiContext<CallbackContext>();
        services.AddSingleton<Action<AIComFramework.Core.Contexts.AiContextBuilder>>(_ => _ => { });
        services.AddAIComFramework().AddAiContext<CallbackContext>();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();
        var first = firstScope.ServiceProvider.GetRequiredService<AiConversationFactory>();
        var second = secondScope.ServiceProvider.GetRequiredService<AiConversationFactory>();
        Assert.NotSame(first, second);
        var context = firstScope.ServiceProvider.GetRequiredService<CallbackContext>();
        Assert.Same(context, secondScope.ServiceProvider.GetRequiredService<CallbackContext>());
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IUntrustedRenderer));
        Assert.Equal("reply", (await first.Create(context).SendAsync("question")).Text);
    }
}
