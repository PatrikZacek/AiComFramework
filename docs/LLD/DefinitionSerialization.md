# Definition serialization and updates

AIComFramework.Abstractions.Serialization owns IAiDefinitionSerializer and immutable AiDefinitionSnapshot, AiRoleSnapshot, AiRuleSnapshot, AiRuleSetSnapshot and AiContextDataSnapshot records. Optional AIComFramework.Serialization.Json implements JsonAiDefinitionSerializer using System.Text.Json and supplies AddAiComJsonSerialization. The adapter is stateless and registered as a singleton with TryAddSingleton. It depends on Abstractions and DI abstractions; Core has no reference to the JSON module or storage services.

AiContext.ExportDefinitions returns the current complete declarative content. The snapshot contains DomainId, all role/rule IDs and instructions, rule exclusive groups, rule-set IDs with flattened ordered rule references, and reusable context IDs, text and optional provenance. IDs are ordinal and scoped by domain and definition kind; a rule and rule set may share an ID. Applications should override the default type-derived DomainId with a stable application-owned ID for persisted definitions. No CLR type-activation metadata, clients, runtime services, conversation history, validator selection or result candidates are serialized.

SchemaVersion defaults to 1 and ContentFormat to text/plain; other values are rejected. Schema version describes the representation, not the revision of natural-language content. There is no required content revision field. Editing compatible text, data or provenance needs neither recompilation nor a schema change. Rule-set nesting is already flattened by AiRuleSet; the snapshot preserves effective order and shared rule identities rather than reconstructing nested sets.

The boundaries are separate:

| Operation | Validation and effect |
| --- | --- |
| AiDefinitionSnapshot.Validate | Checks portable structure without resolving application contracts or authorizing content |
| JsonAiDefinitionSerializer.Serialize | Validates the supplied snapshot and writes indented camelCase JSON |
| JsonAiDefinitionSerializer.Deserialize | Parses strict JSON and validates portable structure; returns data without activation |
| AiContext.ApplyDefinitions | Validates complete content against original registered contracts, then atomically publishes it for new consumers |
| ConversationConfigurationBuilder.RefreshDefinitions | Explicitly adopts the original context instance's current catalog while preserving selections and history |

Portable validation requires a non-empty domain, non-default collections, non-null entries and non-empty IDs unique within each kind. Role/rule instructions must be non-whitespace; context text must be non-null but may be empty. Exclusive groups are optional but cannot be whitespace when supplied. Rule-set references must be unique, resolve to existing rules and contain no exclusive-group conflicts. JSON parsing also rejects unknown properties, duplicate properties, missing required constructor arguments and invalid nulls. Optional schema/format arguments retain their defaults when omitted.

Deserialize raises JsonException for malformed JSON or invalid portable structure; null/empty/whitespace input raises an ArgumentException subtype. Serialize and direct snapshot validation raise ArgumentException subtypes for invalid arguments/structure. Applying a null snapshot raises ArgumentNullException. Structurally valid JSON may still be incompatible with an application's contracts, so successful deserialization does not imply successful application.

DefinitionSnapshotResolver compares each candidate snapshot against the original lazy catalog, not the previously applied content. The domain and complete ID sets for each kind must match. Rule exclusive groups and each rule set's ordered flattened membership must remain unchanged. Missing or unknown slots, domain mismatches and incompatible contracts raise ArgumentException before publication. The replacement catalog retains original registered object references and its ContractIdentity token; only Content changes. Generic selection still requires exactly one registered definition assignable to the requested type.

AiContext publishes a successful replacement with Interlocked.Exchange and reads active content with Volatile.Read. Concurrent updates use last-publication-wins semantics per context instance. Failed updates leave the last active content unchanged; instances sharing a DomainId are updated independently. Registered definition properties continue to expose compiled defaults. ExportDefinitions inspects active content, and composition reads the captured catalog's Content by stable ID.

Compose captures the active catalog immediately, before later selections or Build. Existing composers, built requests and in-flight sends keep their original content across updates. Conversations retain their captured catalog until explicit Configure calls RefreshDefinitions or UseContext. Refresh requires the original context instance and preserves selections/history; UseContext captures current content and clears selections while preserving history. Configuration cannot overlap a send or preview. Failed refresh/configuration leaves committed conversation state unchanged. See [Conversation state](ConversationState.md) and [Prompt composition](PromptComposition.md).

```mermaid
sequenceDiagram
  Application->>AiContext: ExportDefinitions()
  AiContext-->>Application: active snapshot
  Application->>JsonAiDefinitionSerializer: Serialize(snapshot)
  JsonAiDefinitionSerializer-->>Application: JSON for application-owned storage
  Application->>JsonAiDefinitionSerializer: Deserialize(loaded JSON)
  JsonAiDefinitionSerializer-->>Application: structurally validated snapshot
  Note over Application: authorize the source for registered instruction slots
  Application->>AiContext: ApplyDefinitions(snapshot)
  Note over AiContext: validate contracts, then atomically publish
  Application->>AiConversation: Configure(config => config.RefreshDefinitions(context))
  Note over AiConversation: adopt current catalog; preserve selections and history
```

Source authorization, loading, storage and update triggering belong to the application. The adapter supplies no file/database repository, encryption, monitoring, polling or conversation restoration. The sample's [InMemoryDefinitionRepository](../../samples/AIComFramework.Samples.Console/Persistence/InMemoryDefinitionRepository.cs) uses the injected serializer to demonstrate consumer-owned persistence. [Program.cs](../../samples/AIComFramework.Samples.Console/Program.cs) saves, edits, applies and executes updated content, then refreshes an existing conversation.

Deserialization grants no instruction authority. ApplyDefinitions is an explicit application decision to use an authorized source for registered roles/rules. Their text remains instruction content; context text and provenance remain untrusted data rendered as user messages. External content cannot introduce executable behavior, change trust categories, create application CLR types or replace runtime services. This does not guarantee protection against semantic prompt injection. Exports and JSON contain sensitive prompt/data content. See [Trust](Trust.md).

[DefinitionSerializationTests](../../tests/AIComFramework.Tests.Integration/Serialization/DefinitionSerializationTests.cs) cover round trips, strict JSON/structure checks, contract compatibility, source trust categories, instance isolation, typed selection, DI and in-flight/refresh boundaries. [DefinitionRefreshTests](../../tests/AIComFramework.Tests.Integration/Serialization/DefinitionRefreshTests.cs) cover successive updates, late selections, failed updates, request-local overrides, switching back to updated domains and transactional configuration.
