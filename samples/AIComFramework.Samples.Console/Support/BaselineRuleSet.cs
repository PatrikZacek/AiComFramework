using AIComFramework.Abstractions.Rules;

namespace AIComFramework.Samples.Console.Support;

/// <summary>Reusable response conventions.</summary>
public sealed class BaselineRuleSet : AiRuleSet
{
    /// <summary>Defines concise response behavior.</summary>
    public BaselineRuleSet() : base("baseline", [new AiRule("concise", "Give concise, actionable answers.")]) { }
}
