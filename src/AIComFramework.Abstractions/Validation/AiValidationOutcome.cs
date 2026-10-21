using System.Collections.Immutable;

namespace AIComFramework.Abstractions.Validation;

/// <summary>Domain validation metadata retained separately from structural parsing.</summary>
/// <param name="Status">Aggregate domain status.</param>
/// <param name="Findings">Executed validator findings in selection order.</param>
public sealed record AiValidationOutcome(AiValidationStatus Status, ImmutableArray<AiValidatorResult> Findings)
{
    /// <summary>Gets the explicit outcome for requests without validators.</summary>
    public static AiValidationOutcome NotValidated { get; } = new(AiValidationStatus.NotValidated, []);
}
