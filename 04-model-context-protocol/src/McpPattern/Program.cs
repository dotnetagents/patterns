// Pattern 4: Model Context Protocol (MCP)
// Demonstrates connecting to an MCP server via Stdio transport
//
// Usage:
//   dotnet run -- --provider azure --model gpt-4.1
//   dotnet run -- --help

using DotNetAgents.Infrastructure;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;

var cli = new CliArgs(args);

if (cli.HasFlag("--help") || cli.HasFlag("-h"))
{
    PrintUsage();
    return;
}

if (!cli.ValidateInteractiveArgs(out var error))
{
    Console.WriteLine(error);
    return;
}

Console.WriteLine("=== MCP Calculator Demo (Stdio Transport) ===");
Console.WriteLine();

// Determine path to MCP Calculator Server project
var mcpServerPath = GetMcpServerPath();
Console.WriteLine($"MCP Server Path: {mcpServerPath}");
Console.WriteLine();

// Create Stdio transport - this launches the server as a subprocess
var transport = new StdioClientTransport(
    new StdioClientTransportOptions
    {
        Name = "CalculatorServer",
        Command = "dotnet",
        Arguments = ["run", "--project", mcpServerPath, "--no-build"],
    }
);

Console.WriteLine("Starting MCP Calculator Server...");

// Create MCP client and connect to server
var mcpClient = await McpClient.CreateAsync(transport);

await using (mcpClient)
{
    // Discover all tools exposed by the MCP server
    var tools = await mcpClient.ListToolsAsync();
    PrintDiscoveredTools(tools);

    // Create chat client with automatic function invocation
    var baseClient = ChatClientFactory.Create(cli.Provider!, cli.Model!);
    var client = new ChatClientBuilder(baseClient)
        .UseFunctionInvocation()
        .Build();

    // Pass MCP tools to the chat options
    var options = new ChatOptions { Tools = [.. tools] };

    Console.WriteLine("Try asking: \"What is the factorial of 5 plus the square root of 144?\"");
    Console.WriteLine();

    // Run interactive session
    await InteractiveRunner.RunAsync(
        client,
        GetSystemPrompt(),
        "MCP Calculator (Stdio Transport)",
        cli.Provider!,
        cli.Model!,
        options
    );
}

Console.WriteLine();
Console.WriteLine("Calculator session ended.");

static void PrintUsage()
{
    Console.WriteLine("MCP Pattern Demo - Calculator Server");
    Console.WriteLine("=====================================");
    Console.WriteLine();
    Console.WriteLine("Usage: dotnet run -- [options]");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  --provider, -p <name>   LLM provider (azure, openai, ollama, github)");
    Console.WriteLine("  --model, -m <name>      Model name (e.g., gpt-4.1)");
    Console.WriteLine("  --help, -h              Show this help message");
    Console.WriteLine();
    Console.WriteLine("Example:");
    Console.WriteLine("  dotnet run -- --provider azure --model gpt-4.1");
    Console.WriteLine();
    Console.WriteLine("This demo connects to a local MCP Calculator server via Stdio transport.");
    Console.WriteLine("The server is launched automatically as a subprocess.");
}

static string GetMcpServerPath()
{
    return Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "McpCalculatorServer")
    );
}

static void PrintDiscoveredTools(IList<McpClientTool> tools)
{
    Console.WriteLine($"Discovered {tools.Count} MCP tools:");
    foreach (var tool in tools)
    {
        Console.WriteLine($"  - {tool.Name}: {tool.Description}");
    }
    Console.WriteLine();
}

static string GetSystemPrompt() => """
    You are a helpful math assistant with access to calculator tools via MCP.

    When users ask math questions:
    1. Use the available calculator tools to compute results
    2. Show your work by explaining which operations you performed and which tools you called
    3. Provide clear, accurate answers

    Available operations include basic arithmetic, powers, roots, factorials,
    logarithms, and trigonometric functions.
    """;
