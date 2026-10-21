using AIComFramework.Abstractions.Contexts;
using AIComFramework.Abstractions.Trust;
using AIComFramework.Tests.Shared.Fixtures;

namespace AIComFramework.Tests.Unit.Contexts;

/// <summary>Verifies opt-in reusable data and provenance.</summary>
public sealed class ReusableContextTests
{
    /// <summary>Catalog data is transmitted only when selected and remains untrusted.</summary>
    [Fact]
    public void ReusableDataRequiresExplicitSelection()
    {
        var definition = new AiContextDefinition("catalog", Untrusted.From("reference facts", "catalog-v1"));
        var context = new CallbackContext(builder => builder.AddContext(definition));
        var client = new TestChatClient();
        Assert.Single(context.Compose(client).Prompt("question").Build().Messages);
        var request = context.Compose(client).WithContext(definition).WithContext(definition).Prompt("question").Build();
        Assert.Equal(2, request.Messages.Length);
        Assert.Equal<string>(["catalog"], request.Configuration.ContextIds);
        Assert.Contains("catalog-v1", request.Messages[0].Text);
        Assert.Equal(AIComFramework.Abstractions.Composition.MessageKind.User, request.Messages[0].Kind);
        Assert.Throws<ArgumentException>(() => context.Compose(client).WithContext(new("catalog", Untrusted.From("replacement"))));
    }
}
