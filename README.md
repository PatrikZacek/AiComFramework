# AIComFramework

AIComFramework is a .NET library for defining AI behavior once and composing each request explicitly. It replaces scattered prompt assembly with typed domain catalogs, reusable roles/rules, explicit trust values and independently managed conversations.

## Build and run

Requires .NET 10 (SDK baseline 10.0.302). From the solution directory:

```sh
dotnet restore --locked-mode
dotnet build --no-restore
dotnet test --no-build --no-restore
dotnet run --project samples/AIComFramework.Samples.Console --no-build
```

Reference `src/AIComFramework.Core/AIComFramework.Core.csproj` from your application. No AIComFramework package has been published. Core uses Microsoft.Extensions.AI 10.11.0 and Microsoft.Extensions.DependencyInjection.Abstractions 10.0.13; transitive versions are recorded in package lock files. The executable sample and DI integration tests use Microsoft.Extensions.DependencyInjection 10.0.0.

## Define a domain

A context is a reusable catalog, not a conversation or Unit of Work. The sample contains complete Support and Billing domain definitions. For example:

```csharp
using AIComFramework.Core.Contexts;

public sealed class SupportAiContext : AiContext
{
    public SupportAgentRole SupportAgent { get; } = new();
    public BaselineRuleSet Baseline { get; } = new();
    public ReturnsRuleSet Returns { get; }

    public SupportAiContext() => Returns = new(Baseline);

    protected override void OnConfiguring(AiContextBuilder builder) =>
        builder.AddRole(SupportAgent)
            .AddRuleSet(Baseline)
            .AddRuleSet(Returns);
}
```

The referenced role and rule-set classes are in [the executable sample](samples/AIComFramework.Samples.Console/Support). Their constructors pass immutable instructions to `AiRole` and `AiRuleSet`. `AiRule` represents a single reusable instruction. `AiContextDefinition` optionally registers reusable untrusted data using `builder.AddContext(definition)` and explicit `WithContext(definition)` selection.

## Compose one request

Given the sample context and an application-supplied `IChatClient client`:

```csharp
using AIComFramework.Abstractions.Trust;

var support = new SupportAiContext();
var composer = support.Compose(client)
    .WithRole(support.SupportAgent)
    .WithRules(support.Baseline)
    .WithRules(support.Returns)
    .AddContext(Untrusted.From("""{"orderId":42}""", source: "order-record"))
    .Prompt("How do I return my order?");

var request = composer.Build();
Console.WriteLine(request); // Counts only.
var response = await composer.SendAsync(cancellationToken);
Console.WriteLine(response.Text);
```

Only selected definitions are included. Rules deduplicate by reference in first-selection order. Distinct rules with a colliding ID fail registration; selected rules in the same optional exclusive group conflict. Natural-language contradictions cannot be detected automatically.

`request.Messages` exposes sensitive content for deliberate inspection. The framework does not log it. These are framework-rendered messages before any SDK structured-output or provider middleware enrichment.

## Dependency injection and conversations

The sample registers a deterministic `DemoChatClient`; an application registers its real implementation of Microsoft's `IChatClient`:

```csharp
using AIComFramework.Core.Conversations;
using AIComFramework.Core.DependencyInjection;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddSingleton<IChatClient, DemoChatClient>();
services.AddAIComFramework()
    .AddAiContext<SupportAiContext>()
    .AddAiContext<BillingAiContext>();
using var provider = services.BuildServiceProvider();
using var scope = provider.CreateScope();

var support = scope.ServiceProvider.GetRequiredService<SupportAiContext>();
var billing = scope.ServiceProvider.GetRequiredService<BillingAiContext>();
var factory = scope.ServiceProvider.GetRequiredService<AiConversationFactory>();
var conversation = factory.Create(support, config => config
    .WithRole(support.SupportAgent).WithRules(support.Baseline));

await conversation.SendAsync("I need to return my order.");
await conversation.SendAsync("It arrived yesterday.");

conversation.Configure(config => config
    .UseContext(billing)
    .WithRole(billing.Specialist)
    .WithRules(billing.Invoices));

var preview = conversation.Preview("Can you check my invoice?");
await conversation.SendAsync("Can you check my invoice?");
var state = conversation.GetState(); // All three completed turns.
```

`UseContext` clears active roles, rules and reusable data. It preserves full history. The first billing request has no eligible support turns. Returning to Support makes its previous turns eligible again; a domain is identified by the context type's full name, not by its instance or a tenant identity.

Sharing is explicit and per request:

```csharp
await conversation.SendAsync("Use the first support exchange.",
    history: new HistoryOptions(
        selector: new RecentHistory(5),
        sharedTurnIds: [1]));
```

Only original user prompts and assistant replies are replayed; old system instructions and runtime data messages are excluded. Replies can still contain facts derived from earlier context. Applications must judge the content they explicitly share.

`EligibleTurnIds` and `TransmittedTurnIds` record separate decisions. The default selector retains the newest 20 eligible turns. `RecentHistory(0)` sends none; `IHistorySelector` permits custom deterministic selection within eligibility.

Conversation sends, previews and configuration reject overlapping operations. Configuration changes are transactional; failed/canceled sends add no turn. `GetState()` remains available during a send and returns committed state. A preview is not a reservation: a subsequent send builds a new snapshot. Local `configureRequest` overrides do not persist, and the `SendAsync` prompt argument remains the final user message.

## Structured responses and providers

`await composer.SendAsync<YourDto>(cancellationToken: token)` uses the SDK structured-output extension and returns `ChatResponse<YourDto>`. Read `Result` for the typed value and `Usage` for actual provider-reported usage. Deserialization is validated before recording a typed conversation turn. Pass `useJsonSchema: false` for the SDK JSON-only fallback; schema support depends on the provider.

Configure model/provider defaults in your registered client or its middleware. The framework neither replaces `IChatClient` nor owns its lifetime. Provider exceptions and cancellation propagate. No real API credentials are needed by the tests or sample.

## Trust and limitations

Raw prompts and `Untrusted.From` values remain user data in JSON envelopes with provenance. `Trusted.From` is an explicit application-controlled instruction path. JSON escaping and typed wrappers prevent some accidental composition errors; they do not eliminate semantic prompt injection.

This implementation supports complete text requests and text conversation responses. Streaming, tools, multimodal conversation history, durable storage, runtime restoration, semantic selection and token budgeting are deferred. State is exportable as JSON data, but that is not a durable persistence implementation. Full history is unbounded in memory; applications should bound session lifetimes. Context type names and definition IDs must be versioned/resolved by any future storage adapter. Metadata IDs should not contain secrets, and object destructuring/JSON serialization exposes sensitive snapshot properties even though `ToString()` is redacted.

No measured token, cost or quality improvement is claimed.

The solution separates provider-independent definition/snapshot contracts in `AIComFramework.Abstractions` from registration, composition and conversation behavior in `AIComFramework.Core`. Test projects exercise isolated invariants and cross-component SDK/DI behavior. The executable sample demonstrates the public API with two domains. All projects enable nullable analysis, XML documentation and warnings as errors.
