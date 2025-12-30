// Pattern 1: Prompt Chaining
// Demonstrates sequential LLM calls where each agent's output feeds into the next.
// Pipeline: Research -> Outline -> Write
//
// Usage:
//   dotnet run                                        # Interactive demo (model from appsettings.json)
//   dotnet run -- --model gpt-4o                      # Interactive demo with explicit model
//   dotnet run -- --benchmark                         # Run benchmarks (config from appsettings.json)
//   dotnet run -- --list-benchmarks                   # List available benchmarks
//   dotnet run -- --evaluate ./runs/<id>              # Evaluate existing run results
//   dotnet run -- --evaluate ./runs/<id> --model gpt-4o  # Evaluate with explicit model

using DotNetAgents.BenchmarkLlm;
using Microsoft.Extensions.Configuration;

// Load configuration from appsettings.json
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", true, false)
    .Build();

// Check for list benchmarks mode
if (args.Contains("--list-benchmarks") || args.Contains("--list"))
{
    BenchmarkLlmHost.ListBenchmarks();
    return;
}

// Check for evaluate mode
var evaluateIndex = Array.IndexOf(args, "--evaluate");
if (evaluateIndex == -1)
{
    evaluateIndex = Array.IndexOf(args, "-e");
}

if (evaluateIndex >= 0 && evaluateIndex < args.Length - 1)
{
    var runPath = args[evaluateIndex + 1];
    // Get model from --model arg or settings
    var modelIndex = Array.IndexOf(args, "--model");
    var settings = configuration.GetSection("BenchmarkLlm").Get<BenchmarkLlmSettings>();
    var model =
        modelIndex >= 0 && modelIndex < args.Length - 1
            ? args[modelIndex + 1]
            : settings?.EvaluationModel ?? settings?.Model;

    if (string.IsNullOrEmpty(model))
    {
        Console.WriteLine("Error: Model is required for evaluation.");
        Console.WriteLine("Use --model <model-id> or set EvaluationModel in appsettings.json");
        return;
    }

    await BenchmarkLlmHost.EvaluateRunAsync(runPath, model);
    return;
}

// Check for benchmark mode
if (args.Contains("--benchmark") || args.Contains("-b"))
{
    var settings =
        configuration.GetSection("BenchmarkLlm").Get<BenchmarkLlmSettings>()
        ?? new BenchmarkLlmSettings();
    await BenchmarkLlmHost.RunAsync(settings);
    return;
}

// Normal interactive mode
var interactiveSettings = configuration.GetSection("BenchmarkLlm").Get<BenchmarkLlmSettings>();
var interactiveModel = interactiveSettings?.Model;

// Check for --model CLI override
var interactiveModelIndex = Array.IndexOf(args, "--model");
if (interactiveModelIndex >= 0 && interactiveModelIndex < args.Length - 1)
{
    interactiveModel = args[interactiveModelIndex + 1];
}

if (string.IsNullOrEmpty(interactiveModel))
{
    Console.WriteLine("Error: Model is required.");
    Console.WriteLine("Set Model in appsettings.json or use --model <model-id>");
    Console.WriteLine("Examples: 'gpt-4o', 'gpt-4o-mini', 'llama3.2'");
}
