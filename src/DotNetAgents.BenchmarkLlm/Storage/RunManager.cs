using System.Text.Json;
using DotNetAgents.BenchmarkLlm.Core;

namespace DotNetAgents.BenchmarkLlm.Storage;

/// <summary>
/// Manages benchmark run directories and file storage.
/// </summary>
public sealed class RunManager
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// Creates the run directory and returns the path.
    /// </summary>
    public string CreateRunDirectory(BenchmarkLlmConfig config, string prompt)
    {
        var runPath = Path.Combine(config.ArtifactsPath, config.GetEffectiveRunId(prompt));
        Directory.CreateDirectory(runPath);
        return runPath;
    }

    /// <summary>
    /// Saves run configuration.
    /// </summary>
    public async Task SaveConfigAsync(BenchmarkLlmConfig config, string prompt, string runPath)
    {
        var configData = new
        {
            Prompt = prompt,
            config.Filter,
            config.RunId,
            config.Timestamp,
            config.Evaluate,
            config.Exporters,
        };

        var filePath = Path.Combine(runPath, "run-config.json");
        var json = JsonSerializer.Serialize(configData, JsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }

    /// <summary>
    /// Saves environment information.
    /// </summary>
    public async Task SaveEnvironmentAsync(string runPath)
    {
        var envData = new
        {
            Provider = Environment.GetEnvironmentVariable("LLM_PROVIDER") ?? "ollama",
            Model = Environment.GetEnvironmentVariable("MODEL") ?? "default",
            Runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            MachineName = Environment.MachineName,
            Timestamp = DateTime.UtcNow,
        };

        var filePath = Path.Combine(runPath, "environment.json");
        var json = JsonSerializer.Serialize(envData, JsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }

    /// <summary>
    /// Saves benchmark output content.
    /// </summary>
    public async Task SaveOutputAsync(BenchmarkLlmResult result, string runPath)
    {
        var benchmarkDir = Path.Combine(runPath, result.Category, result.BenchmarkName);
        Directory.CreateDirectory(benchmarkDir);

        if (result.Content != null)
        {
            var outputPath = Path.Combine(benchmarkDir, "output.md");
            await File.WriteAllTextAsync(outputPath, result.Content);
        }

        var metricsData = new
        {
            result.Success,
            result.Error,
            DurationSeconds = result.Duration.TotalSeconds,
            result.Metrics.TotalCalls,
            result.Metrics.TotalInputTokens,
            result.Metrics.TotalOutputTokens,
            result.Metrics.TotalTokens,
            result.Metrics.TotalLatencyMs,
            Quality = result.QualityScore != null
                ? new
                {
                    result.QualityScore.Completeness,
                    result.QualityScore.Structure,
                    result.QualityScore.Accuracy,
                    result.QualityScore.Engagement,
                    result.QualityScore.EvidenceQuality,
                    result.QualityScore.Balance,
                    result.QualityScore.Actionability,
                    result.QualityScore.Depth,
                    result.QualityScore.Average,
                }
                : null,
        };

        var metricsPath = Path.Combine(benchmarkDir, "metrics.json");
        var json = JsonSerializer.Serialize(metricsData, JsonOptions);
        await File.WriteAllTextAsync(metricsPath, json);
    }
}
