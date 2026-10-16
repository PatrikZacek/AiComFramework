using AIComFramework.Abstractions.Serialization;

namespace AIComFramework.Core.Contexts;

internal static class DefinitionSnapshotResolver
{
    internal static DefinitionCatalog Resolve(DefinitionCatalog contracts, AiDefinitionSnapshot snapshot)
    {
        snapshot.Validate();
        if (snapshot.DomainId != contracts.DomainId)
            throw new ArgumentException($"Expected domain '{contracts.DomainId}', received '{snapshot.DomainId}'.");
        var baseline = contracts.Content;
        RequireSameIds(baseline.Roles.Select(value => value.Id), snapshot.Roles.Select(value => value.Id), "role");
        RequireSameIds(baseline.Rules.Select(value => value.Id), snapshot.Rules.Select(value => value.Id), "rule");
        RequireSameIds(baseline.RuleSets.Select(value => value.Id), snapshot.RuleSets.Select(value => value.Id), "rule set");
        RequireSameIds(baseline.Contexts.Select(value => value.Id), snapshot.Contexts.Select(value => value.Id), "context data");
        foreach (var rule in snapshot.Rules.Where(rule => baseline.Rules.Single(value => value.Id == rule.Id).ExclusiveGroup != rule.ExclusiveGroup))
            throw new ArgumentException($"Rule '{rule.Id}' cannot change its registered exclusive group.");
        foreach (var set in snapshot.RuleSets.Where(set => !baseline.RuleSets.Single(value => value.Id == set.Id).RuleIds.SequenceEqual(set.RuleIds)))
            throw new ArgumentException($"Rule set '{set.Id}' must preserve its registered ordered rule references.");

        // Keep original CLR references for typed selection; only immutable declarative content is replaced.
        return contracts with { Content = snapshot };
    }

    private static void RequireSameIds(IEnumerable<string> expected, IEnumerable<string> actual, string kind)
    {
        var required = expected.ToHashSet(StringComparer.Ordinal);
        foreach (var id in actual)
            if (!required.Remove(id)) throw new ArgumentException($"Unknown {kind} identity '{id}'.");
        if (required.Count > 0)
            throw new ArgumentException($"Missing required {kind} identities: {string.Join(", ", required)}.");
    }
}
