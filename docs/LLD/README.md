# Low-level design

These documents describe implemented behavior and invariants. The [architecture overview](../Architecture.md) describes module boundaries and the overall lifecycle. Update the relevant design document when changing its implementation.

| Document | Scope |
| --- | --- |
| [Domain contexts](DomainContexts.md) | Lazy registration, contract identity, domain IDs and active catalog publication |
| [Roles and rule sets](RolesAndRules.md) | Flattening, ordering, deduplication and exclusive groups |
| [Definition serialization and updates](DefinitionSerialization.md) | Portable snapshots, strict JSON, contract validation, publication and application-owned storage |
| [Prompt composition](PromptComposition.md) | Captured content, selections, rendering order and immutable requests |
| [Conversation state](ConversationState.md) | Operation gates, transactional configuration, definition refresh and turn commits |
| [History selection](HistorySelection.md) | Domain eligibility, explicit sharing, strategy validation and replay |
| [Provider integration](ProviderIntegration.md) | IChatClient, structured output, cancellation and SDK metadata |
| [Result validation](ResultValidation.md) | Structural diagnostics, domain findings, DI validators and commit eligibility |
| [Trust](Trust.md) | Instruction/data boundaries, source authorization and sensitive content |
