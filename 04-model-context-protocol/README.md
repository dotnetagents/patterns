# Pattern 04: Model Context Protocol (MCP)

Demonstrates **MCP** - an open standard for connecting AI applications to external tools and data sources.

## What is MCP?

The Model Context Protocol enables AI applications to connect to external services through a standardized interface:

- **MCP Server**: Exposes tools via JSON-RPC 2.0 protocol
- **MCP Client**: Discovers and invokes tools from servers
- **Transport**: How client and server communicate (Stdio, HTTP)

## Quick Start

```bash
# Build the projects
cd patterns/04-model-context-protocol/src
dotnet build

# Run the demo
cd McpPattern
dotnet run -- --provider azure --model gpt-4.1
```

This launches the calculator MCP server automatically as a subprocess and connects to it.

## How It Works

### 1. MCP Server Defines Tools

Tools are exposed using attributes:

```csharp
[McpServerToolType]
public static class CalculatorTools
{
    [McpServerTool, Description("Add two numbers together")]
    public static double Add(
        [Description("First number")] double a,
        [Description("Second number")] double b) => a + b;

    [McpServerTool, Description("Calculate factorial of a number")]
    public static long Factorial([Description("Non-negative integer")] int n)
    {
        if (n < 0) throw new ArgumentException("Must be non-negative");
        long result = 1;
        for (int i = 2; i <= n; i++) result *= i;
        return result;
    }
}
```

### 2. Client Connects via Stdio Transport

```csharp
// Create transport - launches server as subprocess
var transport = new StdioClientTransport(new StdioClientTransportOptions
{
    Name = "CalculatorServer",
    Command = "dotnet",
    Arguments = ["run", "--project", "./McpCalculatorServer", "--no-build"]
});

// Connect and discover tools
var mcpClient = await McpClient.CreateAsync(transport);
var tools = await mcpClient.ListToolsAsync();
```

### 3. Integrate with AI Agent

MCP tools implement `AITool`, so they integrate directly with Microsoft.Extensions.AI:

```csharp
var client = new ChatClientBuilder(baseClient)
    .UseFunctionInvocation()
    .Build();

var options = new ChatOptions { Tools = [.. tools] };
var response = await client.GetResponseAsync(messages, options);
```

## Calculator Server Tools

The included calculator server exposes 11 tools:

| Tool | Description |
|------|-------------|
| `add`, `subtract`, `multiply`, `divide` | Basic arithmetic |
| `power`, `square_root`, `factorial` | Advanced math |
| `absolute_value`, `natural_log`, `sine`, `cosine` | Additional functions |

## Resources

- [MCP Specification](https://modelcontextprotocol.io/specification/2025-11-25)
- [MCP C# SDK](https://github.com/modelcontextprotocol/csharp-sdk)
