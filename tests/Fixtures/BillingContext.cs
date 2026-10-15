using AIComFramework.Abstractions.Roles;
using AIComFramework.Abstractions.Rules;
using AIComFramework.Core.Contexts;

namespace AIComFramework.Tests.Fixtures;

internal sealed class BillingContext(AiRule? shared = null) : AiContext
{
    internal AiRole Agent { get; } = new("billing", "Billing instructions");
    internal AiRuleSet Invoices { get; } = new("invoices", [new("invoice", "Explain invoices")]);
    protected override void OnConfiguring(AiContextBuilder builder)
    {
        builder.AddRole(Agent).AddRuleSet(Invoices);
        if (shared is not null) builder.AddRule(shared);
    }
}
