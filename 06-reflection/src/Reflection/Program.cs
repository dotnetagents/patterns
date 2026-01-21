// Pattern 6: Reflection
// Demonstrates iterative generate-critique-refine cycles where a critic agent
// evaluates and provides feedback to improve output quality.
// Use Case: Startup Pitch Perfector (PitchWriter + VCCritic)
//
// Usage:
//   dotnet run -- --provider azure --model gpt-4.1    # Interactive mode
//   dotnet run -- --benchmark                          # Run benchmarks
//   dotnet run -- --list-benchmarks                    # List available benchmarks
//   dotnet run -- --evaluate ./runs/<id>               # Evaluate existing run results

using DotNetAgents.BenchmarkLlm;
using DotNetAgents.Infrastructure;
using DotNetAgents.Patterns.Reflection.UseCases.StartupPitch;
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
    CliArgs.PrintUsage("reflection");
    return;
}

Console.WriteLine("Pattern 6: Reflection - Startup Pitch Perfector");
Console.WriteLine("================================================");
Console.WriteLine();
Console.WriteLine("This pattern demonstrates iterative refinement through critique.");
Console.WriteLine("A PitchWriter agent generates a startup pitch, and a VCCritic agent");
Console.WriteLine("evaluates it. The process repeats until the pitch is approved or");
Console.WriteLine("the maximum iterations are reached.");
Console.WriteLine();

// Default startup idea for interactive demo (Swiss market)
var startupIdea = "An AI-powered platform connecting travelers with sustainable Alpine experiences in Switzerland. " +
                  "Features eco-certified accommodations, local mountain guides, farm-to-table restaurants, and " +
                  "carbon-neutral transport options. Uses AI to create personalized itineraries that minimize " +
                  "environmental impact while maximizing authentic Swiss mountain culture experiences. Target market: " +
                  "environmentally-conscious international travelers seeking premium sustainable tourism in the Swiss Alps.";

Console.WriteLine($"Startup Idea: {startupIdea}");
Console.WriteLine();
Console.WriteLine("Running two-agent reflection pipeline (PitchWriter + VCCritic)...");
Console.WriteLine();

var (workflow, _) = ReflectionPipeline.Create(
    new ReflectionPipelineConfig
    {
        Provider = cli.Provider!,
        WriterModel = cli.Model!,
        CriticModel = cli.Model!,
        MaxIterations = 3
    }
);

var content = await WorkflowRunner.RunAsync(workflow, startupIdea);

Console.WriteLine();
Console.WriteLine("=".PadRight(60, '='));
Console.WriteLine("FINAL PITCH");
Console.WriteLine("=".PadRight(60, '='));
Console.WriteLine();
Console.WriteLine(content);
