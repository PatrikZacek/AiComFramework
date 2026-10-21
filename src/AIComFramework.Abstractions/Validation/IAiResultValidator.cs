namespace AIComFramework.Abstractions.Validation;

/// <summary>A domain check. Operational failures must propagate as exceptions.</summary>
/// <typeparam name="T">Candidate type.</typeparam>
public interface IAiResultValidator<T>
{
    /// <summary>Checks a candidate without mutating it. Missing evidence can yield Unverifiable.</summary>
    /// <param name="candidate">Deserialized model output, still untrusted.</param>
    /// <param name="context">Limited request metadata and explicit reference data.</param>
    /// <param name="cancellationToken">Cancellation for all asynchronous work.</param>
    /// <returns>The required check's finding.</returns>
    ValueTask<AiValidationFinding> ValidateAsync(T candidate, AiValidationContext context, CancellationToken cancellationToken);
}
