using AIComFramework.Abstractions.Validation;

namespace AIComFramework.Tests.Shared.Fixtures;

internal sealed class InvoiceCurrencyValidator : IAiResultValidator<InvoiceResult>
{
    public ValueTask<AiValidationFinding> ValidateAsync(InvoiceResult candidate, AiValidationContext context, CancellationToken cancellationToken) =>
        ValueTask.FromResult(new AiValidationFinding(candidate.Currency == "EUR" ? AiValidationStatus.Valid : AiValidationStatus.Invalid,
            "currency", "The application supports EUR."));
}
