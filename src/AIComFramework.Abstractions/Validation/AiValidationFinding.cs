namespace AIComFramework.Abstractions.Validation;

/// <summary>An immutable diagnostic from one required check.</summary>
public sealed record AiValidationFinding
{
    /// <summary>Creates a finding; validators cannot report NotValidated.</summary>
    /// <param name="status">Valid, Invalid or Unverifiable.</param>
    /// <param name="code">Application-owned diagnostic code.</param>
    /// <param name="message">Diagnostic text; avoid including sensitive data.</param>
    public AiValidationFinding(AiValidationStatus status, string code, string message)
    {
        if (status is not (AiValidationStatus.Valid or AiValidationStatus.Invalid or AiValidationStatus.Unverifiable))
            throw new ArgumentOutOfRangeException(nameof(status));
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentNullException.ThrowIfNull(message);
        Status = status;
        Code = code;
        Message = message;
    }

    /// <summary>Gets this check's outcome.</summary>
    public AiValidationStatus Status { get; }
    /// <summary>Gets the application diagnostic code.</summary>
    public string Code { get; }
    /// <summary>Gets the explicit diagnostic message.</summary>
    public string Message { get; }
}
