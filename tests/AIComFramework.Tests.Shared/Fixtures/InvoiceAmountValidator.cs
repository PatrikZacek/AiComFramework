using AIComFramework.Abstractions.Validation;

namespace AIComFramework.Tests.Shared.Fixtures;

internal sealed class InvoiceAmountValidator(InvoiceLedger ledger) : IAiResultValidator<InvoiceResult>
{
    public ValueTask<AiValidationFinding> ValidateAsync(InvoiceResult candidate, AiValidationContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var status = !ledger.Totals.TryGetValue(candidate.Id, out var amount) ? AiValidationStatus.Unverifiable
            : candidate.Total == amount ? AiValidationStatus.Valid : AiValidationStatus.Invalid;
        return ValueTask.FromResult(new AiValidationFinding(status, "amount", "Compared with the application ledger."));
    }
}
