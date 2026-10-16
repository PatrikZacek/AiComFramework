using AIComFramework.Abstractions.Trust;
using AIComFramework.Core.Composition;
using AIComFramework.Core.Trust;
using Microsoft.Extensions.AI;
using AIComFramework.Abstractions.Serialization;

namespace AIComFramework.Core.Contexts;

/// <summary>A thread-safe reusable definition catalog with no conversation state.</summary>
public abstract class AiContext
{
    private readonly Lazy<DefinitionCatalog> _catalog;
    private DefinitionCatalog? _activeCatalog;

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

    /// <summary>Gets the domain identity. Override with an application-owned stable ID for persisted definitions.</summary>
    public virtual string DomainId => $"{GetType().Assembly.GetName().Name}:{GetType().FullName ?? GetType().Name}";

    /// <summary>Registers the typed definitions exposed by the derived context.</summary>
    protected abstract void OnConfiguring(AiContextBuilder builder);

    /// <summary>Initializes and validates registration without issuing a model request.</summary>
    public void Validate() => _ = Catalog;

    /// <summary>Creates an independent one-shot composer. The caller owns the client lifetime.</summary>
    public AiPromptComposer Compose(IChatClient client, IUntrustedRenderer? renderer = null) =>
        new(Catalog, client, renderer ?? new JsonUntrustedRenderer());

    /// <summary>Exports the currently active immutable declarative content, without runtime dependencies.</summary>
    public AiDefinitionSnapshot ExportDefinitions() => Catalog.Content;

    /// <summary>Validates a complete snapshot against registered contracts and atomically publishes it for new composers.</summary>
    /// <remarks>The caller must authorize the source before applying instruction content. Existing composers and
    /// conversations retain their captured content. Concurrent successful updates use last-publication-wins semantics.</remarks>
    /// <exception cref="ArgumentException">Definitions do not match this application's registered contracts.</exception>
    public void ApplyDefinitions(AiDefinitionSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var resolved = DefinitionSnapshotResolver.Resolve(_catalog.Value, snapshot);
        Interlocked.Exchange(ref _activeCatalog, resolved);
    }

    internal DefinitionCatalog Catalog => Volatile.Read(ref _activeCatalog) ?? _catalog.Value;
}
