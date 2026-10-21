using AIComFramework.Abstractions.Composition;
using AIComFramework.Abstractions.Validation;

namespace AIComFramework.Abstractions.Results;

/// <summary>A candidate and explicit structural/domain outcomes. Raw output remains untrusted.</summary>
/// <typeparam name="T">Expected result type.</typeparam>
public sealed class AiValidatedResult<T>
{
    /// <summary>Creates an execution result with separate structural and domain metadata.</summary>
    /// <param name="candidate">Typed output, or default on structural failure.</param>
    /// <param name="structuralError">Parsing diagnostic, or null on success.</param>
    /// <param name="rawText">Original model text.</param>
    /// <param name="request">Captured request.</param>
    /// <param name="validationRequested">Whether domain checks were requested.</param>
    /// <param name="validation">Domain outcome.</param>
    public AiValidatedResult(T? candidate, string? structuralError, string rawText, RequestSnapshot request,
        bool validationRequested, AiValidationOutcome validation)
    {
        Candidate = candidate;
        StructuralError = structuralError;
        RawText = rawText;
        Request = request;
        ValidationRequested = validationRequested;
        Validation = validation;
    }

    /// <summary>Gets the candidate for explicit inspection; it may be invalid.</summary>
    public T? Candidate { get; }
    /// <summary>Gets whether a non-null typed candidate was obtained.</summary>
    public bool HasResult => StructuralError is null && Candidate is not null;
    /// <summary>Gets a structural parsing diagnostic, or null on success.</summary>
    public string? StructuralError { get; }
    /// <summary>Gets original response text for deliberate diagnostics.</summary>
    public string RawText { get; }
    /// <summary>Gets the attempted request, including sensitive messages.</summary>
    public RequestSnapshot Request { get; }
    /// <summary>Gets whether domain validators were requested.</summary>
    public bool ValidationRequested { get; }
    /// <summary>Gets the aggregate domain outcome and ordered findings.</summary>
    public AiValidationOutcome Validation { get; }
    /// <summary>Returns the candidate only when all configured domain checks passed.</summary>
    /// <exception cref="InvalidOperationException">No typed candidate or no passing domain validation exists.</exception>
    public T GetValidatedValue() => HasResult && ValidationRequested && Validation.Status == AiValidationStatus.Valid
        ? Candidate! : throw new InvalidOperationException("The candidate has not satisfied configured domain validation.");
    /// <summary>Returns status without exposing candidate or raw content.</summary>
    public override string ToString() => $"Typed result: {HasResult}; domain validation: {Validation.Status}";
}
