using System.Collections.Immutable;

namespace AIComFramework.Abstractions.Serialization;

/// <summary>Immutable declarative domain content. Identities are scoped by domain and definition kind.</summary>
/// <param name="DomainId">Application-owned stable domain identity.</param>
/// <param name="Roles">All registered role slots.</param>
/// <param name="Rules">All registered rule slots.</param>
/// <param name="RuleSets">All registered sets with flattened rule references.</param>
/// <param name="Contexts">All reusable untrusted data slots.</param>
/// <param name="SchemaVersion">Wire schema version, unrelated to prompt content revisions.</param>
/// <param name="ContentFormat">Supported text representation.</param>
public sealed record AiDefinitionSnapshot(
    string DomainId,
    ImmutableArray<AiRoleSnapshot> Roles,
    ImmutableArray<AiRuleSnapshot> Rules,
    ImmutableArray<AiRuleSetSnapshot> RuleSets,
    ImmutableArray<AiContextDataSnapshot> Contexts,
    int SchemaVersion = 1,
    string ContentFormat = "text/plain")
{
    /// <summary>Validates portable structure without authorizing content or resolving application contracts.</summary>
    /// <exception cref="ArgumentException">Content is incomplete, ambiguous, or structurally invalid.</exception>
    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(DomainId);
        if (SchemaVersion != 1) throw new ArgumentException($"Unsupported definition schema '{SchemaVersion}'.");
        if (ContentFormat != "text/plain") throw new ArgumentException($"Unsupported content format '{ContentFormat}'.");
        ValidateIds(Roles, role => role.Id, "role");
        ValidateIds(Rules, rule => rule.Id, "rule");
        ValidateIds(RuleSets, set => set.Id, "rule set");
        ValidateIds(Contexts, context => context.Id, "context data");
        foreach (var role in Roles) ArgumentException.ThrowIfNullOrWhiteSpace(role.Instructions);
        foreach (var rule in Rules)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(rule.Instructions);
            if (rule.ExclusiveGroup is not null) ArgumentException.ThrowIfNullOrWhiteSpace(rule.ExclusiveGroup);
        }
        foreach (var context in Contexts) ArgumentNullException.ThrowIfNull(context.Text);
        var rules = Rules.ToDictionary(rule => rule.Id, StringComparer.Ordinal);
        foreach (var set in RuleSets)
        {
            ValidateIds(set.RuleIds, id => id, $"rule reference in '{set.Id}'");
            var groups = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in set.RuleIds)
            {
                if (!rules.TryGetValue(id, out var rule))
                    throw new ArgumentException($"Rule set '{set.Id}' references unknown rule '{id}'.");
                if (rule.ExclusiveGroup is not null && !groups.Add(rule.ExclusiveGroup))
                    throw new ArgumentException($"Rule set '{set.Id}' conflicts in exclusive group '{rule.ExclusiveGroup}'.");
            }
        }
    }

    private static void ValidateIds<T>(ImmutableArray<T> values, Func<T, string> id, string kind)
    {
        if (values.IsDefault) throw new ArgumentException($"Missing {kind} collection; supply an empty array if unused.");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var value in values)
        {
            if (value is null) throw new ArgumentException($"Null {kind} definition.");
            var identity = id(value);
            ArgumentException.ThrowIfNullOrWhiteSpace(identity);
            if (!ids.Add(identity)) throw new ArgumentException($"Duplicate {kind} identity '{identity}'.");
        }
    }
}
