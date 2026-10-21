using AIComFramework.Abstractions.Validation;

namespace AIComFramework.Samples.Console.Validation;

/// <summary>Compares extracted amounts against the application ledger.</summary>
/// <param name="ledger">Authoritative source resolved through standard DI.</param>
public sealed class InvoiceAmountValidator(InvoiceLedger ledger) : IAiResultValidator<InvoiceAnalysis>
{
    /// <inheritdoc />
    public ValueTask<AiValidationFinding> ValidateAsync(InvoiceAnalysis candidate, AiValidationContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var amount = ledger.FindTotal(candidate.Id);
        var finding = amount is null
            ? new AiValidationFinding(AiValidationStatus.Unverifiable, "amount.missing", "No authoritative invoice record exists.")
            : candidate.Total == amount
                ? new AiValidationFinding(AiValidationStatus.Valid, "amount.matches", "Total matches the ledger.")
                : new AiValidationFinding(AiValidationStatus.Invalid, "amount.mismatch", "Total differs from the ledger.");
        return ValueTask.FromResult(finding);
    }
}
