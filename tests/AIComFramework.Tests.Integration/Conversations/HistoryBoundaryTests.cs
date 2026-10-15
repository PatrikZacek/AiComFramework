using System.Text.Json;
using AIComFramework.Abstractions.Conversations;
using AIComFramework.Core.Conversations;
using AIComFramework.Core.Trust;
using AIComFramework.Tests.Fixtures;

namespace AIComFramework.Tests.Integration.Conversations;

/// <summary>Verifies validation of custom strategies and exported state.</summary>
public sealed class HistoryBoundaryTests
{
    /// <summary>Identical full type names in different assemblies must not share history implicitly.</summary>
    [Fact]
    public async Task AssemblyIdentitySeparatesSameNamedDomains()
    {
        var first = DynamicDomainContext.Create("SupportLibrary");
        var second = DynamicDomainContext.Create("BillingLibrary");
        Assert.Equal(first.GetType().FullName, second.GetType().FullName);
        Assert.NotEqual(first.DomainId, second.DomainId);
        var conversation = new AiConversationFactory(new TestChatClient(), new JsonUntrustedRenderer()).Create(first);
        await conversation.SendAsync("private");
        conversation.Configure(config => config.UseContext(second));
        await conversation.SendAsync("different domain");
        Assert.Empty(conversation.GetState().Turns[1].Request.EligibleTurnIds);
        Assert.DoesNotContain(conversation.GetState().Turns[1].Request.Messages, message => message.Text.Contains("private"));
    }

    /// <summary>A strategy cannot add ineligible turns, duplicate IDs or reverse order.</summary>
    [Theory]
    [InlineData(new long[] { 99 })]
    [InlineData(new long[] { 1, 1 })]
    [InlineData(new long[] { 2, 1 })]
    public async Task InvalidStrategyDoesNotSend(long[] ids)
    {
        var client = new TestChatClient();
        var conversation = new AiConversationFactory(client, new JsonUntrustedRenderer()).Create(new SupportContext());
        await conversation.SendAsync("one");
        await conversation.SendAsync("two");
        await Assert.ThrowsAsync<InvalidOperationException>(() => conversation.SendAsync("invalid", history: new(new FixedHistorySelector(ids))));
        Assert.Equal(2, client.Requests.Count);
        Assert.Equal(2, conversation.GetState().Turns.Length);
    }

    /// <summary>Unknown sharing IDs and attempts to select another domain fail before execution.</summary>
    [Fact]
    public async Task DomainEligibilityCannotBeBypassedBySelector()
    {
        var client = new TestChatClient();
        var support = new SupportContext();
        var conversation = new AiConversationFactory(client, new JsonUntrustedRenderer()).Create(support);
        await conversation.SendAsync("private");
        conversation.Configure(config => config.UseContext(new BillingContext()));
        await Assert.ThrowsAsync<InvalidOperationException>(() => conversation.SendAsync("invalid", history: new(new FixedHistorySelector(1))));
        await Assert.ThrowsAsync<ArgumentException>(() => conversation.SendAsync("invalid", history: new(sharedTurnIds: [99])));
        Assert.Single(client.Requests);
        conversation.Configure(config => config.UseContext(support));
        await conversation.SendAsync("returned");
        Assert.Equal<long>([1L], conversation.GetState().Turns[1].Request.TransmittedTurnIds);
    }

    /// <summary>Exported state retains audit data through JSON, without implying runtime restoration.</summary>
    [Fact]
    public async Task StateCanBeSerializedAsData()
    {
        var support = new SupportContext();
        var conversation = new AiConversationFactory(new TestChatClient(), new JsonUntrustedRenderer()).Create(support,
            config => config.WithRole(support.Agent).WithRules(support.Standard));
        await conversation.SendAsync("sensitive question");
        var original = conversation.GetState();
        var restoredData = JsonSerializer.Deserialize<ConversationState>(JsonSerializer.Serialize(original))!;
        Assert.Equal(original.Id, restoredData.Id);
        Assert.Equal(original.Configuration.DomainId, restoredData.Configuration.DomainId);
        Assert.Equal("support", restoredData.Configuration.RoleId);
        Assert.Equal(original.Turns[0].Exchange[^1].Text, restoredData.Turns[0].Exchange[^1].Text);
        Assert.DoesNotContain("sensitive", original.ToString());
        Assert.DoesNotContain("sensitive", original.Turns[0].ToString());
    }
}
