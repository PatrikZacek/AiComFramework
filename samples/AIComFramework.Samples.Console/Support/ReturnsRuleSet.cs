using AIComFramework.Abstractions.Rules;

namespace AIComFramework.Samples.Console.Support;

/// <summary>Return workflow with a shared baseline.</summary>
public sealed class ReturnsRuleSet : AiRuleSet
{
    /// <summary>Reuses baseline rules without duplicating definitions.</summary>
    public ReturnsRuleSet(BaselineRuleSet baseline) : base("returns",
        [new AiRule("return-policy", "Ask for the order date before explaining return eligibility.")], baseline) { }
}
