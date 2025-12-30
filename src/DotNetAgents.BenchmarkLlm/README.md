# BenchmarkLlm

A library for benchmarking LLM workflows. Compare different agent architectures, measure performance metrics, and evaluate output quality using LLM-as-Judge.

## Quick Start

Run benchmarks directly from your pattern project:

```bash
cd patterns/01-prompt-chaining/src/PromptChaining

# List available benchmarks
dotnet run -- --list-benchmarks

# Run benchmarks (config from appsettings.json)
dotnet run -- --benchmark
```

## Setup

### 1. Add Reference

Add a reference to `DotNetAgents.BenchmarkLlm` in your pattern project:

```xml
<ProjectReference Include="..\..\..\src\DotNetAgents.BenchmarkLlm\DotNetAgents.BenchmarkLlm.csproj" />
```

### 2. Add Configuration Packages

```xml
<PackageReference Include="Microsoft.Extensions.Configuration" Version="9.0.*" />
<PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="9.0.*" />
<PackageReference Include="Microsoft.Extensions.Configuration.Binder" Version="9.0.*" />

<ItemGroup>
  <None Update="appsettings.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

### 3. Create appsettings.json

```json
{
  "BenchmarkLlm": {
    "Prompt": "Benefits of Test-Driven Development",
    "Filter": "*",
    "ArtifactsPath": "./runs",
    "Evaluate": false,
    "Exporters": ["console", "markdown"]
  }
}
```

### 4. Update Program.cs

```csharp
using Microsoft.Extensions.Configuration;
using DotNetAgents.BenchmarkLlm;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .Build();

if (args.Contains("--list-benchmarks"))
{
    BenchmarkLlmHost.ListBenchmarks();
    return;
}

if (args.Contains("--benchmark"))
{
    var settings = configuration.GetSection("BenchmarkLlm").Get<BenchmarkLlmSettings>()
        ?? new BenchmarkLlmSettings();
    await BenchmarkLlmHost.RunAsync(settings);
    return;
}

// ... rest of your program
```

## Defining Benchmarks

Create a benchmark class with attributes:

```csharp
using Microsoft.Extensions.AI;
using DotNetAgents.BenchmarkLlm.Core;

[WorkflowBenchmark("my-pattern", Description = "Description of pattern")]
public class MyPatternBenchmarks
{
    [BenchmarkLlm("approach-a", Description = "First approach")]
    public async Task<string> ApproachA(string prompt, IChatClient client)
    {
        return await myWorkflow.GenerateAsync(prompt);
    }

    [BenchmarkLlm("approach-b", Baseline = true, Description = "Baseline approach")]
    public async Task<string> ApproachB(string prompt, IChatClient client)
    {
        return await otherWorkflow.GenerateAsync(prompt);
    }
}
```

### Attributes

**`[WorkflowBenchmark(category)]`** - Marks a class as containing benchmarks
- `category`: Grouping name (e.g., "prompt-chaining", "routing")
- `Description`: Optional description

**`[BenchmarkLlm(name)]`** - Marks a method as a benchmark
- `name`: Benchmark identifier
- `Description`: Optional description
- `Baseline`: Set to `true` to mark as the baseline for comparison

### Benchmark Method Signature

```csharp
public async Task<string> BenchmarkMethod(string prompt, IChatClient client)
```

## Configuration Options

| Setting | Description | Default |
|---------|-------------|---------|
| `Prompt` | Input prompt for content generation | Required |
| `Filter` | Glob pattern to select benchmarks | `*` |
| `ArtifactsPath` | Output directory for results | `./runs` |
| `RunId` | Custom run identifier | Auto-generated |
| `Evaluate` | Enable LLM-as-Judge quality scoring | `false` |
| `Exporters` | List of exporters: console, markdown, json | `["console"]` |

## Output Structure

Results are saved to the artifacts directory with timestamped folders:

```
runs/
└── 2024-12-30_143022_benefits-of-tdd/
    ├── run-config.json           # Input configuration
    ├── environment.json          # Provider, model, runtime info
    ├── my-pattern/
    │   ├── approach-a/
    │   │   ├── output.md         # Generated content
    │   │   └── metrics.json      # Timing and token metrics
    │   └── approach-b/
    │       ├── output.md
    │       └── metrics.json
    ├── results.json              # Full structured results
    ├── comparison.md             # Markdown comparison table
    └── evaluation.json           # Quality scores (if Evaluate=true)
```

## Environment Configuration

Configure the LLM provider via environment variables or `.env` file:

```bash
LLM_PROVIDER=openai    # ollama (default), openai, openrouter, azure
API_KEY=sk-...         # Not needed for Ollama
MODEL=gpt-4o-mini
ENDPOINT=...           # Required for Azure, optional for Ollama
```

## Per-Agent Model Configuration

Override the model for specific agents in code:

```csharp
using DotNetAgents.Infrastructure;

var defaultClient = ChatClientFactory.Create();
var writerClient = ChatClientFactory.Create("gpt-4o");  // Use stronger model
```

## Metrics Collected

Each benchmark run collects:

- **Timing**: Total duration, time to first token
- **Tokens**: Input tokens, output tokens, total tokens
- **Calls**: Number of LLM API calls
- **Quality** (with Evaluate=true): Completeness, Structure, Accuracy, Engagement scores (1-5)

## LLM-as-Judge Evaluation

When `Evaluate` is enabled, a separate LLM call evaluates each output on:

| Dimension | Description |
|-----------|-------------|
| Completeness | Coverage of key points |
| Structure | Organization and flow |
| Accuracy | Factual correctness |
| Engagement | Writing quality and readability |

Scores are 1-5 with an overall average.
