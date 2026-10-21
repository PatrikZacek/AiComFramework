using AIComFramework.Abstractions.Validation;

namespace AIComFramework.Samples.Console.Validation;

/// <summary>Enforces the application's supported currency independently of the amount check.</summary>
public sealed class InvoiceCurrencyValidator : IAiResultValidator<InvoiceAnalysis>
{
    /// <inheritdoc />
    public ValueTask<AiValidationFinding> ValidateAsync(InvoiceAnalysis candidate, AiValidationContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new AiValidationFinding(
            candidate.Currency == "EUR" ? AiValidationStatus.Valid : AiValidationStatus.Invalid,
            "currency.supported", "This application accepts EUR invoices."));
    }
}
