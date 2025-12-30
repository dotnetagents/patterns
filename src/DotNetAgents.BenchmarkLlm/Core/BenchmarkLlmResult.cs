using DotNetAgents.BenchmarkLlm.Evaluation;
using DotNetAgents.BenchmarkLlm.Metrics;

namespace DotNetAgents.BenchmarkLlm.Core;

/// <summary>
/// Result of a single benchmark execution.
/// </summary>
public sealed record BenchmarkLlmResult
{
    public required string Category { get; init; }
    public required string BenchmarkName { get; init; }
    public required string Prompt { get; init; }
    public required bool Success { get; init; }
    public string? Error { get; init; }
    public string? ErrorDetails { get; init; }
    public string? Content { get; init; }
    public required TimeSpan Duration { get; init; }
    public required AggregatedMetrics Metrics { get; init; }
    public QualityScore? QualityScore { get; init; }
    public bool IsBaseline { get; init; }

    /// <summary>
    /// Maps agent names to their configured models.
    /// Key: Agent name (e.g., "Researcher"), Value: Model name (e.g., "gpt-4o-mini").
    /// </summary>
    public IReadOnlyDictionary<string, string>? AgentModels { get; init; }

    public string FullName => $"{Category}.{BenchmarkName}";
}
