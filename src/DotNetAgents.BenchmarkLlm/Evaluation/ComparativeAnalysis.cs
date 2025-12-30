namespace DotNetAgents.BenchmarkLlm.Evaluation;

/// <summary>
/// Result of comparing multiple benchmark outputs side-by-side.
/// </summary>
public sealed record ComparativeAnalysis
{
    /// <summary>
    /// The prompt that was used for all benchmarks.
    /// </summary>
    public required string Prompt { get; init; }

    /// <summary>
    /// Timestamp when the comparison was performed.
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// Individual benchmark comparisons with metrics.
    /// </summary>
    public required IReadOnlyList<BenchmarkComparison> Benchmarks { get; init; }

    /// <summary>
    /// LLM-generated comparative analysis text.
    /// </summary>
    public required string Analysis { get; init; }

    /// <summary>
    /// Summary verdict comparing all approaches.
    /// </summary>
    public required string Verdict { get; init; }
}

/// <summary>
/// Comparison data for a single benchmark.
/// </summary>
public sealed record BenchmarkComparison
{
    /// <summary>
    /// Full name of the benchmark (Category/Name).
    /// </summary>
    public required string FullName { get; init; }

    /// <summary>
    /// Whether this is the baseline benchmark.
    /// </summary>
    public required bool IsBaseline { get; init; }

    /// <summary>
    /// Word count of the output content.
    /// </summary>
    public required int WordCount { get; init; }

    /// <summary>
    /// Total tokens used (input + output).
    /// </summary>
    public required int TotalTokens { get; init; }

    /// <summary>
    /// Total API calls made.
    /// </summary>
    public required int TotalCalls { get; init; }

    /// <summary>
    /// Total latency in milliseconds.
    /// </summary>
    public required long TotalLatencyMs { get; init; }

    /// <summary>
    /// Quality score if evaluation was performed.
    /// </summary>
    public QualityScore? QualityScore { get; init; }

    /// <summary>
    /// Key strengths identified in the content.
    /// </summary>
    public IReadOnlyList<string>? Strengths { get; init; }

    /// <summary>
    /// Key weaknesses identified in the content.
    /// </summary>
    public IReadOnlyList<string>? Weaknesses { get; init; }
}
