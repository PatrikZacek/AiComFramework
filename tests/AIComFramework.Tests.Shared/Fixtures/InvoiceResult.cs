namespace AIComFramework.Tests.Shared.Fixtures;

internal sealed record InvoiceResult
{
    public required string Id { get; init; }
    public required decimal Total { get; init; }
    public required string Currency { get; init; }
}
