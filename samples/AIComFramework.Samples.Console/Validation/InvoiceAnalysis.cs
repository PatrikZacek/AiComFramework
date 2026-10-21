namespace AIComFramework.Samples.Console.Validation;

/// <summary>Extracted invoice fields; parsing does not establish their correctness.</summary>
public sealed record InvoiceAnalysis
{
    /// <summary>Gets the claimed invoice identity.</summary>
    public required string Id { get; init; }
    /// <summary>Gets the claimed total.</summary>
    public required decimal Total { get; init; }
    /// <summary>Gets the claimed currency.</summary>
    public required string Currency { get; init; }
}
