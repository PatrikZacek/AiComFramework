namespace AIComFramework.Abstractions.Roles;

/// <summary>Immutable, reusable behavioral instructions, not an authorization principal.</summary>
public class AiRole
{
    /// <summary>Creates a role with a stable metadata identifier and application instructions.</summary>
    public AiRole(string id, string instructions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(instructions);
        Id = id;
        Instructions = instructions;
    }

    /// <summary>Gets the stable identifier used for validation and snapshots.</summary>
    public string Id { get; }

    /// <summary>Gets the trusted application instructions.</summary>
    public string Instructions { get; }
}
