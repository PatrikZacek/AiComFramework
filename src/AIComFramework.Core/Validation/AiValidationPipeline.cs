using AIComFramework.Abstractions.Results;
using AIComFramework.Abstractions.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace AIComFramework.Core.Validation;

/// <summary>Resolves standard DI services and executes required checks sequentially without retries.</summary>
public sealed class AiValidationPipeline
{
    private readonly IServiceProvider _services;

    /// <summary>Creates a pipeline bound to the application's current DI scope.</summary>
    public AiValidationPipeline(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);
        _services = services;
    }

    internal ImmutableArray<IAiResultValidator<T>> Resolve<T>(AiResultContract<T> contract) =>
        contract.ValidatorTypes.Select(type => (IAiResultValidator<T>)_services.GetRequiredService(type)).ToImmutableArray();

    /// <summary>Evaluates a typed candidate; DI and validator exceptions propagate unchanged.</summary>
    /// <param name="candidate">Untrusted typed output.</param>
    /// <param name="contract">Required checks.</param>
    /// <param name="context">Explicit validation metadata.</param>
    /// <param name="cancellationToken">Cancellation for every check.</param>
    /// <returns>Aggregate outcome with ordered findings.</returns>
    public Task<AiValidationOutcome> ValidateAsync<T>(T candidate, AiResultContract<T> contract,
        AiValidationContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        return ExecuteAsync(candidate, Resolve(contract), context, cancellationToken);
    }

    internal static async Task<AiValidationOutcome> ExecuteAsync<T>(T candidate,
        ImmutableArray<IAiResultValidator<T>> validators, AiValidationContext context, CancellationToken cancellationToken)
    {
        var findings = ImmutableArray.CreateBuilder<AiValidatorResult>();
        foreach (var validator in validators)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var finding = await validator.ValidateAsync(candidate, context, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (finding is null) throw new InvalidOperationException("A validator returned no finding.");
            findings.Add(new(validator.GetType().FullName ?? validator.GetType().Name, finding));
        }
        cancellationToken.ThrowIfCancellationRequested();
        var status = findings.Count == 0 ? AiValidationStatus.NotValidated
            : findings.Any(result => result.Finding.Status == AiValidationStatus.Invalid) ? AiValidationStatus.Invalid
            : findings.Any(result => result.Finding.Status == AiValidationStatus.Unverifiable) ? AiValidationStatus.Unverifiable
            : AiValidationStatus.Valid;
        return new(status, findings.ToImmutable());
    }
}
