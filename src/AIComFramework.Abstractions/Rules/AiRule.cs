namespace AIComFramework.Abstractions.Rules;

/// <summary>An immutable application instruction optionally exclusive with other rules.</summary>
public class AiRule
{
    /// <summary>Creates a rule. Only one distinct selected rule may occupy an exclusive group.</summary>
    public AiRule(string id, string instructions, string? exclusiveGroup = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(instructions);
        if (exclusiveGroup is not null) ArgumentException.ThrowIfNullOrWhiteSpace(exclusiveGroup);
        Id = id;
        Instructions = instructions;
        ExclusiveGroup = exclusiveGroup;
    }

    /// <summary>Gets the stable identifier.</summary>
    public string Id { get; }

    /// <summary>Gets the trusted instruction text.</summary>
    public string Instructions { get; }

    /// <summary>Gets the optional mutually exclusive configuration group.</summary>
    public string? ExclusiveGroup { get; }
}
