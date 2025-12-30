using System.Text;
using DotNetAgents.BenchmarkLlm.Evaluation;

namespace DotNetAgents.BenchmarkLlm.Export;

/// <summary>
/// Exports comparative analysis results to analysis.md format.
/// </summary>
public sealed class AnalysisExporter
{
    public async Task ExportAsync(ComparativeAnalysis analysis, string outputPath)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine($"# Benchmark Analysis: {analysis.Prompt}");
        sb.AppendLine();
        sb.AppendLine($"**Generated:** {analysis.Timestamp:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine();

        // Metrics comparison table
        sb.AppendLine("## Metrics Comparison");
        sb.AppendLine();
        sb.AppendLine("| Benchmark | Words | Tokens | API Calls | Latency | Avg Quality |");
        sb.AppendLine("|-----------|------:|-------:|----------:|--------:|------------:|");

        var baseline = analysis.Benchmarks.FirstOrDefault(b => b.IsBaseline);

        foreach (var b in analysis.Benchmarks)
        {
            var baselineMarker = b.IsBaseline ? " *" : "";
            var quality = b.QualityScore != null ? $"{b.QualityScore.Average:F1}/5" : "-";
            sb.AppendLine(
                $"| {b.FullName}{baselineMarker} | {b.WordCount:N0} | {b.TotalTokens:N0} | {b.TotalCalls} | {b.TotalLatencyMs:N0}ms | {quality} |"
            );
        }

        sb.AppendLine();
        if (baseline != null)
        {
            sb.AppendLine("\\* baseline");
            sb.AppendLine();
        }

        // Delta comparison if we have a baseline
        if (baseline != null && analysis.Benchmarks.Count > 1)
        {
            sb.AppendLine("### Comparison vs Baseline");
            sb.AppendLine();

            foreach (var b in analysis.Benchmarks.Where(x => !x.IsBaseline))
            {
                var wordDiff = b.WordCount - baseline.WordCount;
                var wordPct =
                    baseline.WordCount > 0 ? (double)wordDiff / baseline.WordCount * 100 : 0;

                var tokenDiff = b.TotalTokens - baseline.TotalTokens;
                var tokenPct =
                    baseline.TotalTokens > 0 ? (double)tokenDiff / baseline.TotalTokens * 100 : 0;

                var latencyDiff = b.TotalLatencyMs - baseline.TotalLatencyMs;
                var latencyPct =
                    baseline.TotalLatencyMs > 0
                        ? (double)latencyDiff / baseline.TotalLatencyMs * 100
                        : 0;

                sb.AppendLine($"**{b.FullName}** vs **{baseline.FullName}**:");
                sb.AppendLine();
                sb.AppendLine($"- Words: {wordDiff:+#;-#;0} ({wordPct:+0.0;-0.0;0}%)");
                sb.AppendLine($"- Tokens: {tokenDiff:+#;-#;0} ({tokenPct:+0.0;-0.0;0}%)");
                sb.AppendLine($"- Latency: {latencyDiff:+#;-#;0}ms ({latencyPct:+0.0;-0.0;0}%)");
                sb.AppendLine($"- API Calls: {b.TotalCalls - baseline.TotalCalls:+#;-#;0}");

                if (b.QualityScore != null && baseline.QualityScore != null)
                {
                    var qualityDiff = b.QualityScore.Average - baseline.QualityScore.Average;
                    sb.AppendLine($"- Quality: {qualityDiff:+0.0;-0.0;0}");
                }

                sb.AppendLine();
            }
        }

        // Quality scores breakdown
        var benchmarksWithScores = analysis.Benchmarks.Where(b => b.QualityScore != null).ToList();
        if (benchmarksWithScores.Count > 0)
        {
            sb.AppendLine("## Quality Scores");
            sb.AppendLine();
            sb.AppendLine(
                "| Benchmark | Compl | Struct | Accur | Engage | Evid | Balance | Action | Depth | **Avg** |"
            );
            sb.AppendLine(
                "|-----------|:-----:|:------:|:-----:|:------:|:----:|:-------:|:------:|:-----:|:-------:|"
            );

            foreach (var b in benchmarksWithScores)
            {
                var q = b.QualityScore!;
                sb.AppendLine(
                    $"| {b.FullName} | {q.Completeness} | {q.Structure} | {q.Accuracy} | {q.Engagement} | {q.EvidenceQuality} | {q.Balance} | {q.Actionability} | {q.Depth} | **{q.Average:F1}** |"
                );
            }

            sb.AppendLine();
            sb.AppendLine("*Scores: 1=Poor, 5=Excellent*");
            sb.AppendLine();
        }

        // Strengths and weaknesses
        var benchmarksWithSW = analysis
            .Benchmarks.Where(b => (b.Strengths?.Count > 0) || (b.Weaknesses?.Count > 0))
            .ToList();

        if (benchmarksWithSW.Count > 0)
        {
            sb.AppendLine("## Strengths & Weaknesses");
            sb.AppendLine();

            foreach (var b in benchmarksWithSW)
            {
                sb.AppendLine($"### {b.FullName}");
                sb.AppendLine();

                if (b.Strengths?.Count > 0)
                {
                    sb.AppendLine("**Strengths:**");
                    foreach (var s in b.Strengths)
                    {
                        sb.AppendLine($"- {s}");
                    }
                    sb.AppendLine();
                }

                if (b.Weaknesses?.Count > 0)
                {
                    sb.AppendLine("**Weaknesses:**");
                    foreach (var w in b.Weaknesses)
                    {
                        sb.AppendLine($"- {w}");
                    }
                    sb.AppendLine();
                }
            }
        }

        // Analysis section
        sb.AppendLine("## Comparative Analysis");
        sb.AppendLine();
        sb.AppendLine(analysis.Analysis);
        sb.AppendLine();

        // Verdict
        sb.AppendLine("## Verdict");
        sb.AppendLine();
        sb.AppendLine(analysis.Verdict);
        sb.AppendLine();

        // Footer
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("*Generated by BenchmarkLlm Comparative Evaluator*");

        var filePath = Path.Combine(outputPath, "analysis.md");
        await File.WriteAllTextAsync(filePath, sb.ToString());
        Console.WriteLine($"Analysis report saved to: {filePath}");
    }
}
