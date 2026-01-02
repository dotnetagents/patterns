using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace DotNetAgents.BenchmarkLlm.Metrics;

/// <summary>
/// Collects telemetry from Microsoft.Extensions.AI's built-in OpenTelemetry instrumentation.
/// Uses in-memory exporter to capture spans and extract metrics.
/// </summary>
public sealed class BenchmarkTelemetryCollector : IDisposable
{
    private readonly List<Activity> _activities = [];
    private readonly TracerProvider _tracerProvider;
    private readonly object _lock = new();

    /// <summary>
    /// Source name for DotNetAgents telemetry.
    /// </summary>
    public const string SourceName = "DotNetAgents.Benchmark";

    public BenchmarkTelemetryCollector()
    {
        _tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddSource(SourceName)
            .AddSource("Microsoft.Extensions.AI")
            .AddSource("Experimental.Microsoft.Extensions.AI")
            .AddInMemoryExporter(_activities)
            .Build();
    }

    /// <summary>
    /// Gets metrics extracted from collected telemetry activities.
    /// </summary>
    public AggregatedMetrics GetMetrics()
    {
        List<Activity> snapshot;
        lock (_lock)
        {
            snapshot = _activities.ToList();
        }

        // Extract metrics from gen_ai chat completion spans
        var calls = snapshot
            .Where(a => a.OperationName.Contains("chat", StringComparison.OrdinalIgnoreCase))
            .Select(ExtractCallMetrics)
            .ToList();

        return new AggregatedMetrics(calls);
    }

    private static CallMetrics ExtractCallMetrics(Activity activity)
    {
        return new CallMetrics
        {
            ClientName = GetTagString(activity, "gen_ai.system")
                ?? GetTagString(activity, "gen_ai.request.model")
                ?? activity.DisplayName
                ?? "unknown",
            Timestamp = activity.StartTimeUtc,
            LatencyMs = (long)activity.Duration.TotalMilliseconds,
            InputTokens = GetTagInt(activity, "gen_ai.usage.input_tokens")
                ?? GetTagInt(activity, "gen_ai.request.input_tokens")
                ?? 0,
            OutputTokens = GetTagInt(activity, "gen_ai.usage.output_tokens")
                ?? GetTagInt(activity, "gen_ai.response.output_tokens")
                ?? 0,
            ResponseLength = GetTagInt(activity, "gen_ai.response.length") ?? 0,
            InputMessageCount = GetTagInt(activity, "gen_ai.request.message_count") ?? 1,
            WasStreaming = GetTagString(activity, "gen_ai.request.streaming") == "true",
        };
    }

    private static string? GetTagString(Activity activity, string key)
    {
        return activity.GetTagItem(key)?.ToString();
    }

    private static int? GetTagInt(Activity activity, string key)
    {
        var value = activity.GetTagItem(key);
        return value switch
        {
            int i => i,
            long l => (int)l,
            string s when int.TryParse(s, out var i) => i,
            _ => null
        };
    }

    public void Dispose()
    {
        _tracerProvider.Dispose();
    }
}
