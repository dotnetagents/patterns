// Pattern 2: Tool Use
// Demonstrates AI agents calling external functions/APIs.
// E-commerce scenario: Product search, shopping cart, checkout
//
// Usage:
//   dotnet run -- --provider azure --model gpt-4.1    # Interactive mode
//   dotnet run -- --benchmark                          # Run all benchmarks
//   dotnet run -- --benchmark --filter "with-tools"    # Run filtered benchmarks
//   dotnet run -- --list-benchmarks                    # List available benchmarks
//   dotnet run -- --evaluate ./runs/<id>               # Evaluate existing run results

using DotNetAgents.BenchmarkLlm;
using DotNetAgents.Infrastructure;
using DotNetAgents.Patterns.ToolUse.Data;
using DotNetAgents.Patterns.ToolUse.Services;
using DotNetAgents.Patterns.ToolUse.UseCases.ShoppingAssistant;
using Microsoft.EntityFrameworkCore;
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
        settings.Filter = cli.Filter;

    await BenchmarkLlmHost.RunAsync(settings);
    return;
}

// Interactive mode
if (!cli.ValidateInteractiveArgs(out var error))
{
    Console.WriteLine(error);
    Console.WriteLine();
    CliArgs.PrintUsage("tool-use");
    return;
}

// Setup SQLite database
var dbPath = Path.Combine(AppContext.BaseDirectory, "ecommerce.db");
var dbOptions = new DbContextOptionsBuilder<ECommerceDbContext>()
    .UseSqlite($"Data Source={dbPath}")
    .Options;

await using var context = new ECommerceDbContext(dbOptions);
await context.Database.EnsureCreatedAsync();
DbInitializer.Initialize(context);

// Setup services
var productService = new ProductService(context);
var cartService = new CartService(context);
var orderService = new OrderService(context);

// Create agent with tools
var (client, chatOptions, _) = ShoppingAgentPipeline.Create(
    new ShoppingAgentPipelineConfig
    {
        Provider = cli.Provider!,
        Model = cli.Model!,
        ProductService = productService,
        CartService = cartService,
        OrderService = orderService,
    }
);

Console.WriteLine("Try asking: \"What books do you have under $50?\"");
Console.WriteLine();

await InteractiveRunner.RunAsync(
    client,
    ShoppingAgentPipeline.GetSystemPrompt(),
    "Shopping Assistant (Tool Use Pattern)",
    cli.Provider!,
    cli.Model!,
    chatOptions,
    async () => await cartService.ClearCartAsync("customer-001"));

Console.WriteLine("Thank you for shopping with us!");
