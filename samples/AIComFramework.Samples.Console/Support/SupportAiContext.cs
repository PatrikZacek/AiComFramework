using AIComFramework.Core.Contexts;

namespace AIComFramework.Samples.Console.Support;

/// <summary>Reusable support catalog.</summary>
public sealed class SupportAiContext : AiContext
{
    /// <inheritdoc />
    public override string DomainId => "sample.support";

    /// <summary>Creates composed return definitions.</summary>
    public SupportAiContext() => Returns = new(Baseline);

    /// <summary>Gets the support role.</summary>
    public SupportAgentRole SupportAgent { get; } = new();

    /// <summary>Gets baseline rules.</summary>
    public BaselineRuleSet Baseline { get; } = new();

    /// <summary>Gets return rules.</summary>
    public ReturnsRuleSet Returns { get; }

    /// <inheritdoc />
    protected override void OnConfiguring(AiContextBuilder builder) =>
        builder.AddRole(SupportAgent).AddRuleSet(Baseline).AddRuleSet(Returns);
}
