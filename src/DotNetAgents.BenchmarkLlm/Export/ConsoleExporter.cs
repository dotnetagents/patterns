using DotNetAgents.BenchmarkLlm.Core;

namespace DotNetAgents.BenchmarkLlm.Export;

/// <summary>
/// Exports results to console output.
/// </summary>
public sealed class ConsoleExporter : IResultExporter
{
    public string Name => "console";

    public Task ExportAsync(
        IReadOnlyList<BenchmarkLlmResult> results,
        BenchmarkLlmConfig config,
        string outputPath
    )
    {
        Console.WriteLine();
        Console.WriteLine(
            "╔══════════════════════════════════════════════════════════════════════╗"
        );
        Console.WriteLine(
            "║                    BENCHMARK RESULTS SUMMARY                         ║"
        );
        Console.WriteLine(
            "╚══════════════════════════════════════════════════════════════════════╝"
        );
        Console.WriteLine();

        var prompt = results.FirstOrDefault()?.Prompt ?? "N/A";
        Console.WriteLine($"Prompt: {prompt}");
        Console.WriteLine($"Run ID: {config.GetEffectiveRunId(prompt)}");
        Console.WriteLine();

        // Results table header
        Console.WriteLine(
            $"{"Benchmark", -35} {"Status", -8} {"Calls", -6} {"Tokens", -8} {"Latency", -10} {"Quality", -8}"
        );
        Console.WriteLine(new string('-', 85));

        var baseline = results.FirstOrDefault(r => r.IsBaseline);

        foreach (var result in results)
        {
            var status = result.Success ? "OK" : "FAIL";
            var calls = result.Metrics.TotalCalls.ToString();
            var tokens = result.Metrics.TotalTokens.ToString();
            var latency = $"{result.Duration.TotalSeconds:F1}s";
            var quality = result.QualityScore != null ? $"{result.QualityScore.Average:F1}/5" : "-";

            Console.WriteLine(
                $"{result.FullName, -35} {status, -8} {calls, -6} {tokens, -8} {latency, -10} {quality, -8}"
            );
        }

        Console.WriteLine();

        // Agent models section
        var resultsWithModels = results
            .Where(r => r.AgentModels != null && r.AgentModels.Count > 0)
            .ToList();
        if (resultsWithModels.Count > 0)
        {
            Console.WriteLine("Agent Models:");
            Console.WriteLine();

            foreach (var result in resultsWithModels)
            {
                Console.WriteLine($"  {result.FullName}:");
                foreach (var (agent, model) in result.AgentModels!)
                {
                    Console.WriteLine($"    {agent, -15} → {model}");
                }
            }

            Console.WriteLine();
        }

        // Comparison with baseline
        if (baseline != null && results.Count > 1)
        {
            Console.WriteLine("Comparison with baseline:");
            Console.WriteLine();

            foreach (var result in results.Where(r => !r.IsBaseline))
            {
                var tokenDiff = result.Metrics.TotalTokens - baseline.Metrics.TotalTokens;
                var tokenPct =
                    baseline.Metrics.TotalTokens > 0
                        ? (double)tokenDiff / baseline.Metrics.TotalTokens * 100
                        : 0;

                var latencyDiff = result.Duration.TotalSeconds - baseline.Duration.TotalSeconds;
                var latencyPct =
                    baseline.Duration.TotalSeconds > 0
                        ? latencyDiff / baseline.Duration.TotalSeconds * 100
                        : 0;

                Console.WriteLine($"  {result.FullName} vs {baseline.FullName}:");
                Console.WriteLine($"    Tokens: {tokenDiff:+#;-#;0} ({tokenPct:+0.0;-0.0;0}%)");
                Console.WriteLine(
                    $"    Latency: {latencyDiff:+0.0;-0.0;0}s ({latencyPct:+0.0;-0.0;0}%)"
                );
                Console.WriteLine(
                    $"    API Calls: {result.Metrics.TotalCalls - baseline.Metrics.TotalCalls:+#;-#;0}"
                );

                if (result.QualityScore != null && baseline.QualityScore != null)
                {
                    var qualityDiff = result.QualityScore.Average - baseline.QualityScore.Average;
                    Console.WriteLine($"    Quality: {qualityDiff:+0.0;-0.0;0}");
                }

                Console.WriteLine();
            }
        }

        return Task.CompletedTask;
    }
}
