# Prompt composition

AiPromptComposer is an operation-local mutable builder. AiContext creates independent instances; conversations create one per turn. Builders are not safe for concurrent mutation, and request callbacks must not escape them into concurrent work.

AiContext.Compose captures its current immutable DefinitionCatalog immediately, before any selection or Build call. Conversation composers use the conversation's captured catalog. Applying new definitions later, including inside a request callback, cannot mix new content into an existing composer. New standalone composers see the latest published content; conversations adopt it only through explicit configuration. See [Domain contexts](DomainContexts.md) and [Definition serialization and updates](DefinitionSerialization.md).

DefinitionSelection is an internal immutable value shared by composition and conversation configuration. It centralizes domain membership, role replacement, rule conflict detection and reusable-data selection. No definitions are transmitted just because they are registered.

Object-based selection requires original registered references. Generic selection resolves exactly one registered object assignable to the requested type. Build obtains role/rule instruction strings and reusable data/provenance from the captured catalog's Content by stable ID. Registered object properties retain compiled defaults after updates; those properties are not the active rendering source. Rule-set membership and exclusive groups remain the registered contracts.

Conversation callbacks receive AiRequestConfiguration, a restricted facade over the local composer. It exposes selection and contextual-data overrides, but no execution, prompt or history methods. This keeps Preview read-only and prevents unrecorded nested provider calls through the callback API. ClearRole, ClearRules and ClearContexts support removing active selections for one request; clearing contexts removes both reusable definitions and runtime trusted/untrusted additions.

Build requires a prompt and returns a RequestSnapshot containing immutable strings and arrays. Message order is:

1. One system message, if needed: selected role, effective rules, explicit Trusted context, separated by blank lines.
2. Selected historical user/assistant exchanges.
3. Selected reusable domain data, in selection order.
4. Runtime Untrusted context, in insertion order.
5. The current Untrusted prompt.

The default renderer emits JSON envelopes. Empty user data is legal; missing Prompt is not. Repeated Build calls are deterministic with the default renderer and unchanged builder. Custom renderers are trusted application strategies and must be deterministic and concurrent-consumer safe.

RequestExecutor allocates fresh ChatMessage objects for each execution. Mutable provider inputs cannot mutate framework snapshots. Its provider dependency is IChatClient; the optional validated path also receives a scoped AiValidationPipeline for explicitly selected validators. SDK typed-output processing or provider middleware may enrich messages/options after the framework snapshot; the snapshot is not a network-wire capture.

Build and conversation Preview provide explicit sensitive inspection. Diagnostic ToString methods omit message bodies. Accessing Messages or serializing/destructuring snapshots is intentionally sensitive and is not automatically redacted. Definition IDs themselves should not contain private data.

Expect<T>() captures Build() immediately into an immutable AiTypedRequest<T>. Finish role/rule/context/prompt selection before Expect. ValidateWith<TValidator>() returns a new typed request without changing an earlier selection. SendAsync on that request obtains an AiValidatedResult<T>; the original composer SendAsync overloads still return SDK responses. Domain validation occurs after execution, never during Build or Preview.
