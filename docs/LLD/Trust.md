# Trusted and untrusted content

Trusted.From is an explicit developer-controlled instruction path. Untrusted.From and raw Prompt strings always represent runtime data; there is no implicit string-to-Trusted conversion. Both wrappers retain immutable strings and redact their default diagnostic representation.

JsonUntrustedRenderer serializes a fixed envelope with kind, source and text fields using System.Text.Json's default escaping. Source is encoded as data too. Tests round-trip quotes, closing delimiters, newlines, control characters, Unicode and attempted role declarations. The contract preserves data/provenance boundaries in the serialized representation, not the model's interpretation.

Reusable AiContextDefinition data remains Untrusted. Explicit Trusted additions join the system message; runtime data is emitted as user messages. Role/rule instructions initially come from compiled definitions and may be replaced by an application-authorized definition snapshot. Their registered instruction slots remain system-message inputs. The renderer is replaceable through IUntrustedRenderer; the default DI registration does not replace an application registration.

Deserialization validates data without authorizing it or activating definitions. Calling ApplyDefinitions is the application's explicit decision to accept the source for registered instruction slots. Contract validation restricts identities, exclusive groups and ordered membership; it does not establish that natural-language instructions are safe. A snapshot cannot activate CLR types, replace services or promote reusable context data/provenance to Trusted. Updated reusable data still passes through the untrusted renderer as user content. See [Definition serialization and updates](DefinitionSerialization.md).

Definition exports and serialized JSON contain instruction text and reusable data. They are sensitive content alongside request/history snapshots. Source authorization, storage access and deliberate inspection belong to the application; serialization supplies no encryption, redaction or authorization policy.

Model output remains assistant text. It cannot invoke Configure, resolve domain definitions, alter permissions or become a trusted instruction through the normal path. Unsupported elevated provider message roles are rejected when recording conversations.

No delimiter scheme can guarantee protection against semantic prompt injection. Explicitly shared replies can repeat sensitive facts from prior contextual data. Applications control selection and remain responsible for the information they choose to disclose.
