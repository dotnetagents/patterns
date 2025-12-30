using DotNetAgents.BenchmarkLlm.Core;
using DotNetAgents.BenchmarkLlm.Evaluation;
using DotNetAgents.BenchmarkLlm.Export;
using DotNetAgents.BenchmarkLlm.Storage;
using DotNetAgents.Infrastructure;

namespace DotNetAgents.BenchmarkLlm;

/// <summary>
/// Entry point for running BenchmarkLlm from a pattern project.
/// </summary>
public static class BenchmarkLlmHost
{
    /// <summary>
    /// Runs all benchmarks matching the settings configuration.
    /// </summary>
    public static async Task RunAsync(
        BenchmarkLlmSettings settings,
        CancellationToken cancellationToken = default
    )
    {
        // EvaluationModel is required only when evaluation is enabled
        if (settings.Evaluate && string.IsNullOrEmpty(settings.EvaluationModel))
        {
            Console.WriteLine("Error: EvaluationModel is required when Evaluate is true.");
            Console.WriteLine("Examples: 'gpt-4o', 'gpt-4o-mini'");
            return;
        }

        PrintHeader();

        // Discover benchmarks
        var discovery = new BenchmarkLlmDiscovery();
        var allBenchmarks = discovery.DiscoverAll();
        var benchmarks = discovery.Filter(allBenchmarks, settings.Filter);

        if (benchmarks.Count == 0)
        {
            Console.WriteLine($"No benchmarks found matching filter: {settings.Filter}");
            Console.WriteLine("Use --list-benchmarks to see available benchmarks.");
            return;
        }

        // Get prompt from first benchmark (defined in WorkflowBenchmark attribute)
        var prompt = benchmarks[0].Prompt;

        Console.WriteLine($"Prompt: {prompt}");
        Console.WriteLine($"Filter: {settings.Filter}");
        Console.WriteLine($"Benchmarks to run: {benchmarks.Count}");
        if (settings.Evaluate)
        {
            Console.WriteLine($"Evaluation model: {settings.EvaluationModel}");
        }
        Console.WriteLine();

        // Create config
        var config = new BenchmarkLlmConfig
        {
            Filter = settings.Filter,
            RunId = settings.RunId,
            ArtifactsPath = settings.ArtifactsPath,
            Evaluate = settings.Evaluate,
            Exporters = settings.Exporters,
        };

        // Create evaluator (benchmarks create their own clients via ChatClientFactory)
        IContentEvaluator? evaluator = settings.Evaluate
            ? new LlmJudgeEvaluator(ChatClientFactory.Create(settings.EvaluationModel!))
            : null;

        // Run benchmarks
        var runner = new BenchmarkLlmRunner(evaluator);
        var results = await runner.RunAsync(benchmarks, config, cancellationToken);

        // Create run directory and save outputs
        var runManager = new RunManager();
        var runPath = runManager.CreateRunDirectory(config, prompt);

        await runManager.SaveConfigAsync(config, prompt, runPath);
        await runManager.SaveEnvironmentAsync(runPath);

        foreach (var result in results)
        {
            await runManager.SaveOutputAsync(result, runPath);
        }

        // Export results
        var exporters = GetExporters(settings.Exporters);
        foreach (var exporter in exporters)
        {
            await exporter.ExportAsync(results, config, runPath);
        }

        // Run comparative analysis if evaluation is enabled and we have multiple results
        if (settings.Evaluate && results.Count > 1)
        {
            Console.WriteLine();
            Console.WriteLine("Running comparative analysis...");

            var comparativeEvaluator = new ComparativeEvaluator(
                ChatClientFactory.Create(settings.EvaluationModel!)
            );
            var analysis = await comparativeEvaluator.CompareAsync(results, cancellationToken);

            var analysisExporter = new AnalysisExporter();
            await analysisExporter.ExportAsync(analysis, runPath);
        }

        Console.WriteLine();
        Console.WriteLine($"Run saved to: {runPath}");
    }

    /// <summary>
    /// Evaluates existing benchmark results from a run directory.
    /// </summary>
    /// <param name="runPath">Path to the run directory containing benchmark outputs.</param>
    /// <param name="model">The model to use for evaluation (e.g., "gpt-4o", "gpt-4o-mini").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task EvaluateRunAsync(
        string runPath,
        string model,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrEmpty(model))
        {
            Console.WriteLine("Error: Model is required for evaluation.");
            Console.WriteLine("Examples: 'gpt-4o', 'gpt-4o-mini', 'llama3.2'");
            return;
        }

        PrintHeader();
        PrintEnvironmentInfo(model);

        if (!Directory.Exists(runPath))
        {
            Console.WriteLine($"Error: Run directory not found: {runPath}");
            return;
        }

        Console.WriteLine($"Evaluating run: {runPath}");
        Console.WriteLine();

        // Read run config to get the prompt
        var configPath = Path.Combine(runPath, "run-config.json");
        if (!File.Exists(configPath))
        {
            Console.WriteLine($"Error: run-config.json not found in {runPath}");
            return;
        }

        var configJson = await File.ReadAllTextAsync(configPath, cancellationToken);
        var configDoc = System.Text.Json.JsonDocument.Parse(configJson);
        var prompt = configDoc.RootElement.GetProperty("prompt").GetString() ?? "";

        Console.WriteLine($"Prompt: {prompt}");
        Console.WriteLine();

        // Create evaluator
        var evaluator = new LlmJudgeEvaluator(ChatClientFactory.Create(model));

        // Find all benchmark output files
        var results =
            new List<(string category, string benchmark, string content, QualityScore? score)>();

        foreach (var categoryDir in Directory.GetDirectories(runPath))
        {
            var category = Path.GetFileName(categoryDir);
            if (category == "." || category == "..")
                continue;

            foreach (var benchmarkDir in Directory.GetDirectories(categoryDir))
            {
                var benchmarkName = Path.GetFileName(benchmarkDir);
                var outputPath = Path.Combine(benchmarkDir, "output.md");

                if (!File.Exists(outputPath))
                {
                    Console.WriteLine(
                        $"  Skipping {category}/{benchmarkName} - no output.md found"
                    );
                    continue;
                }

                var content = await File.ReadAllTextAsync(outputPath, cancellationToken);
                Console.WriteLine($"  Evaluating {category}/{benchmarkName}...");

                try
                {
                    var score = await evaluator.EvaluateAsync(prompt, content, cancellationToken);
                    results.Add((category, benchmarkName, content, score));
                    Console.WriteLine($"    Quality: {score.Average:F1}/5");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"    Evaluation failed: {ex.Message}");
                    results.Add((category, benchmarkName, content, null));
                }
            }
        }

        // Save evaluation results
        var evaluationPath = Path.Combine(runPath, "evaluation.json");
        var evaluationData = results.Select(r => new
        {
            Category = r.category,
            Benchmark = r.benchmark,
            Quality = r.score != null
                ? new
                {
                    r.score.Completeness,
                    r.score.Structure,
                    r.score.Accuracy,
                    r.score.Engagement,
                    r.score.EvidenceQuality,
                    r.score.Balance,
                    r.score.Actionability,
                    r.score.Depth,
                    r.score.Average,
                    r.score.Reasoning,
                }
                : null,
        });

        var json = System.Text.Json.JsonSerializer.Serialize(
            evaluationData,
            new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            }
        );
        await File.WriteAllTextAsync(evaluationPath, json, cancellationToken);

        // Generate markdown report
        var mdBuilder = new System.Text.StringBuilder();
        mdBuilder.AppendLine($"# Evaluation Results: {prompt}");
        mdBuilder.AppendLine();
        mdBuilder.AppendLine($"**Evaluation Model:** {model}");
        mdBuilder.AppendLine($"**Timestamp:** {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        mdBuilder.AppendLine();

        mdBuilder.AppendLine("## Quality Scores");
        mdBuilder.AppendLine();
        mdBuilder.AppendLine(
            "| Benchmark | Compl | Struct | Accur | Engage | Evid | Bal | Action | Depth | **Avg** |"
        );
        mdBuilder.AppendLine(
            "|-----------|:-----:|:------:|:-----:|:------:|:----:|:---:|:------:|:-----:|:-------:|"
        );

        foreach (var (category, benchmark, _, score) in results)
        {
            if (score != null)
            {
                mdBuilder.AppendLine(
                    $"| {category}/{benchmark} | {score.Completeness} | {score.Structure} | {score.Accuracy} | {score.Engagement} | {score.EvidenceQuality} | {score.Balance} | {score.Actionability} | {score.Depth} | **{score.Average:F1}** |"
                );
            }
            else
            {
                mdBuilder.AppendLine(
                    $"| {category}/{benchmark} | - | - | - | - | - | - | - | - | **-** |"
                );
            }
        }

        mdBuilder.AppendLine();
        mdBuilder.AppendLine(
            "*Compl=Completeness, Struct=Structure, Accur=Accuracy, Engage=Engagement, Evid=Evidence, Bal=Balance, Action=Actionability*"
        );
        mdBuilder.AppendLine();

        // Add reasoning for each benchmark
        var resultsWithReasoning = results.Where(r => r.score?.Reasoning != null).ToList();
        if (resultsWithReasoning.Count > 0)
        {
            mdBuilder.AppendLine("## Evaluation Reasoning");
            mdBuilder.AppendLine();

            foreach (var (category, benchmark, _, score) in resultsWithReasoning)
            {
                mdBuilder.AppendLine($"### {category}/{benchmark}");
                mdBuilder.AppendLine();
                mdBuilder.AppendLine(score!.Reasoning);
                mdBuilder.AppendLine();
            }
        }

        mdBuilder.AppendLine("---");
        mdBuilder.AppendLine();
        mdBuilder.AppendLine("*Generated by BenchmarkLlm Evaluator*");

        var mdPath = Path.Combine(runPath, "evaluation.md");
        await File.WriteAllTextAsync(mdPath, mdBuilder.ToString(), cancellationToken);

        Console.WriteLine();
        Console.WriteLine($"Evaluation saved to: {evaluationPath}");
        Console.WriteLine($"Markdown report saved to: {mdPath}");

        // Print summary
        Console.WriteLine();
        Console.WriteLine(
            "╔═══════════════════════════════════════════════════════════════════════════════════════════════════╗"
        );
        Console.WriteLine(
            "║                                    EVALUATION SUMMARY                                             ║"
        );
        Console.WriteLine(
            "╚═══════════════════════════════════════════════════════════════════════════════════════════════════╝"
        );
        Console.WriteLine();
        Console.WriteLine(
            $"{"Benchmark", -35} {"C", -3} {"S", -3} {"A", -3} {"E", -3} {"Ev", -3} {"B", -3} {"Ac", -3} {"D", -3} {"Avg", -5}"
        );
        Console.WriteLine(new string('-', 68));

        foreach (var (category, benchmark, _, score) in results)
        {
            if (score != null)
            {
                Console.WriteLine(
                    $"{category}/{benchmark, -33} {score.Completeness, -3} {score.Structure, -3} {score.Accuracy, -3} {score.Engagement, -3} {score.EvidenceQuality, -3} {score.Balance, -3} {score.Actionability, -3} {score.Depth, -3} {score.Average:F1}"
                );
            }
            else
            {
                Console.WriteLine(
                    $"{category}/{benchmark, -33} {"--", -3} {"--", -3} {"--", -3} {"--", -3} {"--", -3} {"--", -3} {"--", -3} {"--", -3} {"--", -5}"
                );
            }
        }

        Console.WriteLine();
        Console.WriteLine(
            "C=Completeness, S=Structure, A=Accuracy, E=Engagement, Ev=Evidence, B=Balance, Ac=Actionability, D=Depth"
        );
    }

    /// <summary>
    /// Lists all available benchmarks discovered in the current assembly.
    /// </summary>
    public static void ListBenchmarks()
    {
        PrintHeader();

        var discovery = new BenchmarkLlmDiscovery();
        var allBenchmarks = discovery.DiscoverAll();

        Console.WriteLine("Available benchmarks:");
        Console.WriteLine();

        var grouped = allBenchmarks.GroupBy(b => b.Category);
        foreach (var group in grouped)
        {
            Console.WriteLine($"  {group.Key}/");
            foreach (var b in group)
            {
                var baseline = b.IsBaseline ? " (baseline)" : "";
                var desc = b.Description != null ? $" - {b.Description}" : "";
                Console.WriteLine($"    {b.Name}{baseline}{desc}");
            }
        }

        Console.WriteLine();
        Console.WriteLine($"Total: {allBenchmarks.Count} benchmarks");
    }

    private static void PrintHeader()
    {
        Console.WriteLine(
            "╔══════════════════════════════════════════════════════════════════════╗"
        );
        Console.WriteLine(
            "║                        BenchmarkLlm                                  ║"
        );
        Console.WriteLine(
            "╚══════════════════════════════════════════════════════════════════════╝"
        );
        Console.WriteLine();
    }

    private static void PrintEnvironmentInfo(string model)
    {
        Console.WriteLine(
            $"Provider: {Environment.GetEnvironmentVariable("LLM_PROVIDER") ?? "ollama"}"
        );
        Console.WriteLine($"Model: {model}");
        Console.WriteLine();
    }

    private static IReadOnlyList<IResultExporter> GetExporters(IEnumerable<string> names)
    {
        var exporters = new List<IResultExporter>();
        foreach (var name in names)
        {
            switch (name.ToLowerInvariant().Trim())
            {
                case "console":
                    exporters.Add(new ConsoleExporter());
                    break;
                case "markdown":
                case "md":
                    exporters.Add(new MarkdownExporter());
                    break;
                case "json":
                    exporters.Add(new JsonExporter());
                    break;
            }
        }
        return exporters;
    }
}
