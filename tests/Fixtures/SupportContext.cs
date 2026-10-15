using AIComFramework.Abstractions.Roles;
using AIComFramework.Abstractions.Rules;
using AIComFramework.Core.Contexts;

namespace AIComFramework.Tests.Fixtures;

internal sealed class SupportContext : AiContext
{
    internal AiRole Agent { get; } = new("support", "Support instructions");
    internal AiRole Specialist { get; } = new("specialist", "Specialist instructions");
    internal AiRule Baseline { get; } = new("baseline", "Be accurate");
    internal AiRule Returns { get; } = new("returns", "Explain returns");
    internal AiRule Short { get; } = new("short", "Be brief", "length");
    internal AiRule Long { get; } = new("long", "Be detailed", "length");
    internal AiRuleSet Standard { get; }
    internal AiRuleSet ReturnRules { get; }
    internal AiRuleSet Unselected { get; } = new("unused", [new("unused-rule", "NEVER_AUTO_INCLUDE")]);
    internal SupportContext()
    {
        Standard = new("standard", [Baseline]);
        ReturnRules = new("return-rules", [Returns], Standard);
    }
    protected override void OnConfiguring(AiContextBuilder builder) => builder.AddRole(Agent).AddRole(Specialist)
        .AddRuleSet(Standard).AddRuleSet(ReturnRules).AddRuleSet(Unselected).AddRule(Short).AddRule(Long);
}
