using AIComFramework.Abstractions.Trust;
using AIComFramework.Core.Composition;
using AIComFramework.Core.Trust;
using Microsoft.Extensions.AI;

namespace AIComFramework.Core.Contexts;

/// <summary>A thread-safe reusable definition catalog with no conversation state.</summary>
public abstract class AiContext
{
    private readonly Lazy<DefinitionCatalog> _catalog;

    /// <summary>Defers configuration until derived definition properties have initialized.</summary>
    protected AiContext()
    {
        _catalog = new(() =>
        {
            var builder = new AiContextBuilder();
            OnConfiguring(builder);
            return builder.Build(DomainId);
        }, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>Gets the assembly simple name and full context type name, excluding assembly version.</summary>
    public string DomainId => $"{GetType().Assembly.GetName().Name}:{GetType().FullName ?? GetType().Name}";

    /// <summary>Registers the typed definitions exposed by the derived context.</summary>
    protected abstract void OnConfiguring(AiContextBuilder builder);

    /// <summary>Initializes and validates registration without issuing a model request.</summary>
    public void Validate() => _ = Catalog;

    /// <summary>Creates an independent one-shot composer. The caller owns the client lifetime.</summary>
    public AiPromptComposer Compose(IChatClient client, IUntrustedRenderer? renderer = null) =>
        new(Catalog, client, renderer ?? new JsonUntrustedRenderer());

    internal DefinitionCatalog Catalog => _catalog.Value;
}
