using System.Text.Json;
using System.Text.Json.Serialization;
using DotNetAgents.BenchmarkLlm.Core;

namespace DotNetAgents.BenchmarkLlm.Export;

/// <summary>
/// Exports results to JSON format.
/// </summary>
public sealed class JsonExporter : IResultExporter
{
    public string Name => "json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public async Task ExportAsync(
        IReadOnlyList<BenchmarkLlmResult> results,
        BenchmarkLlmConfig config,
        string outputPath
    )
    {
        var prompt = results.FirstOrDefault()?.Prompt ?? "N/A";
        var report = new
        {
            RunId = config.GetEffectiveRunId(prompt),
            Prompt = prompt,
            Timestamp = config.Timestamp,
            EvaluationEnabled = config.Evaluate,
            Results = results
                .Select(r => new
                {
                    r.Category,
                    r.BenchmarkName,
                    r.FullName,
                    r.Success,
                    r.Error,
                    r.ErrorDetails,
                    r.IsBaseline,
                    DurationSeconds = r.Duration.TotalSeconds,
                    Metrics = new
                    {
                        r.Metrics.TotalCalls,
                        r.Metrics.TotalInputTokens,
                        r.Metrics.TotalOutputTokens,
                        r.Metrics.TotalTokens,
                        r.Metrics.TotalLatencyMs,
                        r.Metrics.AverageLatencyMs,
                    },
                    Quality = r.QualityScore != null
                        ? new
                        {
                            r.QualityScore.Completeness,
                            r.QualityScore.Structure,
                            r.QualityScore.Accuracy,
                            r.QualityScore.Engagement,
                            r.QualityScore.EvidenceQuality,
                            r.QualityScore.Balance,
                            r.QualityScore.Actionability,
                            r.QualityScore.Depth,
                            r.QualityScore.Average,
                            r.QualityScore.Reasoning,
                        }
                        : null,
                    AgentModels = r.AgentModels,
                })
                .ToList(),
        };

        var filePath = Path.Combine(outputPath, "results.json");
        var json = JsonSerializer.Serialize(report, JsonOptions);
        await File.WriteAllTextAsync(filePath, json);
        Console.WriteLine($"JSON report saved to: {filePath}");
    }
}
