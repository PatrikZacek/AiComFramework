using AIComFramework.Abstractions.Roles;
using AIComFramework.Abstractions.Rules;
using AIComFramework.Core.Contexts;
using AIComFramework.Tests.Fixtures;

namespace AIComFramework.Tests.Unit.Contexts;

/// <summary>Verifies typed domain membership and immutable catalog initialization.</summary>
public sealed class DomainTests
{
    /// <summary>A failed set registration cannot publish some of its rules if application code catches the error.</summary>
    [Fact]
    public void SetRegistrationIsAtomic()
    {
        var existing = new AiRule("existing", "original");
        var candidate = new AiRule("candidate", "candidate");
        var invalid = new AiRuleSet("invalid", [candidate, new AiRule("existing", "conflict")]);
        var context = new CallbackContext(builder =>
        {
            builder.AddRule(existing);
            Assert.Throws<ArgumentException>(() => builder.AddRuleSet(invalid));
        });
        var composer = context.Compose(new TestChatClient());
        Assert.Throws<ArgumentException>(() => composer.WithRule(candidate));
        Assert.Throws<ArgumentException>(() => composer.WithRules(invalid));
    }

    /// <summary>Distinct same-ID definitions fail predictably.</summary>
    [Fact]
    public void ConflictingRegistrationFails()
    {
        var context = new CallbackContext(builder => builder.AddRole(new("same", "first")).AddRole(new("same", "second")));
        Assert.Throws<ArgumentException>(context.Validate);
    }

    /// <summary>Repeated references are idempotent, and captured builders cannot change published catalogs.</summary>
    [Fact]
    public void CatalogIsDetachedFromBuilder()
    {
        AiContextBuilder? captured = null;
        var role = new AiRole("role", "instructions");
        var context = new CallbackContext(builder => { captured = builder; builder.AddRole(role).AddRole(role); });
        context.Validate();
        var late = new AiRole("late", "late instructions");
        captured!.AddRole(late);
        Assert.Throws<ArgumentException>(() => context.Compose(new TestChatClient()).WithRole(late));
        Assert.Equal("role", context.Compose(new TestChatClient()).WithRole(role).Prompt("x").Build().Configuration.RoleId);
    }

    /// <summary>Concurrent consumers initialize a shared context once.</summary>
    [Fact]
    public async Task CatalogInitializesOnce()
    {
        var calls = 0;
        var context = new CallbackContext(_ => Interlocked.Increment(ref calls));
        await Task.WhenAll(Enumerable.Range(0, 32).Select(_ => Task.Run(context.Validate)));
        Assert.Equal(1, calls);
    }

    /// <summary>Domain membership requires explicit reference registration.</summary>
    [Fact]
    public void SharedDefinitionsRequireRegistration()
    {
        var support = new SupportContext();
        var billing = new BillingContext(support.Baseline);
        var client = new TestChatClient();
        Assert.Throws<ArgumentException>(() => billing.Compose(client).WithRole(support.Agent));
        Assert.Throws<ArgumentException>(() => billing.Compose(client).WithRule(new("baseline", "Be accurate")));
        Assert.Equal<string>(["baseline"], billing.Compose(client).WithRule(support.Baseline).Prompt("x").Build().Configuration.RuleIds);
    }

    /// <summary>Rule-set inputs are copied, composed and deduplicated deterministically.</summary>
    [Fact]
    public void RuleSetsCopyAndCompose()
    {
        var first = new AiRule("first", "one");
        var second = new AiRule("second", "two");
        var source = new List<AiRule> { first };
        var baseline = new AiRuleSet("base", source);
        source.Clear();
        var combined = new AiRuleSet("combined", [second, first], baseline);
        Assert.Equal<AiRule>([second, first], combined.Rules);
        Assert.Throws<ArgumentException>(() => new AiRuleSet("bad", [first, new("first", "different")]));
        Assert.Throws<ArgumentException>(() => new AiRuleSet("bad", [new("a", "A", "group"), new("b", "B", "group")]));
    }

    /// <summary>Empty instruction definitions are rejected early.</summary>
    [Fact]
    public void InvalidDefinitionsFailEarly()
    {
        Assert.Throws<ArgumentException>(() => new AiRole("", "instructions"));
        Assert.Throws<ArgumentException>(() => new AiRule("rule", " "));
        Assert.Throws<ArgumentException>(() => new AiRuleSet(" ", []));
    }
}
