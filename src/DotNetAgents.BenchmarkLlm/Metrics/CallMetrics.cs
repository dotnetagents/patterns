namespace DotNetAgents.BenchmarkLlm.Metrics;

/// <summary>
/// Captures metrics for a single IChatClient call.
/// </summary>
public sealed record CallMetrics
{
    public required string ClientName { get; init; }
    public required DateTime Timestamp { get; init; }
    public required long LatencyMs { get; init; }
    public required int InputTokens { get; init; }
    public required int OutputTokens { get; init; }
    public int TotalTokens => InputTokens + OutputTokens;
    public required int ResponseLength { get; init; }
    public required int InputMessageCount { get; init; }
    public bool WasStreaming { get; init; }
}
