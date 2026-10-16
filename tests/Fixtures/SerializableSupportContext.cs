using AIComFramework.Abstractions.Contexts;
using AIComFramework.Abstractions.Rules;
using AIComFramework.Abstractions.Trust;
using AIComFramework.Core.Contexts;

namespace AIComFramework.Tests.Fixtures;

internal sealed class SerializableSupportContext : AiContext
{
    public override string DomainId => "tests.support";
    internal SerializableSupportRole Agent { get; } = new();
    internal AiRule Policy { get; } = new("policy", "Original policy", "policy");
    internal AiRuleSet Baseline { get; } = new("baseline", [new("accuracy", "Be accurate")]);
    internal SerializableSupportRules Returns { get; }
    internal AiContextDefinition Data { get; } = new("catalog", Untrusted.From("Original data", "catalog-source"));
    internal SerializableSupportContext() => Returns = new(Policy, Baseline);
    protected override void OnConfiguring(AiContextBuilder builder) => builder.AddRole(Agent)
        .AddRuleSet(Baseline).AddRuleSet(Returns).AddContext(Data);
}
