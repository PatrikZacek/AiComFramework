using AIComFramework.Abstractions.Serialization;

namespace AIComFramework.Core.Contexts;

internal sealed record DefinitionCatalog(string DomainId, ImmutableArray<AiRole> Roles,
    ImmutableArray<AiRule> Rules, ImmutableArray<AiRuleSet> RuleSets, ImmutableArray<AiContextDefinition> Contexts)
{
    // Record copies retain this token so refresh cannot bind selections to another context's contracts.
    internal object ContractIdentity { get; } = new();

    internal AiDefinitionSnapshot Content { get; init; } = new(DomainId,
        Roles.Select(role => new AiRoleSnapshot(role.Id, role.Instructions)).ToImmutableArray(),
        Rules.Select(rule => new AiRuleSnapshot(rule.Id, rule.Instructions, rule.ExclusiveGroup)).ToImmutableArray(),
        RuleSets.Select(set => new AiRuleSetSnapshot(set.Id, set.Rules.Select(rule => rule.Id).ToImmutableArray())).ToImmutableArray(),
        Contexts.Select(context => new AiContextDataSnapshot(context.Id, context.Data.Text, context.Data.Source)).ToImmutableArray());

    internal void RequireRole(AiRole role) => Require(Roles, role);
    internal void RequireRule(AiRule rule) => Require(Rules, rule);
    internal void RequireRuleSet(AiRuleSet set) => Require(RuleSets, set);
    internal void RequireContext(AiContextDefinition definition) => Require(Contexts, definition);

    internal T Select<T, TBase>(ImmutableArray<TBase> definitions) where T : TBase
    {
        var matches = definitions.OfType<T>().ToArray();
        return matches.Length == 1 ? matches[0] :
            throw new ArgumentException($"Expected exactly one registered definition of type '{typeof(T).Name}'; found {matches.Length}.");
    }

    private static void Require<T>(ImmutableArray<T> definitions, T value) where T : class
    {
        ArgumentNullException.ThrowIfNull(value);
        if (!definitions.Any(definition => ReferenceEquals(definition, value)))
            throw new ArgumentException("Definition is not registered in the selected domain.", nameof(value));
    }
}
