using AIComFramework.Core.Contexts;

namespace AIComFramework.Samples.Console.Billing;

/// <summary>Reusable billing catalog.</summary>
public sealed class BillingAiContext : AiContext
{
    /// <summary>Gets the billing role.</summary>
    public BillingSpecialistRole Specialist { get; } = new();

    /// <summary>Gets invoice rules.</summary>
    public InvoiceRuleSet Invoices { get; } = new();

    /// <inheritdoc />
    protected override void OnConfiguring(AiContextBuilder builder) => builder.AddRole(Specialist).AddRuleSet(Invoices);
}
