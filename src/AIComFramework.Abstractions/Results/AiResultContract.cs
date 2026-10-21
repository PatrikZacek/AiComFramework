using System.Collections.Immutable;
using AIComFramework.Abstractions.Validation;

namespace AIComFramework.Abstractions.Results;

/// <summary>Immutable expected type and ordered required validator selection.</summary>
/// <typeparam name="T">Expected deserialized result type.</typeparam>
public sealed class AiResultContract<T>
{
    /// <summary>Creates a typed contract without domain validation.</summary>
    public AiResultContract() : this([], ImmutableDictionary<string, string>.Empty) { }

    private AiResultContract(ImmutableArray<Type> validators, ImmutableDictionary<string, string> referenceData)
    {
        ValidatorTypes = validators;
        ReferenceData = referenceData;
    }

    /// <summary>Gets required validator service types in execution order.</summary>
    public ImmutableArray<Type> ValidatorTypes { get; }
    /// <summary>Gets application-selected reference data.</summary>
    public ImmutableDictionary<string, string> ReferenceData { get; }
    /// <summary>Gets whether domain validation was explicitly requested.</summary>
    public bool ValidationRequested => !ValidatorTypes.IsEmpty;

    /// <summary>Returns a new contract selecting a registered validator, deduplicated by type.</summary>
    public AiResultContract<T> ValidateWith<TValidator>() where TValidator : class, IAiResultValidator<T> =>
        new(ValidatorTypes.Contains(typeof(TValidator)) ? ValidatorTypes : ValidatorTypes.Add(typeof(TValidator)), ReferenceData);

    /// <summary>Returns a new contract with an application-owned reference string.</summary>
    /// <param name="key">Reference name.</param>
    /// <param name="value">Immutable reference value, supplied deliberately to validators.</param>
    public AiResultContract<T> WithReference(string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);
        return new(ValidatorTypes, ReferenceData.SetItem(key, value));
    }
}
