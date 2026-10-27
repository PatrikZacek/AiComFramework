using System.IO.Compression;
using System.Reflection.Metadata;
using System.Text.Json;
using AIComFramework.Abstractions.Roles;
using AIComFramework.Abstractions.Serialization;
using AIComFramework.Core.Contexts;
using AIComFramework.Serialization.Json;
using Microsoft.Extensions.AI;

var context = new SmokeContext();
IAiDefinitionSerializer serializer = new JsonAiDefinitionSerializer();
var snapshot = serializer.Deserialize(serializer.Serialize(context.ExportDefinitions()));
if (snapshot.DomainId != context.DomainId || snapshot.Roles.Single().Instructions != "Answer briefly")
    throw new InvalidOperationException("Definition JSON round-trip failed.");
context.ApplyDefinitions(snapshot);
using var client = new SmokeChatClient();
var response = await context.Compose(client).WithRole(context.Agent).Prompt("Smoke test").SendAsync();
if (response.Text != "consumer-ok") throw new InvalidOperationException("Packaged Core request failed.");

// Inspect actual portable PDBs from the symbol packages, without another tool dependency.
var sourceLinkId = new Guid("CC110556-A091-4D38-9FEC-25AB9A351A6A");
foreach (var symbols in Directory.GetFiles(args[0], "*.snupkg"))
{
    using var archive = ZipFile.OpenRead(symbols);
    var entry = archive.Entries.Single(e => e.FullName.EndsWith(".pdb", StringComparison.Ordinal));
    using var memory = new MemoryStream();
    using (var input = entry.Open()) input.CopyTo(memory);
    memory.Position = 0;
    using var provider = MetadataReaderProvider.FromPortablePdbStream(memory);
    var reader = provider.GetMetadataReader();
    var link = reader.CustomDebugInformation.Select(reader.GetCustomDebugInformation)
        .Single(info => reader.GetGuid(info.Kind) == sourceLinkId);
    using var json = JsonDocument.Parse(reader.GetBlobBytes(link.Value));
    var mappings = json.RootElement.GetProperty("documents").EnumerateObject().ToArray();
    if (!mappings.Any(p => p.Value.GetString()!.StartsWith(
            $"https://raw.githubusercontent.com/PatrikZacek/AiComFramework/{args[1]}/", StringComparison.Ordinal)))
        throw new InvalidOperationException($"Missing commit-specific Source Link in {symbols}.");
    foreach (var handle in reader.Documents)
    {
        var name = reader.GetString(reader.GetDocument(handle).Name);
        if (!name.StartsWith("/_/", StringComparison.Ordinal))
            throw new InvalidOperationException($"Non-deterministic source path: {name}");
    }
}
Console.WriteLine("Package consumer, JSON round-trip, Core request and Source Link checks passed.");

sealed class SmokeContext : AiContext
{
    public override string DomainId => "package.smoke";
    public AiRole Agent { get; } = new("agent", "Answer briefly");
    protected override void OnConfiguring(AiContextBuilder builder) => builder.AddRole(Agent);
}

sealed class SmokeChatClient : IChatClient
{
    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages,
        ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!messages.Any()) throw new InvalidOperationException("No composed messages.");
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "consumer-ok")));
    }
    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages,
        ChatOptions? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;
    public void Dispose() { }
}
