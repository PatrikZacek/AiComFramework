namespace AIComFramework.Abstractions.Validation;

/// <summary>Associates a finding with its validator identity.</summary>
/// <param name="Validator">Diagnostic type name; not used for service resolution.</param>
/// <param name="Finding">The validator's immutable finding.</param>
public sealed record AiValidatorResult(string Validator, AiValidationFinding Finding);
