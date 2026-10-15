namespace AIComFramework.Core.Contexts;

/// <summary>Collects explicitly registered domain definitions during catalog initialization.</summary>
public sealed class AiContextBuilder
{
    private readonly Dictionary<string, AiRole> _roles = new(StringComparer.Ordinal);
    private readonly Dictionary<string, AiRule> _rules = new(StringComparer.Ordinal);
    private readonly Dictionary<string, AiRuleSet> _sets = new(StringComparer.Ordinal);
    private readonly Dictionary<string, AiContextDefinition> _contexts = new(StringComparer.Ordinal);

    /// <summary>Creates an empty initialization builder.</summary>
    public AiContextBuilder() { }

    /// <summary>Registers a role; identical repeated references are harmless.</summary>
    public AiContextBuilder AddRole(AiRole role)
    {
        ArgumentNullException.ThrowIfNull(role);
        Register(_roles, role.Id, role);
        return this;
    }

    /// <summary>Registers an individually selectable rule.</summary>
    public AiContextBuilder AddRule(AiRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        Register(_rules, rule.Id, rule);
        return this;
    }

    /// <summary>Registers a selectable set and its flattened rules.</summary>
    public AiContextBuilder AddRuleSet(AiRuleSet rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ValidateRegistration(_sets, rules.Id, rules);
        foreach (var rule in rules.Rules) ValidateRegistration(_rules, rule.Id, rule);
        Register(_sets, rules.Id, rules);
        foreach (var rule in rules.Rules) AddRule(rule);
        return this;
    }

    /// <summary>Registers reusable domain data without automatically transmitting it.</summary>
    public AiContextBuilder AddContext(AiContextDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        Register(_contexts, definition.Id, definition);
        return this;
    }

    internal DefinitionCatalog Build(string domainId) => new(domainId,
        _roles.Values.ToImmutableArray(), _rules.Values.ToImmutableArray(), _sets.Values.ToImmutableArray(),
        _contexts.Values.ToImmutableArray());

    private static void Register<T>(Dictionary<string, T> registrations, string id, T definition) where T : class
    {
        ValidateRegistration(registrations, id, definition);
        registrations[id] = definition;
    }

    private static void ValidateRegistration<T>(Dictionary<string, T> registrations, string id, T definition) where T : class
    {
        if (registrations.TryGetValue(id, out var existing) && !ReferenceEquals(existing, definition))
            throw new ArgumentException($"Conflicting {typeof(T).Name} registration '{id}'.");
    }
}
