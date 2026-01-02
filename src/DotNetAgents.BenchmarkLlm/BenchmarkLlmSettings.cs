namespace DotNetAgents.BenchmarkLlm;

/// <summary>
/// Settings for BenchmarkLlm runs, typically bound from appsettings.json.
/// </summary>
public class BenchmarkLlmSettings
{
    /// <summary>
    /// The provider to use for LLM-as-Judge evaluation. Required when Evaluate is true.
    /// Valid providers: "ollama", "openai", "azure", "openrouter", "github"
    /// </summary>
    public string? EvaluationProvider { get; set; }

    /// <summary>
    /// The model to use for LLM-as-Judge evaluation. Required when Evaluate is true.
    /// Examples: "gpt-4o", "gpt-4o-mini", "llama3.2"
    /// </summary>
    public string? EvaluationModel { get; set; }

    /// <summary>
    /// Glob pattern to filter which benchmarks to run. Default: "*" (all).
    /// </summary>
    public string Filter { get; set; } = "*";

    /// <summary>
    /// Output directory for benchmark results. Default: "./runs".
    /// </summary>
    public string ArtifactsPath { get; set; } = "./runs";

    /// <summary>
    /// Optional custom run identifier. If not set, auto-generated from timestamp + prompt.
    /// </summary>
    public string? RunId { get; set; }

    /// <summary>
    /// Enable LLM-as-Judge quality evaluation. Default: false.
    /// </summary>
    public bool Evaluate { get; set; }

    /// <summary>
    /// Type of evaluator to use: "content" for articles/text, "task" for agent tool-use.
    /// Default: "content".
    /// </summary>
    public string EvaluatorType { get; set; } = "content";

    /// <summary>
    /// List of exporters to use: "console", "markdown", "json". Default: ["console"].
    /// </summary>
    public List<string> Exporters { get; set; } = ["console"];
}
