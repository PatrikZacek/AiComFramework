using AIComFramework.Abstractions.Trust;
using AIComFramework.Core.Conversations;
using AIComFramework.Core.DependencyInjection;
using AIComFramework.Samples.Console.Billing;
using AIComFramework.Samples.Console.Providers;
using AIComFramework.Samples.Console.Support;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddSingleton<IChatClient, DemoChatClient>();
services.AddAIComFramework().AddAiContext<SupportAiContext>().AddAiContext<BillingAiContext>();
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
Console.WriteLine((await conversation.SendAsync("It arrived yesterday.")).Text);
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
