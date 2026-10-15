using AIComFramework.Abstractions.Composition;
using AIComFramework.Abstractions.Trust;
using AIComFramework.Tests.Fixtures;

namespace AIComFramework.Tests.Unit.Composition;

/// <summary>Verifies explicit selection, ordering and snapshot isolation.</summary>
public sealed class CompositionTests
{
    /// <summary>Only selected components are composed in documented order.</summary>
    [Fact]
    public void CompositionIsSelectiveAndDeterministic()
    {
        var context = new SupportContext();
        var composer = context.Compose(new TestChatClient()).WithRole(context.Agent)
            .WithRules(context.Standard).WithRules(context.ReturnRules).WithRules(context.Standard)
            .AddContext(Trusted.From("Trusted details")).AddContext(Untrusted.From("record", "orders")).Prompt("question");
        var first = composer.Build();
        var second = composer.Build();
        Assert.Equal<MessageSnapshot>(first.Messages, second.Messages);
        Assert.Equal("Support instructions\n\nBe accurate\n\nExplain returns\n\nTrusted details", first.Messages[0].Text);
        Assert.Equal<string>(["baseline", "returns"], first.Configuration.RuleIds);
        Assert.Equal([MessageKind.System, MessageKind.User, MessageKind.User], first.Messages.Select(message => message.Kind));
        Assert.DoesNotContain(first.Messages, message => message.Text.Contains("NEVER_AUTO_INCLUDE"));
    }

    /// <summary>Builder changes never mutate prior snapshots or shared definitions.</summary>
    [Fact]
    public void SnapshotsAreIsolated()
    {
        var context = new SupportContext();
        var composer = context.Compose(new TestChatClient()).WithRole(context.Agent).Prompt("secret");
        var snapshot = composer.Build();
        composer.WithRole(context.Specialist).WithRule(context.Baseline).Prompt("other");
        Assert.Equal("support", snapshot.Configuration.RoleId);
        Assert.Empty(snapshot.Configuration.RuleIds);
        Assert.Contains("secret", snapshot.Messages[^1].Text);
        Assert.Empty(context.Compose(new TestChatClient()).Prompt("new").Build().Configuration.RuleIds);
        Assert.DoesNotContain("secret", snapshot.ToString());
        Assert.DoesNotContain("secret", snapshot.Messages[^1].ToString());
    }

    /// <summary>Explicit conflicts fail without partial selection.</summary>
    [Fact]
    public void ConflictingRulesFailAtomically()
    {
        var context = new SupportContext();
        var composer = context.Compose(new TestChatClient()).WithRule(context.Short).Prompt("x");
        Assert.Throws<ArgumentException>(() => composer.WithRule(context.Long));
        Assert.Equal<string>(["short"], composer.Build().Configuration.RuleIds);
    }

    /// <summary>A request must contain an explicitly supplied user prompt.</summary>
    [Fact]
    public void MissingPromptFails() => Assert.Throws<InvalidOperationException>(() => new SupportContext().Compose(new TestChatClient()).Build());
}
