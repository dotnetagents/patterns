using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace DotNetAgents.BenchmarkLlm.Metrics;

/// <summary>
/// Decorator that wraps an IChatClient to collect metrics on each call.
/// Uses DelegatingChatClient pattern from Microsoft.Extensions.AI.
/// </summary>
public sealed class MetricsCollectingChatClient : DelegatingChatClient
{
    private readonly List<CallMetrics> _callMetrics = new();
    private readonly object _lock = new();
    private readonly string _clientName;

    public MetricsCollectingChatClient(IChatClient innerClient, string clientName = "default")
        : base(innerClient)
    {
        _clientName = clientName;
    }

    public IReadOnlyList<CallMetrics> CallMetrics
    {
        get
        {
            lock (_lock)
                return _callMetrics.ToList();
        }
    }

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        var stopwatch = Stopwatch.StartNew();
        var messageList = messages.ToList();

        var response = await base.GetResponseAsync(messageList, options, cancellationToken);

        stopwatch.Stop();

        var metrics = new CallMetrics
        {
            ClientName = _clientName,
            Timestamp = DateTime.UtcNow,
            LatencyMs = stopwatch.ElapsedMilliseconds,
            InputTokens = (int)(response.Usage?.InputTokenCount ?? 0),
            OutputTokens = (int)(response.Usage?.OutputTokenCount ?? 0),
            ResponseLength = response.Text?.Length ?? 0,
            InputMessageCount = messageList.Count,
            WasStreaming = false,
        };

        lock (_lock)
        {
            _callMetrics.Add(metrics);
        }

        return response;
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        var stopwatch = Stopwatch.StartNew();
        var messageList = messages.ToList();
        var responseLength = 0;
        int? inputTokens = null;
        int? outputTokens = null;

        await foreach (
            var update in base.GetStreamingResponseAsync(messageList, options, cancellationToken)
        )
        {
            responseLength += update.Text?.Length ?? 0;

            if (update.Contents != null)
            {
                foreach (var content in update.Contents)
                {
                    if (content is UsageContent usageContent)
                    {
                        inputTokens = (int?)usageContent.Details.InputTokenCount;
                        outputTokens = (int?)usageContent.Details.OutputTokenCount;
                    }
                }
            }

            yield return update;
        }

        stopwatch.Stop();

        var metrics = new CallMetrics
        {
            ClientName = _clientName,
            Timestamp = DateTime.UtcNow,
            LatencyMs = stopwatch.ElapsedMilliseconds,
            InputTokens = inputTokens ?? 0,
            OutputTokens = outputTokens ?? 0,
            ResponseLength = responseLength,
            InputMessageCount = messageList.Count,
            WasStreaming = true,
        };

        lock (_lock)
        {
            _callMetrics.Add(metrics);
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            _callMetrics.Clear();
        }
    }

    public AggregatedMetrics GetAggregatedMetrics()
    {
        lock (_lock)
        {
            return new AggregatedMetrics(_callMetrics.ToList());
        }
    }
}
