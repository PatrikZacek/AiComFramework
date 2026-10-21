using AIComFramework.Abstractions.Validation;

namespace AIComFramework.Tests.Shared.Fixtures;

internal sealed class CallbackValidator : IAiResultValidator<InvoiceResult>
{
    internal Func<InvoiceResult, AiValidationContext, CancellationToken, ValueTask<AiValidationFinding>> Callback { get; set; } =
        (_, _, _) => ValueTask.FromResult(new AiValidationFinding(AiValidationStatus.Valid, "callback", "Passed."));
    public ValueTask<AiValidationFinding> ValidateAsync(InvoiceResult candidate, AiValidationContext context, CancellationToken cancellationToken) =>
        Callback(candidate, context, cancellationToken);
}
