using AIComFramework.Abstractions.Rules;

namespace AIComFramework.Samples.Console.Billing;

/// <summary>Invoice response rules.</summary>
public sealed class InvoiceRuleSet : AiRuleSet
{
    /// <summary>Defines invoice verification behavior.</summary>
    public InvoiceRuleSet() : base("invoices", [new AiRule("invoice-id", "Ask for an invoice identifier before discussing an invoice.")]) { }
}
