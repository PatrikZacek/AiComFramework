using System.Text.Json;
using AIComFramework.Abstractions.Results;
using AIComFramework.Abstractions.Validation;
using AIComFramework.Core.Validation;
using Microsoft.Extensions.AI;

namespace AIComFramework.Core.Composition;

internal sealed class RequestExecutor(IChatClient client)
{
    internal async Task<(AiValidatedResult<T> Result, ChatResponse Response)> SendValidatedAsync<T>(
        RequestSnapshot request, AiResultContract<T> contract, AiValidationPipeline? pipeline,
        JsonSerializerOptions serializerOptions, bool useJsonSchema, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var validators = contract.ValidationRequested
            ? (pipeline ?? throw new InvalidOperationException("A validation pipeline is required for selected validators.")).Resolve(contract)
            : ImmutableArray<IAiResultValidator<T>>.Empty;
        var context = new AiValidationContext(Guid.NewGuid(), request.Configuration, contract.ReferenceData);
        var response = await client.GetResponseAsync<T>(ToMessages(request), serializerOptions,
            useJsonSchemaResponseFormat: useJsonSchema, cancellationToken: cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        T? candidate = default;
        string? structuralError = null;
        try
        {
            candidate = response.Result;
        }
        catch (JsonException exception)
        {
            structuralError = exception.Message;
        }
        catch (InvalidOperationException exception) when (
            exception.TargetSite?.DeclaringType?.IsGenericType == true &&
            exception.TargetSite.DeclaringType.GetGenericTypeDefinition() == typeof(ChatResponse<>) &&
            exception.TargetSite.Name == "get_Result")
        {
            // SDK 10.11 throws here for null results or absent structured-output wrappers.
            // Restrict this catch to SDK-originated failures, not application converters.
            structuralError = exception.Message;
        }
        cancellationToken.ThrowIfCancellationRequested();
        var validation = structuralError is null
            ? await AiValidationPipeline.ExecuteAsync(candidate!, validators, context, cancellationToken).ConfigureAwait(false)
            : contract.ValidationRequested
                ? new AiValidationOutcome(AiValidationStatus.Unverifiable, [])
                : AiValidationOutcome.NotValidated;
        return (new(candidate, structuralError, response.Text, request, contract.ValidationRequested, validation), response);
    }

    internal async Task<ChatResponse> SendAsync(RequestSnapshot request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var response = await client.GetResponseAsync(ToMessages(request), cancellationToken: cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return response;
    }

    internal async Task<ChatResponse<T>> SendAsync<T>(RequestSnapshot request, JsonSerializerOptions serializerOptions,
        bool useJsonSchema, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var response = await client.GetResponseAsync<T>(ToMessages(request), serializerOptions,
            useJsonSchemaResponseFormat: useJsonSchema, cancellationToken: cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        // The SDK deserializes lazily. Validate before a conversation can commit a typed turn.
        _ = response.Result;
        return response;
    }

    private static ChatMessage[] ToMessages(RequestSnapshot request) => request.Messages.Select(message =>
        new ChatMessage(message.Kind switch
        {
            MessageKind.System => ChatRole.System,
            MessageKind.User => ChatRole.User,
            MessageKind.Assistant => ChatRole.Assistant,
            _ => throw new InvalidOperationException("Unsupported message role.")
        }, message.Text)).ToArray();
}
