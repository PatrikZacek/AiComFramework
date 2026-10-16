using System.Collections.Immutable;

namespace AIComFramework.Abstractions.Serialization;

/// <summary>Ordered references to the flattened rules of a registered set.</summary>
/// <param name="Id">Stable set identity.</param>
/// <param name="RuleIds">Ordered, unique rule identities.</param>
public sealed record AiRuleSetSnapshot(string Id, ImmutableArray<string> RuleIds);
