using AIComFramework.Abstractions.Trust;

namespace AIComFramework.Core.Composition;

/// <summary>Operation-local conversation overrides, without provider execution or history mutation capabilities.</summary>
public sealed class AiRequestConfiguration
{
    private readonly AiPromptComposer _composer;

    internal AiRequestConfiguration(AiPromptComposer composer) => _composer = composer;

    /// <summary>Replaces the role for this request only.</summary>
    public AiRequestConfiguration WithRole(AiRole role)
    {
        _composer.WithRole(role);
        return this;
    }

    /// <summary>Adds registered rules for this request only.</summary>
    public AiRequestConfiguration WithRules(AiRuleSet rules)
    {
        _composer.WithRules(rules);
        return this;
    }

    /// <summary>Adds one registered rule for this request only.</summary>
    public AiRequestConfiguration WithRule(AiRule rule)
    {
        _composer.WithRule(rule);
        return this;
    }

    /// <summary>Selects registered reusable data for this request only.</summary>
    public AiRequestConfiguration WithContext(AiContextDefinition definition)
    {
        _composer.WithContext(definition);
        return this;
    }

    /// <summary>Adds explicit developer-controlled instructions for this request.</summary>
    public AiRequestConfiguration AddContext(Trusted value)
    {
        _composer.AddContext(value);
        return this;
    }

    /// <summary>Adds runtime data for this request.</summary>
    public AiRequestConfiguration AddContext(Untrusted value)
    {
        _composer.AddContext(value);
        return this;
    }

    /// <summary>Removes the active role for this request only.</summary>
    public AiRequestConfiguration ClearRole()
    {
        _composer.ClearRole();
        return this;
    }

    /// <summary>Removes active rules and rule sets so an alternative can be selected for this request.</summary>
    public AiRequestConfiguration ClearRules()
    {
        _composer.ClearRules();
        return this;
    }

    /// <summary>Removes reusable data and runtime contexts for this request only.</summary>
    public AiRequestConfiguration ClearContexts()
    {
        _composer.ClearContexts();
        return this;
    }
}
