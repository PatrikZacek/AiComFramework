using System.Text.Json;
using AIComFramework.Abstractions.Results;
using AIComFramework.Abstractions.Validation;
using AIComFramework.Core.Validation;

namespace AIComFramework.Core.Composition;

/// <summary>An immutable typed request captured by Expect.</summary>
/// <typeparam name="T">Expected result type.</typeparam>
public sealed class AiTypedRequest<T>
{
    private readonly RequestExecutor _executor;
    private readonly RequestSnapshot _request;
    private readonly AiResultContract<T> _contract;
    private readonly AiValidationPipeline? _pipeline;

    internal AiTypedRequest(RequestExecutor executor, RequestSnapshot request, AiResultContract<T> contract, AiValidationPipeline? pipeline)
    {
        _executor = executor;
        _request = request;
        _contract = contract;
        _pipeline = pipeline;
    }

    /// <summary>Returns a new request requiring this registered validator.</summary>
    public AiTypedRequest<T> ValidateWith<TValidator>() where TValidator : class, IAiResultValidator<T> =>
        new(_executor, _request, _contract.ValidateWith<TValidator>(), _pipeline);

    /// <summary>Sends once and returns explicit parsing and domain validation outcomes.</summary>
    /// <param name="cancellationToken">Cancellation for execution and validation.</param>
    /// <param name="serializerOptions">JSON type contracts, including required members.</param>
    /// <param name="useJsonSchema">Whether to request provider schema support.</param>
    public async Task<AiValidatedResult<T>> SendAsync(CancellationToken cancellationToken = default,
        JsonSerializerOptions? serializerOptions = null, bool useJsonSchema = true) =>
        (await _executor.SendValidatedAsync(_request, _contract, _pipeline,
            serializerOptions ?? JsonSerializerOptions.Default, useJsonSchema, cancellationToken).ConfigureAwait(false)).Result;
}
