namespace DotNetAgents.BenchmarkLlm;

/// <summary>
/// Settings for BenchmarkLlm runs, typically bound from appsettings.json.
/// </summary>
public class BenchmarkLlmSettings
{
    /// <summary>
    /// The model to use for running benchmarks. Required.
    /// Examples: "gpt-4o", "gpt-4o-mini", "llama3.2"
    /// </summary>
    public string Model { get; set; } = null!;

    /// <summary>
    /// The model to use for LLM-as-Judge evaluation. Optional.
    /// If not set, uses the same model as benchmark runs.
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
    /// List of exporters to use: "console", "markdown", "json". Default: ["console"].
    /// </summary>
    public List<string> Exporters { get; set; } = ["console"];
}
