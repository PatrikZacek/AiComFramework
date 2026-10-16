using AIComFramework.Abstractions.Trust;
using AIComFramework.Core.Conversations;
using AIComFramework.Core.DependencyInjection;
using AIComFramework.Samples.Console.Billing;
using AIComFramework.Samples.Console.Providers;
using AIComFramework.Samples.Console.Support;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using AIComFramework.Serialization.Json.DependencyInjection;
using AIComFramework.Samples.Console.Persistence;

var services = new ServiceCollection();
services.AddSingleton<IChatClient, DemoChatClient>();
services.AddAIComFramework().AddAiContext<SupportAiContext>().AddAiContext<BillingAiContext>();
services.AddAiComJsonSerialization();
services.AddSingleton<InMemoryDefinitionRepository>();
using var provider = services.BuildServiceProvider(new ServiceProviderOptions
{
    ValidateScopes = true,
    ValidateOnBuild = true
});
using var scope = provider.CreateScope();
var support = scope.ServiceProvider.GetRequiredService<SupportAiContext>();
var billing = scope.ServiceProvider.GetRequiredService<BillingAiContext>();
var client = scope.ServiceProvider.GetRequiredService<IChatClient>();

var composer = support.Compose(client).WithRole(support.SupportAgent).WithRules(support.Returns)
    .AddContext(Untrusted.From("""{"orderId":42}""", "order-record"))
    .Prompt("How can I return this order?");
Console.WriteLine(composer.Build()); // Content-free diagnostics.
Console.WriteLine((await composer.SendAsync()).Text);

var factory = scope.ServiceProvider.GetRequiredService<AiConversationFactory>();
var conversation = factory.Create(support, config => config.WithRole(support.SupportAgent).WithRules(support.Baseline));
Console.WriteLine((await conversation.SendAsync("I need to return order 42.")).Text);

// Simulate an authorized external content edit. No content revision or new CLR definition is needed.
var repository = scope.ServiceProvider.GetRequiredService<InMemoryDefinitionRepository>();
var definitions = support.ExportDefinitions();
repository.Save(definitions);
var storedDefinitions = repository.Load();
repository.Save(storedDefinitions with
{
    Roles = [storedDefinitions.Roles[0] with { Instructions = "Help with returns. Explain the updated 60-day return policy." }]
});
support.ApplyDefinitions(repository.Load());
var updated = support.Compose(client).WithRole<SupportAgentRole>().WithRules<ReturnsRuleSet>().Prompt("What changed?");
Console.WriteLine($"Updated request: {updated.Build().Messages[0].Text}");
Console.WriteLine($"Previously composed request: {composer.Build().Messages[0].Text}");
Console.WriteLine((await updated.SendAsync()).Text);
conversation.Configure(config => config.RefreshDefinitions(support));

Console.WriteLine((await conversation.SendAsync("It arrived yesterday.")).Text);
var refreshedTurn = conversation.GetState().Turns[^1];
if (!refreshedTurn.Request.Messages[0].Text.Contains("60-day", StringComparison.Ordinal) ||
    !refreshedTurn.Request.TransmittedTurnIds.Contains(1))
    throw new InvalidOperationException("Definition refresh must update instructions and preserve eligible history.");
conversation.Configure(config => config.UseContext(billing).WithRole(billing.Specialist).WithRules(billing.Invoices));
Console.WriteLine((await conversation.SendAsync("Can you check my invoice?")).Text);

var state = conversation.GetState();
var latest = state.Turns[^1].Request;
Console.WriteLine($"Recorded turns: {state.Turns.Length}; history sent to billing: {latest.TransmittedTurnIds.Length}");
if (state.Turns.Length != 3 || latest.TransmittedTurnIds.Length != 0)
    throw new InvalidOperationException("Domain isolation failed.");

// Explicit inspection of synthetic content only. Real requests may contain private records.
foreach (var message in latest.Messages)
{
    Console.WriteLine($"{message.Kind}: {message.Text}");
}
await conversation.SendAsync("Use my first support exchange.", history: new HistoryOptions(sharedTurnIds: [1]));
Console.WriteLine($"Explicitly shared history IDs: {string.Join(", ", conversation.GetState().Turns[^1].Request.TransmittedTurnIds)}");
