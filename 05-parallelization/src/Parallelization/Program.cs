// Pattern 5: Parallelization
// Demonstrates running multiple agents in parallel using the Microsoft Agent Framework's
// native Fan-Out/Fan-In workflow pattern.
// Use Case: Travel planning with parallel research agents for hotels, transport, and activities.
//
// Key Framework Features:
//   - AddFanOutEdge: Distributes input to multiple agents simultaneously
//   - AddFanInEdge: Collects results from multiple agents for aggregation
//
// Usage:
//   dotnet run -- --provider azure --model gpt-4.1    # Interactive mode
//   dotnet run -- --benchmark                          # Run benchmarks
//   dotnet run -- --list-benchmarks                    # List available benchmarks
//   dotnet run -- --evaluate ./runs/<id>               # Evaluate existing run results

using DotNetAgents.BenchmarkLlm;
using DotNetAgents.Infrastructure;
using DotNetAgents.Patterns.Parallelization.Services;
using DotNetAgents.Patterns.Parallelization.UseCases.TravelPlanning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var cli = new CliArgs(args);

// Load .env file early (before checking for API keys)
ChatClientFactory.EnsureEnvLoaded();

// Load configuration from appsettings.json
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", true, false)
    .Build();

// Set up dependency injection with IHttpClientFactory
var services = new ServiceCollection();

// Register HttpClient for GoogleSearchService via IHttpClientFactory
services.AddHttpClient<GoogleSearchService>();

// Register search services
services.AddSingleton<MockGoogleSearchService>();

// Build service provider
var serviceProvider = services.BuildServiceProvider();

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
    CliArgs.PrintUsage("parallelization");
    return;
}

Console.WriteLine("=== Travel Planning Agent (Parallelization Pattern) ===");
Console.WriteLine();
Console.WriteLine("Uses Microsoft Agent Framework's Fan-Out/Fan-In pattern:");
Console.WriteLine("  - FanOutExecutor broadcasts query to 3 agents in parallel");
Console.WriteLine("  - AddFanOutEdge → [Hotels, Transport, Activities] agents");
Console.WriteLine("  - AddFanInEdge → FanInExecutor collects and synthesizes");
Console.WriteLine();

// Determine which search service to use
IGoogleSearchService searchService;
var googleApiKey = Environment.GetEnvironmentVariable("GOOGLE_SEARCH_API_KEY");

if (!string.IsNullOrEmpty(googleApiKey))
{
    Console.WriteLine("Using real Google Custom Search API.");
    searchService = serviceProvider.GetRequiredService<GoogleSearchService>();
}
else
{
    Console.WriteLine("GOOGLE_SEARCH_API_KEY not set. Using mock search service.");
    Console.WriteLine("Set GOOGLE_SEARCH_API_KEY and GOOGLE_SEARCH_ENGINE_ID for real web searches.");
    searchService = serviceProvider.GetRequiredService<MockGoogleSearchService>();
}

Console.WriteLine();
Console.WriteLine("Enter your travel planning request (or 'quit' to exit):");
Console.WriteLine();

while (true)
{
    Console.Write("> ");
    var input = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(input))
    {
        continue;
    }

    if (input.Equals("quit", StringComparison.OrdinalIgnoreCase) ||
        input.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    Console.WriteLine();
    Console.WriteLine("Planning your trip using parallel agents (Fan-Out/Fan-In pattern)...");
    Console.WriteLine();

    var config = new TravelPlanningConfig
    {
        Provider = cli.Provider!,
        HotelsModel = cli.Model!,
        TransportModel = cli.Model!,
        ActivitiesModel = cli.Model!,
        AggregatorModel = cli.Model!
    };

    try
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Create workflow using framework's parallel execution
        var (workflow, agentModels) = ParallelTravelPipeline.Create(config, searchService);

        // Run workflow using WorkflowRunner
        var result = await WorkflowRunner.RunAsync(workflow, input);

        stopwatch.Stop();

        Console.WriteLine("=== Travel Plan ===");
        Console.WriteLine();
        Console.WriteLine(result);
        Console.WriteLine();
        Console.WriteLine($"Generated in {stopwatch.Elapsed.TotalSeconds:F1} seconds");
        Console.WriteLine($"Agents used: {string.Join(", ", agentModels.Select(kvp => $"{kvp.Key} ({kvp.Value})"))}");
        Console.WriteLine();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
        Console.WriteLine();
    }
}

Console.WriteLine("Goodbye!");
