using AIComFramework.Core.Composition;
using AIComFramework.Core.Contexts;

namespace AIComFramework.Core.Conversations;

/// <summary>Builds the next conversation configuration transactionally.</summary>
public sealed class ConversationConfigurationBuilder
{
    internal ConversationConfigurationBuilder(DefinitionSelection selection) => Selection = selection;
    internal DefinitionSelection Selection { get; private set; }

    /// <summary>Selects a domain and clears roles, rules and reusable data, even for the same context.</summary>
    public ConversationConfigurationBuilder UseContext(AiContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        Selection = DefinitionSelection.Empty(context.Catalog);
        return this;
    }

    /// <summary>Replaces the selected role with a registered role.</summary>
    public ConversationConfigurationBuilder WithRole(AiRole role)
    {
        Selection = Selection.WithRole(role);
        return this;
    }

    /// <summary>Adds registered rules in selection order.</summary>
    public ConversationConfigurationBuilder WithRules(AiRuleSet rules)
    {
        Selection = Selection.WithRules(rules);
        return this;
    }

    /// <summary>Adds an individually registered rule.</summary>
    public ConversationConfigurationBuilder WithRule(AiRule rule)
    {
        Selection = Selection.WithRule(rule);
        return this;
    }

    /// <summary>Selects reusable domain data for subsequent turns.</summary>
    public ConversationConfigurationBuilder WithContext(AiContextDefinition definition)
    {
        Selection = Selection.WithContext(definition);
        return this;
    }

    /// <summary>Clears selected reusable data without changing roles or rules.</summary>
    public ConversationConfigurationBuilder ClearContexts()
    {
        Selection = Selection with { Contexts = [] };
        return this;
    }

    /// <summary>Clears selected rules and sets while preserving the role.</summary>
    public ConversationConfigurationBuilder ClearRules()
    {
        Selection = Selection with { Rules = [], RuleSets = [] };
        return this;
    }

    /// <summary>Clears the selected role while preserving rules.</summary>
    public ConversationConfigurationBuilder ClearRole()
    {
        Selection = Selection with { Role = null };
        return this;
    }
}
