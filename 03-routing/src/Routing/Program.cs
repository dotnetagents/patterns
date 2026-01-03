// Pattern 3: Routing
// Demonstrates conditional routing of requests to specialized agents based on classification.
// Pipeline: Classifier -> Switch -> Specialist (Billing/Technical/Account/Product)
//
// Usage:
//   dotnet run -- --provider azure --model gpt-4.1    # Interactive mode
//   dotnet run -- --benchmark                          # Run benchmarks
//   dotnet run -- --list-benchmarks                    # List available benchmarks
//   dotnet run -- --evaluate ./runs/<id>               # Evaluate existing run results

using DotNetAgents.BenchmarkLlm;
using DotNetAgents.Infrastructure;
using DotNetAgents.Patterns.Routing.UseCases.CustomerSupport;
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
    CliArgs.PrintUsage("routing");
    return;
}

Console.WriteLine("Customer Support Routing Demo");
Console.WriteLine("=============================");
Console.WriteLine();
Console.WriteLine("Enter a support ticket to see routing in action.");
Console.WriteLine("Type 'demo' to see sample tickets, 'quit' to exit.");
Console.WriteLine();

while (true)
{
    Console.Write("Ticket> ");
    var input = Console.ReadLine()?.Trim();

    if (string.IsNullOrEmpty(input))
        continue;

    if (input.Equals("quit", StringComparison.OrdinalIgnoreCase) ||
        input.Equals("exit", StringComparison.OrdinalIgnoreCase))
        break;

    if (input.Equals("demo", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine();
        Console.WriteLine("Sample tickets:");
        foreach (var (name, ticket, expectedCategory) in SampleTickets.All)
        {
            var preview = ticket.Split('\n')[0].Trim();
            if (preview.Length > 60)
                preview = preview[..60] + "...";
            Console.WriteLine($"  [{name}] ({expectedCategory}): {preview}");
        }
        Console.WriteLine();
        Console.WriteLine("Type a ticket name to use it (e.g., 'Billing', 'Technical')");
        Console.WriteLine();
        continue;
    }

    // Check if input matches a sample ticket name
    var matchedTicket = SampleTickets.All
        .FirstOrDefault(t => t.Name.Equals(input, StringComparison.OrdinalIgnoreCase));

    var ticketText = matchedTicket != default ? matchedTicket.Ticket : input;

    var config = new RoutingPipelineConfig
    {
        Provider = cli.Provider!,
        ClassifierModel = cli.Model!,
        SpecialistModel = cli.Model!,
    };

    Console.WriteLine();
    Console.WriteLine("Processing ticket...");

    try
    {
        var (workflow, _) = RoutingWorkflow.Create(config);
        var response = await WorkflowRunner.RunAsync(workflow, ticketText);

        Console.WriteLine();
        Console.WriteLine("--- Response ---");
        Console.WriteLine(response);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }

    Console.WriteLine();
}

Console.WriteLine("Thank you for using Customer Support Routing!");
