using System.Text.Json;
using AIComFramework.Abstractions.Trust;
using AIComFramework.Core.Contexts;
using Microsoft.Extensions.AI;

namespace AIComFramework.Core.Composition;

/// <summary>Builds one text request. Instances are mutable, local builders and are not thread-safe.</summary>
public sealed class AiPromptComposer
{
    /// <summary>Selects the unique registered definition assignable to the application type.</summary>
    public AiPromptComposer WithRole<T>() where T : AiRole =>
        WithRole(_selection.Catalog.Select<T, AiRole>(_selection.Catalog.Roles));

    /// <summary>Selects the unique registered definition assignable to the application type.</summary>
    public AiPromptComposer WithRules<T>() where T : AiRuleSet =>
        WithRules(_selection.Catalog.Select<T, AiRuleSet>(_selection.Catalog.RuleSets));

    /// <summary>Selects the unique registered definition assignable to the application type.</summary>
    public AiPromptComposer WithRule<T>() where T : AiRule =>
        WithRule(_selection.Catalog.Select<T, AiRule>(_selection.Catalog.Rules));

    /// <summary>Selects the unique registered definition assignable to the application type.</summary>
    public AiPromptComposer WithContext<T>() where T : AiContextDefinition =>
        WithContext(_selection.Catalog.Select<T, AiContextDefinition>(_selection.Catalog.Contexts));

    private DefinitionSelection _selection;
    private readonly IUntrustedRenderer _renderer;
    private readonly RequestExecutor _executor;
    private readonly List<Trusted> _trusted = [];
    private readonly List<Untrusted> _data = [];
    private Untrusted? _prompt;
    private ImmutableArray<MessageSnapshot> _history = [];
    private ImmutableArray<long> _eligible = [];
    private ImmutableArray<long> _transmitted = [];

    internal AiPromptComposer(DefinitionCatalog catalog, IChatClient client, IUntrustedRenderer renderer)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(renderer);
        _selection = DefinitionSelection.Empty(catalog);
        _renderer = renderer;
        _executor = new(client);
    }

    /// <summary>Replaces the selected role with an explicitly registered domain role.</summary>
    public AiPromptComposer WithRole(AiRole role)
    {
        _selection = _selection.WithRole(role);
        return this;
    }

    /// <summary>Adds a registered rule set in selection order; repeated rules deduplicate.</summary>
    public AiPromptComposer WithRules(AiRuleSet rules)
    {
        _selection = _selection.WithRules(rules);
        return this;
    }

    /// <summary>Adds a registered individual rule.</summary>
    public AiPromptComposer WithRule(AiRule rule)
    {
        _selection = _selection.WithRule(rule);
        return this;
    }

    /// <summary>Selects registered reusable domain data; duplicate references deduplicate.</summary>
    public AiPromptComposer WithContext(AiContextDefinition definition)
    {
        _selection = _selection.WithContext(definition);
        return this;
    }

    /// <summary>Removes the selected role from this request only.</summary>
    public AiPromptComposer ClearRole()
    {
        _selection = _selection with { Role = null };
        return this;
    }

    /// <summary>Removes all selected rules and rule sets from this request only.</summary>
    public AiPromptComposer ClearRules()
    {
        _selection = _selection with { Rules = [], RuleSets = [] };
        return this;
    }

    /// <summary>Removes selected reusable data and all runtime context from this request.</summary>
    public AiPromptComposer ClearContexts()
    {
        _selection = _selection with { Contexts = [] };
        _trusted.Clear();
        _data.Clear();
        return this;
    }

    /// <summary>Adds explicit developer-controlled instructions to the system message.</summary>
    public AiPromptComposer AddContext(Trusted value)
    {
        ArgumentNullException.ThrowIfNull(value);
        _trusted.Add(value);
        return this;
    }

    /// <summary>Adds runtime data as a separately encoded user message.</summary>
    public AiPromptComposer AddContext(Untrusted value)
    {
        ArgumentNullException.ThrowIfNull(value);
        _data.Add(value);
        return this;
    }

    /// <summary>Sets the current user prompt, always classified as untrusted.</summary>
    public AiPromptComposer Prompt(string text) => Prompt(Untrusted.From(text, "user"));

    /// <summary>Sets the current untrusted prompt with caller-provided provenance.</summary>
    public AiPromptComposer Prompt(Untrusted value)
    {
        ArgumentNullException.ThrowIfNull(value);
        _prompt = value;
        return this;
    }

    /// <summary>Captures the effective request. Reading Messages explicitly exposes sensitive content.</summary>
    /// <exception cref="InvalidOperationException">No prompt has been supplied.</exception>
    public RequestSnapshot Build()
    {
        if (_prompt is null) throw new InvalidOperationException("A user prompt is required.");
        var messages = ImmutableArray.CreateBuilder<MessageSnapshot>();
        var instructions = new List<string>();
        var content = _selection.Catalog.Content;
        if (_selection.Role is not null) instructions.Add(content.Roles.Single(role => role.Id == _selection.Role.Id).Instructions);
        instructions.AddRange(_selection.Rules.Select(rule => content.Rules.Single(value => value.Id == rule.Id).Instructions));
        instructions.AddRange(_trusted.Select(value => value.Text));
        if (instructions.Count > 0) messages.Add(new(MessageKind.System, string.Join("\n\n", instructions)));
        messages.AddRange(_history);
        foreach (var definition in _selection.Contexts)
        {
            var data = content.Contexts.Single(value => value.Id == definition.Id);
            messages.Add(new(MessageKind.User, _renderer.Render(Untrusted.From(data.Text, data.Source))));
        }
        foreach (var value in _data) messages.Add(new(MessageKind.User, _renderer.Render(value)));
        messages.Add(new(MessageKind.User, _renderer.Render(_prompt)));
        return new(_selection.Snapshot, messages.ToImmutable(), _eligible, _transmitted);
    }

    /// <summary>Sends a newly captured request, preserving provider errors and cancellation.</summary>
    public Task<ChatResponse> SendAsync(CancellationToken cancellationToken = default) =>
        _executor.SendAsync(Build(), cancellationToken);

    /// <summary>Requests a typed SDK response. Schema support depends on the provider.</summary>
    /// <param name="serializerOptions">Serializer configuration for the response type.</param>
    /// <param name="useJsonSchema">Use false for the SDK's JSON-only fallback.</param>
    /// <param name="cancellationToken">Cancellation propagated to the client.</param>
    public Task<ChatResponse<T>> SendAsync<T>(JsonSerializerOptions? serializerOptions = null,
        bool useJsonSchema = true, CancellationToken cancellationToken = default) =>
        _executor.SendAsync<T>(Build(), serializerOptions ?? JsonSerializerOptions.Default, useJsonSchema, cancellationToken);

    internal void SetSelection(DefinitionSelection selection) => _selection = selection;
    internal void SetHistory(ImmutableArray<MessageSnapshot> messages, ImmutableArray<long> eligible, ImmutableArray<long> transmitted)
    {
        _history = messages;
        _eligible = eligible;
        _transmitted = transmitted;
    }
}
