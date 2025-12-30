using System.Text;
using DotNetAgents.BenchmarkLlm.Core;

namespace DotNetAgents.BenchmarkLlm.Export;

/// <summary>
/// Exports results to Markdown format.
/// </summary>
public sealed class MarkdownExporter : IResultExporter
{
    public string Name => "markdown";

    public async Task ExportAsync(
        IReadOnlyList<BenchmarkLlmResult> results,
        BenchmarkLlmConfig config,
        string outputPath
    )
    {
        var sb = new StringBuilder();

        var prompt = results.FirstOrDefault()?.Prompt ?? "N/A";
        sb.AppendLine($"# Benchmark Results: {prompt}");
        sb.AppendLine();
        sb.AppendLine($"**Run ID:** {config.GetEffectiveRunId(prompt)}");
        sb.AppendLine($"**Timestamp:** {config.Timestamp:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine();

        // Results table
        sb.AppendLine("## Results");
        sb.AppendLine();
        sb.AppendLine("| Benchmark | Status | API Calls | Tokens | Latency | Quality |");
        sb.AppendLine("|-----------|--------|-----------|--------|---------|---------|");

        foreach (var result in results)
        {
            var status = result.Success ? "OK" : "FAIL";
            var quality = result.QualityScore != null ? $"{result.QualityScore.Average:F1}/5" : "-";
            var baseline = result.IsBaseline ? " (baseline)" : "";

            sb.AppendLine(
                $"| {result.FullName}{baseline} | {status} | {result.Metrics.TotalCalls} | {result.Metrics.TotalTokens} | {result.Duration.TotalSeconds:F1}s | {quality} |"
            );
        }

        sb.AppendLine();

        // Errors section
        var failedResults = results.Where(r => !r.Success).ToList();
        if (failedResults.Count > 0)
        {
            sb.AppendLine("## Errors");
            sb.AppendLine();

            foreach (var result in failedResults)
            {
                sb.AppendLine($"### {result.FullName}");
                sb.AppendLine();
                sb.AppendLine($"**Error:** {result.Error}");
                sb.AppendLine();

                if (!string.IsNullOrEmpty(result.ErrorDetails))
                {
                    sb.AppendLine("**Stack Trace:**");
                    sb.AppendLine("```");
                    sb.AppendLine(result.ErrorDetails);
                    sb.AppendLine("```");
                    sb.AppendLine();
                }
            }
        }

        // Comparison
        var baselineResult = results.FirstOrDefault(r => r.IsBaseline);
        if (baselineResult != null && results.Count > 1)
        {
            sb.AppendLine("## Comparison");
            sb.AppendLine();
            sb.AppendLine($"Baseline: **{baselineResult.FullName}**");
            sb.AppendLine();

            foreach (var result in results.Where(r => !r.IsBaseline))
            {
                var tokenDiff = result.Metrics.TotalTokens - baselineResult.Metrics.TotalTokens;
                var tokenPct =
                    baselineResult.Metrics.TotalTokens > 0
                        ? (double)tokenDiff / baselineResult.Metrics.TotalTokens * 100
                        : 0;

                var latencyDiff =
                    result.Duration.TotalSeconds - baselineResult.Duration.TotalSeconds;

                sb.AppendLine($"### {result.FullName}");
                sb.AppendLine();
                sb.AppendLine($"- **Tokens:** {tokenDiff:+#;-#;0} ({tokenPct:+0.0;-0.0;0}%)");
                sb.AppendLine($"- **Latency:** {latencyDiff:+0.0;-0.0;0}s");
                sb.AppendLine(
                    $"- **API Calls:** {result.Metrics.TotalCalls - baselineResult.Metrics.TotalCalls:+#;-#;0}"
                );

                if (result.QualityScore != null && baselineResult.QualityScore != null)
                {
                    var qualityDiff =
                        result.QualityScore.Average - baselineResult.QualityScore.Average;
                    sb.AppendLine($"- **Quality:** {qualityDiff:+0.0;-0.0;0}");
                }

                sb.AppendLine();
            }
        }

        // Agent models
        var resultsWithModels = results
            .Where(r => r.AgentModels != null && r.AgentModels.Count > 0)
            .ToList();
        if (resultsWithModels.Count > 0)
        {
            sb.AppendLine("## Agent Models");
            sb.AppendLine();

            foreach (var result in resultsWithModels)
            {
                sb.AppendLine($"### {result.FullName}");
                sb.AppendLine();
                sb.AppendLine("| Agent | Model |");
                sb.AppendLine("|-------|-------|");

                foreach (var (agent, model) in result.AgentModels!)
                {
                    sb.AppendLine($"| {agent} | `{model}` |");
                }

                sb.AppendLine();
            }
        }

        // Quality details
        if (results.Any(r => r.QualityScore != null))
        {
            sb.AppendLine("## Quality Breakdown");
            sb.AppendLine();
            sb.AppendLine(
                "| Benchmark | Compl | Struct | Accur | Engage | Evid | Bal | Action | Depth | **Avg** |"
            );
            sb.AppendLine(
                "|-----------|:-----:|:------:|:-----:|:------:|:----:|:---:|:------:|:-----:|:-------:|"
            );

            foreach (var result in results.Where(r => r.QualityScore != null))
            {
                var q = result.QualityScore!;
                sb.AppendLine(
                    $"| {result.FullName} | {q.Completeness} | {q.Structure} | {q.Accuracy} | {q.Engagement} | {q.EvidenceQuality} | {q.Balance} | {q.Actionability} | {q.Depth} | **{q.Average:F1}** |"
                );
            }

            sb.AppendLine();
            sb.AppendLine(
                "*Compl=Completeness, Struct=Structure, Accur=Accuracy, Engage=Engagement, Evid=Evidence, Bal=Balance, Action=Actionability*"
            );
            sb.AppendLine();
        }

        var filePath = Path.Combine(outputPath, "comparison.md");
        await File.WriteAllTextAsync(filePath, sb.ToString());
        Console.WriteLine($"Markdown report saved to: {filePath}");
    }
}
