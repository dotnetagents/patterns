namespace DotNetAgents.BenchmarkLlm.Core;

/// <summary>
/// Output from a benchmark method, containing the generated content and optional metadata.
/// Benchmark methods can return either a string or a BenchmarkOutput.
/// </summary>
public sealed record BenchmarkOutput
{
    /// <summary>
    /// The generated content from the benchmark.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Maps agent names to their configured models.
    /// Key: Agent name (e.g., "Researcher"), Value: Model name (e.g., "gpt-4o-mini").
    /// </summary>
    public IReadOnlyDictionary<string, string>? AgentModels { get; init; }

    /// <summary>
    /// Creates a BenchmarkOutput from content string (implicit conversion).
    /// </summary>
    public static implicit operator BenchmarkOutput(string content) => new() { Content = content };

    /// <summary>
    /// Creates a BenchmarkOutput with content and agent model configuration.
    /// </summary>
    public static BenchmarkOutput WithModels(
        string content,
        IReadOnlyDictionary<string, string> agentModels
    ) => new() { Content = content, AgentModels = agentModels };
}
