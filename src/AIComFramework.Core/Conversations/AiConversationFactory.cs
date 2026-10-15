using AIComFramework.Abstractions.Trust;
using AIComFramework.Core.Composition;
using AIComFramework.Core.Contexts;
using Microsoft.Extensions.AI;

namespace AIComFramework.Core.Conversations;

/// <summary>Creates isolated conversations using application-owned provider and rendering services.</summary>
public sealed class AiConversationFactory
{
    private readonly IChatClient _client;
    private readonly IUntrustedRenderer _renderer;

    /// <summary>Creates a factory; dependencies should support the application's concurrent conversations.</summary>
    public AiConversationFactory(IChatClient client, IUntrustedRenderer renderer)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(renderer);
        _client = client;
        _renderer = renderer;
    }

    /// <summary>Creates an empty conversation in the supplied domain with explicit initial selections.</summary>
    public AiConversation Create(AiContext context, Action<ConversationConfigurationBuilder>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        var conversation = new AiConversation(DefinitionSelection.Empty(context.Catalog), _client, _renderer);
        if (configure is not null) conversation.Configure(configure);
        return conversation;
    }
}
