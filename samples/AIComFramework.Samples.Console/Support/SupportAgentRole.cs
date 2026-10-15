using AIComFramework.Abstractions.Roles;

namespace AIComFramework.Samples.Console.Support;

/// <summary>Reusable support behavior.</summary>
public sealed class SupportAgentRole : AiRole
{
    /// <summary>Defines support responsibilities.</summary>
    public SupportAgentRole() : base("support-agent", "Help customers resolve orders and returns. Do not invent order facts.") { }
}
