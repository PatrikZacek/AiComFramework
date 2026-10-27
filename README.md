# AIComFramework

AIComFramework is a .NET library for defining AI behavior once and composing each request explicitly. It replaces scattered prompt assembly with typed domain catalogs, reusable roles/rules, explicit trust values and independently managed conversations.

## Packages and installation

The repository prepares three packages for **.NET 10** at version **0.1.0-alpha.1**.
Preparing a package or merging this change does not publish it to NuGet.org. See
[packaging and releases](docs/Packaging.md) for the local feed and publication prerequisites.

| Package | Purpose | Framework dependencies |
| --- | --- | --- |
| `AIComFramework.Abstractions` | Contracts, trust values, roles/rules and snapshots | None |
| `AIComFramework.Core` | Composition, conversations, DI and result validation | Abstractions; Microsoft.Extensions.AI; DI abstractions |
| `AIComFramework.Serialization.Json` | Strict JSON definition serialization and DI registration | Abstractions; DI abstractions |

Core does not depend on the JSON adapter. The JSON adapter does not depend on Core.
Install Core for normal application usage; add the JSON adapter only for definition serialization.
For a locally built feed (replace `<feed>` with the verified packages directory):

```sh
dotnet add package AIComFramework.Core --version 0.1.0-alpha.1 --no-restore
dotnet add package AIComFramework.Serialization.Json --version 0.1.0-alpha.1 --no-restore
dotnet restore --source <feed> --source https://api.nuget.org/v3/index.json
```

Both main packages and portable PDB symbol packages contain commit-specific repository metadata.
Samples and tests are built and tested but never packed. The library is licensed under
the [MIT license](LICENSE); dependencies retain their own licenses.

## Build and run

Requires exactly .NET SDK 10.0.302 and PowerShell 7 for verification scripts. From the solution directory:

```sh
dotnet restore --locked-mode
dotnet build -c Release --no-restore -p:ContinuousIntegrationBuild=true
dotnet test -c Release --no-build --no-restore
dotnet pack -c Release --no-build --no-restore -o artifacts/packages -p:ContinuousIntegrationBuild=true
dotnet run -c Release --project samples/AIComFramework.Samples.Console --no-build
```

For the complete build/test/pack, package inspection, isolated consumer smoke test and
rebuild comparison, run `pwsh -File eng/Build-Verify.ps1`. No live model, API key or
publishing credential is needed. CI runs this on Linux and Windows.

Core uses Microsoft.Extensions.AI 10.11.0 and Microsoft.Extensions.DependencyInjection.Abstractions 10.0.13; transitive versions and content hashes are recorded in committed lock files and CI restores in locked mode. The executable sample and DI integration tests use Microsoft.Extensions.DependencyInjection 10.0.0. Package versioning is centralized in `eng/Versions.props`.

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

`UseContext` clears active roles, rules and reusable data. It preserves full history. The first billing request has no eligible support turns. Returning to Support makes its previous turns eligible again; a domain is identified by its assembly simple name plus the context type's full name (without assembly version), not by its instance or a tenant identity.

Sharing is explicit and per request:

```csharp
await conversation.SendAsync("Use the first support exchange.",
    history: new HistoryOptions(
        selector: new RecentHistory(5),
        sharedTurnIds: [1]));
```

Only original user prompts and assistant replies are replayed; old system instructions and runtime data messages are excluded. Replies can still contain facts derived from earlier context. Applications must judge the content they explicitly share.

`EligibleTurnIds` and `TransmittedTurnIds` record separate decisions. The default selector retains the newest 20 eligible turns. `RecentHistory(0)` sends none; `IHistorySelector` permits custom deterministic selection within eligibility.

Conversation sends, previews and configuration reject overlapping operations. Configuration changes are transactional; failed/canceled sends add no turn. `GetState()` remains available during a send and returns committed state. A preview is not a reservation: a subsequent send builds a new snapshot. Local `configureRequest` overrides use `AiRequestConfiguration`, which cannot execute requests or replace the user prompt. Overrides do not persist. Use `ClearRole()`, `ClearRules()` and `ClearContexts()` to omit active selections for one request; `ClearRules().WithRule(alternative)` can replace an exclusive rule without changing the conversation configuration. Callback builders are operation-local and must not be mutated concurrently.

## Structured responses and providers

`await composer.SendAsync<YourDto>(cancellationToken: token)` uses the SDK structured-output extension and returns `ChatResponse<YourDto>`. Read `Result` for the typed value and `Usage` for actual provider-reported usage. Deserialization is validated before recording a typed conversation turn. Pass `useJsonSchema: false` for the SDK JSON-only fallback; schema support depends on the provider.

Configure model/provider defaults in your registered client or its middleware. The framework neither replaces `IChatClient` nor owns its lifetime. Provider exceptions and cancellation propagate. Cancellation is checked before dispatch and after provider completion, including for clients that return after cancellation. An empty response message list or non-assistant/non-text content cannot be recorded as a completed conversation turn. No real API credentials are needed by the tests or sample.

## Optional result validation

Model execution, JSON parsing and domain validation are separate boundaries. Existing `SendAsync` methods retain their SDK response types and make no domain-validation claim. Use `Expect<T>()` for an explicit result envelope: without validators its domain status is `NotValidated`.

```csharp
using AIComFramework.Core.Validation;
using AIComFramework.Abstractions.Results;
using AIComFramework.Abstractions.Validation;
using AIComFramework.Samples.Console.Validation; // Replace with your application types.

services.AddAIComFramework()
    .AddAiResultValidator<InvoiceAnalysis, InvoiceAmountValidator>()
    .AddAiResultValidator<InvoiceAnalysis, InvoiceCurrencyValidator>();
services.AddSingleton<InvoiceLedger>();

// Resolve from the application's scope after building its provider.
var pipeline = scope.ServiceProvider.GetRequiredService<AiValidationPipeline>();
var result = await billing.Compose(client).Prompt("Extract the invoice.")
    .Expect<InvoiceAnalysis>(pipeline: pipeline)
    .ValidateWith<InvoiceAmountValidator>()
    .ValidateWith<InvoiceCurrencyValidator>()
    .SendAsync();

if (result.HasResult && result.Validation.Status == AiValidationStatus.Valid)
{
    var invoice = result.GetValidatedValue(); // Satisfies configured checks only.
}
// Candidate, RawText, StructuralError and Validation.Findings support explicit fallback/diagnostics.

var contract = new AiResultContract<InvoiceAnalysis>()
    .ValidateWith<InvoiceAmountValidator>()
    .ValidateWith<InvoiceCurrencyValidator>();
var turnResult = await conversation.SendValidatedAsync("Extract the invoice.", contract, pipeline);
```

`Expect` captures the composed request immediately; finish prompt selection before calling it. Contracts and typed request selections are immutable. Validators are standard scoped DI components implementing `IAiResultValidator<T>`; they receive the candidate, configuration IDs, correlation ID and optional explicit reference strings, not conversation contents. Missing validator registration fails before provider dispatch.

Required checks run sequentially in selection order. Any known violation yields `Invalid`; otherwise insufficient evidence yields `Unverifiable`; all passing checks yield `Valid`. Findings are retained even after a failure. Empty contracts yield `NotValidated`. Passing configured checks neither establishes universal correctness nor authorizes a business operation.

Structural failures (including missing C# `required` properties) produce `HasResult == false` and `StructuralError`, with raw text retained. Domain validators are skipped: domain status is `Unverifiable` when requested, otherwise `NotValidated`. The SDK parser implements the supplied JSON type contract, not every JSON Schema constraint. Provider, DI, unexpected converter/validator and infrastructure exceptions propagate unchanged; cancellation propagates throughout. Missing authoritative evidence is an explicit validator finding, distinct from an unavailable dependency. No automatic retry or repair occurs.

Conversation validation uses the existing operation gate. Valid and explicitly unvalidated typed responses commit detached text and findings; invalid, unverifiable and structurally failed responses leave history unchanged. Returned results retain the attempted request for application-owned auditing. Typed candidates and mutable SDK responses are not stored in history. Legacy turns explicitly report `NotValidated`.

The [invoice sample](samples/AIComFramework.Samples.Console/Validation/InvoiceValidationDemo.cs) executes all four domain outcomes using a fake model, two independent validators and an application-owned ledger without credentials.

## Trust and limitations

Raw prompts and `Untrusted.From` values remain user data in JSON envelopes with provenance. `Trusted.From` is an explicit application-controlled instruction path. JSON escaping and typed wrappers prevent some accidental composition errors; they do not eliminate semantic prompt injection.

This implementation supports complete text requests and text conversation responses. Streaming, tools, multimodal conversation history, durable conversation storage, conversation restoration, semantic selection and token budgeting are deferred. Full history is unbounded in memory; applications should bound session lifetimes. Metadata IDs should not contain secrets. Definition snapshots and their JSON contain prompt text and data; treat them as sensitive content.

No measured token, cost or quality improvement is claimed.

The solution separates provider-independent definition/snapshot contracts in `AIComFramework.Abstractions` from registration, composition and conversation behavior in `AIComFramework.Core`. Test projects exercise isolated invariants and cross-component SDK/DI behavior. The executable sample demonstrates the public API with two domains. All projects enable nullable analysis, XML documentation and warnings as errors.

Reusable test fixtures live in `AIComFramework.Tests.Shared`, a separate non-packable helper project referenced by both test projects through ordinary `ProjectReference` entries. Fixtures are compiled once, with no linked folders or duplicate source files. Production projects do not reference test infrastructure.

After restoring packages, run `./tools/Verify-Solution.ps1` to audit solution membership, physical files versus evaluated MSBuild items (Debug, Release and design-time), source ownership, links, duplicate source bodies and reference cycles. Test-runner binary assets imported by NuGet are checked separately from project-owned sources.

## Optional definition serialization

Reference `AIComFramework.Serialization.Json` only when you need JSON definitions. Core has no reference to that module, storage providers, or databases. The shared `IAiDefinitionSerializer` and immutable snapshot records live in `AIComFramework.Abstractions.Serialization`.

For persisted domains, override `AiContext.DomainId` with an application-owned constant such as `"support"`. The legacy default remains the assembly simple name and full context type name for compatibility. Definition IDs are scoped by domain and kind: a rule and rule set may share an ID. Keep these identities stable when editing content or renaming CLR classes; no assembly-qualified type names are persisted or activated.

```csharp
using AIComFramework.Abstractions.Serialization;
using AIComFramework.Serialization.Json.DependencyInjection;

services.AddAIComFramework().AddAiContext<SupportAiContext>();
services.AddAiComJsonSerialization();

// Inject this into your own file, SQL, Redis, or configuration repository.
IAiDefinitionSerializer serializer = provider.GetRequiredService<IAiDefinitionSerializer>();
string storedContent = serializer.Serialize(support.ExportDefinitions());

// Application retrieves content and establishes that its source is authorized.
var loaded = serializer.Deserialize(storedContent);
support.ApplyDefinitions(loaded);

var next = support.Compose(client)
    .WithRole<SupportAgentRole>()
    .WithRules<ReturnsRuleSet>()
    .Prompt("What is the return policy?");

// Between turns, explicitly adopt the newest definitions without losing history or selections.
conversation.Configure(config => config.RefreshDefinitions(support));
```

The executable sample's `InMemoryDefinitionRepository` demonstrates saving, editing, loading and applying content through an injected serializer. Replace that consumer-owned repository with your storage implementation. Updating natural-language text requires neither recompilation, redeployment, a revision field, nor a schema-version change. Loading and refresh are application-triggered; there is no polling or storage monitoring.

Snapshots include all roles, rules, flattened ordered rule-set references, reusable context data and provenance. Rule-set nesting is already flattened by the existing domain model; the snapshot preserves its effective composition and shared rule identities. Schema 1 supports plain text only. Full snapshots are required: missing or unknown IDs, duplicate IDs within a kind, unknown JSON properties, duplicate JSON properties, null required values, unsupported schemas/formats and invalid rule references fail validation. Applying content also requires the same registered exclusive groups and ordered set membership. Structural changes require updating the application's contracts; text, data and provenance can change independently. JSON failures raise `JsonException` (empty arguments raise `ArgumentException`); contract resolution failures raise actionable `ArgumentException` messages before any active state changes.

Deserialization returns data and does not authorize it. Calling `ApplyDefinitions` is the application's explicit decision to use an authorized source for registered instruction slots. Roles and rules remain instruction slots; reusable contexts and their provenance remain untrusted user data. External content cannot introduce executable behavior, change trust categories, create CLR types, or replace runtime services. This does not provide complete prompt-injection protection.

Each `Compose` call captures a single immutable content snapshot, even if selections or `Build` happen after an update. Successful updates publish atomically per context instance; concurrent updates use last-publication-wins semantics. Existing composers, built requests and in-flight sends retain their original content. Registered definition objects remain immutable contracts, so their instruction properties show compiled defaults; use `ExportDefinitions` to inspect active content. Typed generic selection requires exactly one registered definition assignable to that type; existing property-based selections remain supported.

Conversations retain their current snapshot until `RefreshDefinitions` is called with their original domain context instance. Refresh preserves selected roles, rules, context data and history, and follows the existing rule that configuration cannot overlap a send. `UseContext` also captures current definitions but clears selections, as before. Failed updates and refreshes leave active state unchanged. Conversation history serialization remains separate from definition serialization.
