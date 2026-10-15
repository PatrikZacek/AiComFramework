using System.Collections.Immutable;

namespace AIComFramework.Abstractions.Composition;

/// <summary>Definition metadata captured for one configuration or request.</summary>
/// <param name="DomainId">Stable domain identity.</param>
/// <param name="RoleId">Selected role, if any.</param>
/// <param name="RuleSetIds">Explicitly selected rule sets.</param>
/// <param name="RuleIds">Effective rules in first-selection order.</param>
/// <param name="ContextIds">Explicitly selected reusable domain data.</param>
public sealed record ConfigurationSnapshot(string DomainId, string? RoleId,
    ImmutableArray<string> RuleSetIds, ImmutableArray<string> RuleIds, ImmutableArray<string> ContextIds);
