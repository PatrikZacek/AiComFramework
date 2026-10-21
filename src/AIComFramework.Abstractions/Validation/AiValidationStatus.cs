namespace AIComFramework.Abstractions.Validation;

/// <summary>Outcome of configured domain checks, never a universal correctness guarantee.</summary>
public enum AiValidationStatus
{
    /// <summary>No domain validators were requested.</summary>
    NotValidated,
    /// <summary>Every configured required check passed.</summary>
    Valid,
    /// <summary>At least one required check established a violation.</summary>
    Invalid,
    /// <summary>No violation was established, but required evidence was insufficient.</summary>
    Unverifiable
}
