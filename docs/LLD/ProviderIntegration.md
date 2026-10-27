# Provider integration

Core references Microsoft.Extensions.AI 10.11.0 and Microsoft.Extensions.DependencyInjection.Abstractions 10.0.13. Abstractions uses only .NET runtime libraries and has no provider dependency. SDK signatures were checked against the restored package XML documentation before implementation.

RequestExecutor calls IChatClient.GetResponseAsync with freshly allocated SDK messages. It passes cancellation unchanged and does not catch/translate provider errors. Client lifetime and model defaults belong to the application or DI container. Clients and custom renderer/history strategies must support the application's concurrency across separate conversations.

Both typed and untyped execution check cancellation before dispatch and after provider completion. Pre-canceled requests never reach the client, and clients that ignore cancellation cannot return late success. Conversation history rejects an empty response message list rather than recording an unanswered user-only turn.

Typed execution uses the installed ChatClientStructuredOutputExtensions.GetResponseAsync<T> overload with JsonSerializerOptions and useJsonSchemaResponseFormat. Default schema mode requires provider support; false selects the SDK's JSON-only fallback. The SDK lazily deserializes ChatResponse<T>.Result, so RequestExecutor forces Result before returning to avoid recording malformed typed output as a successful turn.

The returned ChatResponse or ChatResponse<T> retains provider-reported Usage, response IDs and other SDK metadata. Usage in a fake test verifies passthrough, not real token measurement. SDK formatting instructions and provider middleware enrichment occur outside the framework composition snapshot.

The framework implements no new model provider interface. Complete-response calls are supported. Streaming/tool orchestration are not implemented. Existing one-shot SendAsync overloads return SDK responses; Expect<T>().SendAsync returns the explicit result envelope described below. Conversation history accepts assistant text only and rejects unsupported content explicitly.

Definition serialization and ApplyDefinitions issue no model calls and do not replace the client or its configuration. Existing composers and in-flight sends retain their captured instruction/data content while a context publishes updates. Conversation RefreshDefinitions affects subsequent request composition only. See [Prompt composition](PromptComposition.md).

Automated tests use the real IChatClient contract through a deterministic fake, covering cancellation, errors, mutable SDK objects, typed schema/fallback output, malformed JSON and response role validation. No provider credentials are needed.

The optional result API uses the same SDK structured-output extension and Result getter. It returns parsing failures explicitly without committing a conversation turn. Only successfully parsed non-null candidates reach the validation pipeline. JsonException raised while reading Result is structural, including one raised by a converter. InvalidOperationException is structural only when it originates in the SDK Result getter (null or absent wrapper in SDK 10.11); other converter exceptions propagate. This narrow SDK-specific filter has regression coverage and needs review when upgrading the SDK. Existing SDK-returning overloads retain their original exception behavior. The new envelope retains raw response text and the framework request, but does not expose SDK usage or provider-specific metadata; callers needing those can continue using the existing SDK response APIs and invoke AiValidationPipeline.ValidateAsync separately. Custom serializer options must supply a TypeInfoResolver as required by the SDK (for example, copy JsonSerializerOptions.Default).
