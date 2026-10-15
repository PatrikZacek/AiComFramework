using AIComFramework.Abstractions.Roles;

namespace AIComFramework.Samples.Console.Billing;

/// <summary>Reusable billing behavior.</summary>
public sealed class BillingSpecialistRole : AiRole
{
    /// <summary>Defines billing responsibilities.</summary>
    public BillingSpecialistRole() : base("billing-specialist", "Explain billing and invoice questions using supplied facts.") { }
}
