using AIComFramework.Abstractions.Rules;

namespace AIComFramework.Tests.Fixtures;

internal sealed class SerializableSupportRules(AiRule rule, AiRuleSet baseline) : AiRuleSet("returns", [rule], baseline);
