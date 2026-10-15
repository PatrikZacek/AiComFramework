using System.Text.Json;
using AIComFramework.Abstractions.Conversations;
using AIComFramework.Abstractions.Trust;
using AIComFramework.Core.Composition;
using Microsoft.Extensions.AI;

namespace AIComFramework.Core.Conversations;

/// <summary>An isolated text conversation. Overlapping sends, previews or configuration operations are rejected.</summary>
public sealed class AiConversation
{
    private readonly object _stateLock = new();
    private readonly IChatClient _client;
    private readonly IUntrustedRenderer _renderer;
    private readonly RequestExecutor _executor;
    private DefinitionSelection _selection;
    private ImmutableArray<ConversationTurn> _turns = [];
    private int _operationActive;

    internal AiConversation(DefinitionSelection selection, IChatClient client, IUntrustedRenderer renderer)
    {
        _selection = selection;
        _client = client;
        _renderer = renderer;
        _executor = new(client);
    }

    /// <summary>Gets this conversation's independent identity.</summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>Returns immutable committed state, including sensitive full history.</summary>
    public ConversationState GetState()
    {
        lock (_stateLock) return new(Id, _selection.Snapshot, _turns);
    }

    /// <summary>Inspects a possible next request without execution or state changes. A later send captures state again.</summary>
    public RequestSnapshot Preview(string prompt, HistoryOptions? history = null, Action<AiRequestConfiguration>? configureRequest = null)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        EnterOperation();
        try { return CreateRequest(prompt, history, configureRequest); }
        finally { ExitOperation(); }
    }

    /// <summary>Atomically replaces active configuration after the callback succeeds; history is retained.</summary>
    /// <exception cref="InvalidOperationException">Another operation is active.</exception>
    public AiConversation Configure(Action<ConversationConfigurationBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        EnterOperation();
        try
        {
            var builder = new ConversationConfigurationBuilder(_selection);
            configure(builder);
            lock (_stateLock) _selection = builder.Selection;
            return this;
        }
        finally { ExitOperation(); }
    }

    /// <summary>Sends a turn and commits only on success. Request overrides do not change active configuration.</summary>
    /// <param name="prompt">Untrusted user input.</param>
    /// <param name="cancellationToken">Cancellation propagated to the client.</param>
    /// <param name="history">Eligibility grants and selection strategy for this request.</param>
    /// <param name="configureRequest">Optional local role, rule and contextual data overrides.</param>
    /// <exception cref="InvalidOperationException">Another operation is active or a history selector returns invalid IDs.</exception>
    /// <exception cref="NotSupportedException">The response contains non-text content or non-assistant roles.</exception>
    public Task<ChatResponse> SendAsync(string prompt, CancellationToken cancellationToken = default,
        HistoryOptions? history = null, Action<AiRequestConfiguration>? configureRequest = null) =>
        SendCoreAsync(prompt, history, configureRequest, request => _executor.SendAsync(request, cancellationToken), cancellationToken);

    /// <summary>Sends a typed SDK request and commits its text exchange on success.</summary>
    /// <param name="prompt">Untrusted user input.</param>
    /// <param name="cancellationToken">Cancellation propagated to the provider.</param>
    /// <param name="history">Per-request history eligibility and selection.</param>
    /// <param name="configureRequest">Local overrides; the prompt argument remains the final user message.</param>
    /// <param name="serializerOptions">Response serializer options; use source-generated metadata for trimmed applications.</param>
    /// <param name="useJsonSchema">Whether the provider should receive a response schema; false uses the SDK JSON fallback.</param>
    public Task<ChatResponse<T>> SendAsync<T>(string prompt, CancellationToken cancellationToken = default,
        HistoryOptions? history = null, Action<AiRequestConfiguration>? configureRequest = null,
        JsonSerializerOptions? serializerOptions = null, bool useJsonSchema = true) =>
        SendCoreAsync(prompt, history, configureRequest,
            request => _executor.SendAsync<T>(request, serializerOptions ?? JsonSerializerOptions.Default, useJsonSchema, cancellationToken),
            cancellationToken);

    private async Task<TResponse> SendCoreAsync<TResponse>(string prompt, HistoryOptions? history,
        Action<AiRequestConfiguration>? configureRequest, Func<RequestSnapshot, Task<TResponse>> execute,
        CancellationToken cancellationToken) where TResponse : ChatResponse
    {
        ArgumentNullException.ThrowIfNull(prompt);
        EnterOperation();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var request = CreateRequest(prompt, history, configureRequest);
            var response = await execute(request).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            var exchange = ConversationExchange.Capture(request, response);
            lock (_stateLock) _turns = _turns.Add(new(_turns.Length + 1L, request, exchange));
            return response;
        }
        finally { ExitOperation(); }
    }

    private RequestSnapshot CreateRequest(string prompt, HistoryOptions? history, Action<AiRequestConfiguration>? configureRequest)
    {
        var selectedHistory = HistorySelection.Create(_turns, _selection.Catalog.DomainId, history ?? new());
        var composer = new AiPromptComposer(_selection.Catalog, _client, _renderer);
        composer.SetSelection(_selection);
        composer.SetHistory(selectedHistory.Messages, selectedHistory.Eligible, selectedHistory.Transmitted);
        configureRequest?.Invoke(new AiRequestConfiguration(composer));
        return composer.Prompt(prompt).Build();
    }

    private void EnterOperation()
    {
        if (Interlocked.CompareExchange(ref _operationActive, 1, 0) != 0)
            throw new InvalidOperationException("A conversation operation is already active.");
    }
    private void ExitOperation() => Volatile.Write(ref _operationActive, 0);
}
