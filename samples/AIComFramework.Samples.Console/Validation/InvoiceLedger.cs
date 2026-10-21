namespace AIComFramework.Samples.Console.Validation;

/// <summary>Application-owned authoritative data, independent of model output.</summary>
public sealed class InvoiceLedger
{
    /// <summary>Looks up an independently recorded amount; absence is different from an operational failure.</summary>
    public decimal? FindTotal(string id) => id == "INV-42" ? 120m : null;
}
