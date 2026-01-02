// Pattern 1: Prompt Chaining
// Demonstrates sequential LLM calls where each agent's output feeds into the next.
// Pipeline: Research -> Outline -> Write
//
// Usage:
//   dotnet run -- --provider azure --model gpt-4.1    # Interactive mode
//   dotnet run -- --benchmark                          # Run benchmarks
//   dotnet run -- --list-benchmarks                    # List available benchmarks
//   dotnet run -- --evaluate ./runs/<id>               # Evaluate existing run results

using DotNetAgents.BenchmarkLlm;
using DotNetAgents.Infrastructure;
using DotNetAgents.Patterns.PromptChaining.UseCases.ContentGeneration;
using Microsoft.Extensions.Configuration;

var cli = new CliArgs(args);

// Load configuration from appsettings.json
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", true, false)
    .Build();

// List benchmarks mode
if (cli.IsList)
{
    BenchmarkLlmHost.ListBenchmarks();
    return;
}

// Evaluate mode
if (cli.EvaluatePath != null)
{
    var settings = configuration.GetSection("BenchmarkLlm").Get<BenchmarkLlmSettings>();
    var model = cli.Model ?? settings?.EvaluationModel;

    if (string.IsNullOrEmpty(model))
    {
        Console.WriteLine("Error: Model is required for evaluation.");
        Console.WriteLine("Use --model <model-id> or set EvaluationModel in appsettings.json");
        return;
    }

    await BenchmarkLlmHost.EvaluateRunAsync(cli.EvaluatePath, model);
    return;
}

// Benchmark mode
if (cli.IsBenchmark)
{
    var settings = configuration.GetSection("BenchmarkLlm").Get<BenchmarkLlmSettings>()
                   ?? new BenchmarkLlmSettings();

    if (cli.Filter != null)
    {
        settings.Filter = cli.Filter;
    }

    await BenchmarkLlmHost.RunAsync(settings);
    return;
}

// Interactive mode
if (!cli.ValidateInteractiveArgs(out var error))
{
    Console.WriteLine(error);
    Console.WriteLine();
    CliArgs.PrintUsage("prompt-chaining");
    return;
}

var (workflow, _) = MultiAgentContentPipeline.Create(
    new MultiAgentContentPipelineConfig
    {
        Provider = cli.Provider!,
        ResearcherModel = cli.Model!,
        OutlinerModel = cli.Model!,
        WriterModel = cli.Model!
    }
);

var content = await WorkflowRunner.RunAsync(
    workflow,
    "The benefits of test-driven development in software engineering");
Console.WriteLine("Generated Content:");
Console.WriteLine(content);