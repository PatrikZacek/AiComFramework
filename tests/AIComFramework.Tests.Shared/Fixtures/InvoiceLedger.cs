namespace AIComFramework.Tests.Shared.Fixtures;

internal sealed class InvoiceLedger
{
    internal Dictionary<string, decimal> Totals { get; } = new() { ["invoice-1"] = 120m };
}
