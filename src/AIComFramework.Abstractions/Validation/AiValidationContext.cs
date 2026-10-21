using System.Collections.Immutable;
using AIComFramework.Abstractions.Composition;

namespace AIComFramework.Abstractions.Validation;

/// <summary>Immutable metadata deliberately excluding prompt bodies and conversation history.</summary>
/// <param name="CorrelationId">Unique identifier for this execution.</param>
/// <param name="Configuration">Effective configuration IDs captured before execution.</param>
/// <param name="ReferenceData">Application-provided reference strings only.</param>
public sealed record AiValidationContext(Guid CorrelationId, ConfigurationSnapshot Configuration,
    ImmutableDictionary<string, string> ReferenceData);
