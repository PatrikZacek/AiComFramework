using AIComFramework.Core.Contexts;

namespace AIComFramework.Core.Composition;

internal sealed record DefinitionSelection(DefinitionCatalog Catalog, AiRole? Role,
    ImmutableArray<AiRuleSet> RuleSets, ImmutableArray<AiRule> Rules, ImmutableArray<AiContextDefinition> Contexts)
{
    internal static DefinitionSelection Empty(DefinitionCatalog catalog) => new(catalog, null, [], [], []);
    internal ConfigurationSnapshot Snapshot => new(Catalog.DomainId, Role?.Id,
        RuleSets.Select(set => set.Id).ToImmutableArray(), Rules.Select(rule => rule.Id).ToImmutableArray(),
        Contexts.Select(context => context.Id).ToImmutableArray());

    internal DefinitionSelection WithContext(AiContextDefinition definition)
    {
        Catalog.RequireContext(definition);
        return Contexts.Any(context => ReferenceEquals(context, definition))
            ? this : this with { Contexts = Contexts.Add(definition) };
    }

    internal DefinitionSelection WithRole(AiRole role)
    {
        Catalog.RequireRole(role);
        return this with { Role = role };
    }

    internal DefinitionSelection WithRules(AiRuleSet set)
    {
        Catalog.RequireRuleSet(set);
        var result = this;
        foreach (var rule in set.Rules) result = result.WithRule(rule);
        return result with { RuleSets = RuleSets.Any(selected => ReferenceEquals(selected, set)) ? RuleSets : RuleSets.Add(set) };
    }

    internal DefinitionSelection WithRule(AiRule rule)
    {
        Catalog.RequireRule(rule);
        if (Rules.Any(selected => ReferenceEquals(selected, rule))) return this;
        if (rule.ExclusiveGroup is not null && Rules.Any(selected => selected.ExclusiveGroup == rule.ExclusiveGroup))
            throw new ArgumentException($"Conflicting exclusive group '{rule.ExclusiveGroup}'.");
        return this with { Rules = Rules.Add(rule) };
    }
}
