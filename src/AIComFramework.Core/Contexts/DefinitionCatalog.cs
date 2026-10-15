namespace AIComFramework.Core.Contexts;

internal sealed record DefinitionCatalog(string DomainId, ImmutableArray<AiRole> Roles,
    ImmutableArray<AiRule> Rules, ImmutableArray<AiRuleSet> RuleSets, ImmutableArray<AiContextDefinition> Contexts)
{
    internal void RequireRole(AiRole role) => Require(Roles, role);
    internal void RequireRule(AiRule rule) => Require(Rules, rule);
    internal void RequireRuleSet(AiRuleSet set) => Require(RuleSets, set);
    internal void RequireContext(AiContextDefinition definition) => Require(Contexts, definition);

    private static void Require<T>(ImmutableArray<T> definitions, T value) where T : class
    {
        ArgumentNullException.ThrowIfNull(value);
        if (!definitions.Any(definition => ReferenceEquals(definition, value)))
            throw new ArgumentException("Definition is not registered in the selected domain.", nameof(value));
    }
}
