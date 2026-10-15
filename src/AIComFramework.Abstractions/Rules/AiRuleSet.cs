using System.Collections.Immutable;

namespace AIComFramework.Abstractions.Rules;

/// <summary>Immutable rules composed from direct rules followed by included sets.</summary>
public class AiRuleSet
{
    /// <summary>Copies and flattens definitions; repeated references deduplicate in first-occurrence order.</summary>
    /// <exception cref="ArgumentException">Distinct rules share an identifier or exclusive group.</exception>
    public AiRuleSet(string id, IEnumerable<AiRule> rules, params AiRuleSet[] includes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(includes);
        Id = id;
        var selected = new List<AiRule>();
        foreach (var rule in rules.Concat(includes.SelectMany(set =>
                     (set ?? throw new ArgumentException("Included set is null.", nameof(includes))).Rules)))
        {
            ArgumentNullException.ThrowIfNull(rule);
            var existing = selected.Find(candidate => candidate.Id == rule.Id);
            if (ReferenceEquals(existing, rule)) continue;
            if (existing is not null) throw new ArgumentException($"Conflicting rule ID '{rule.Id}'.");
            if (rule.ExclusiveGroup is not null && selected.Any(candidate => candidate.ExclusiveGroup == rule.ExclusiveGroup))
                throw new ArgumentException($"Conflicting exclusive group '{rule.ExclusiveGroup}'.");
            selected.Add(rule);
        }
        Rules = selected.ToImmutableArray();
    }

    /// <summary>Gets the stable identifier.</summary>
    public string Id { get; }

    /// <summary>Gets the flattened rules in deterministic order.</summary>
    public ImmutableArray<AiRule> Rules { get; }
}
