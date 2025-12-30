namespace DotNetAgents.BenchmarkLlm.Metrics;

/// <summary>
/// Aggregated metrics across multiple IChatClient calls.
/// </summary>
public sealed class AggregatedMetrics
{
    public int TotalCalls { get; }
    public long TotalLatencyMs { get; }
    public double AverageLatencyMs { get; }
    public int TotalInputTokens { get; }
    public int TotalOutputTokens { get; }
    public int TotalTokens { get; }
    public int TotalResponseLength { get; }
    public IReadOnlyList<CallMetrics> AllCalls { get; }

    public AggregatedMetrics(IReadOnlyList<CallMetrics> calls)
    {
        AllCalls = calls;
        TotalCalls = calls.Count;
        TotalLatencyMs = calls.Sum(c => c.LatencyMs);
        AverageLatencyMs = TotalCalls > 0 ? (double)TotalLatencyMs / TotalCalls : 0;
        TotalInputTokens = calls.Sum(c => c.InputTokens);
        TotalOutputTokens = calls.Sum(c => c.OutputTokens);
        TotalTokens = TotalInputTokens + TotalOutputTokens;
        TotalResponseLength = calls.Sum(c => c.ResponseLength);
    }

    public static AggregatedMetrics Empty => new(Array.Empty<CallMetrics>());
}
