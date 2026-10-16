using AIComFramework.Core.Composition;
using AIComFramework.Core.Contexts;

namespace AIComFramework.Core.Conversations;

/// <summary>Builds the next conversation configuration transactionally.</summary>
public sealed class ConversationConfigurationBuilder
{
    /// <summary>Selects the unique registered definition assignable to the application type.</summary>
    public ConversationConfigurationBuilder WithRole<T>() where T : AiRole =>
        WithRole(Selection.Catalog.Select<T, AiRole>(Selection.Catalog.Roles));

    /// <summary>Selects the unique registered definition assignable to the application type.</summary>
    public ConversationConfigurationBuilder WithRules<T>() where T : AiRuleSet =>
        WithRules(Selection.Catalog.Select<T, AiRuleSet>(Selection.Catalog.RuleSets));

    /// <summary>Selects the unique registered definition assignable to the application type.</summary>
    public ConversationConfigurationBuilder WithRule<T>() where T : AiRule =>
        WithRule(Selection.Catalog.Select<T, AiRule>(Selection.Catalog.Rules));

    /// <summary>Selects the unique registered definition assignable to the application type.</summary>
    public ConversationConfigurationBuilder WithContext<T>() where T : AiContextDefinition =>
        WithContext(Selection.Catalog.Select<T, AiContextDefinition>(Selection.Catalog.Contexts));

    internal ConversationConfigurationBuilder(DefinitionSelection selection) => Selection = selection;
    internal DefinitionSelection Selection { get; private set; }

    /// <summary>Captures this context's latest content while preserving active selections and conversation history.</summary>
    /// <remarks>Use between turns. The context must own the same registered definition objects as the active domain.</remarks>
    public ConversationConfigurationBuilder RefreshDefinitions(AiContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var catalog = context.Catalog;
        if (!ReferenceEquals(catalog.ContractIdentity, Selection.Catalog.ContractIdentity))
            throw new ArgumentException("Refresh requires the original domain context instance.", nameof(context));
        Selection = Selection with { Catalog = catalog };
        return this;
    }

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
