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
    "Filter": "*",
    "ArtifactsPath": "./runs",
    "Evaluate": false,
    "EvaluationProvider": "azure",
    "EvaluationModel": "gpt-4o-mini",
    "EvaluatorType": "content",
    "Exporters": ["console", "markdown"]
  }
}
```

Note: The prompt is defined in the `[WorkflowBenchmark]` attribute, not in settings.

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
| `Filter` | Glob pattern to select benchmarks | `*` |
| `ArtifactsPath` | Output directory for results | `./runs` |
| `RunId` | Custom run identifier | Auto-generated |
| `Evaluate` | Enable LLM-as-Judge quality scoring | `false` |
| `EvaluationProvider` | Provider for evaluation (required when Evaluate=true) | - |
| `EvaluationModel` | Model for evaluation (required when Evaluate=true) | - |
| `EvaluatorType` | Evaluator type: "content" or "task" | `"content"` |
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

Provider is specified in code via `AgentConfig.Provider`. Configure credentials via environment variables or `.env` file:

```bash
# Azure OpenAI
AZURE_OPENAI_API_KEY=your-key
AZURE_OPENAI_ENDPOINT=https://your-resource.openai.azure.com/

# OpenAI
OPENAI_API_KEY=sk-...

# Ollama (local, no key needed)
OLLAMA_ENDPOINT=http://localhost:11434

# OpenRouter
OPENROUTER_API_KEY=sk-or-...

# GitHub Models
GITHUB_TOKEN=github_pat_...
```

## Per-Agent Model Configuration

Override the model for specific agents in code:

```csharp
using DotNetAgents.Infrastructure;

var defaultClient = ChatClientFactory.Create("azure", "gpt-4o-mini");
var writerClient = ChatClientFactory.Create("azure", "gpt-4o");  // Use stronger model
```

## Metrics Collected

Metrics are collected automatically via OpenTelemetry instrumentation. Each benchmark run captures:

- **Timing**: Total duration, latency per call
- **Tokens**: Input tokens, output tokens, total tokens
- **Calls**: Number of LLM API calls
- **Quality** (with Evaluate=true): Completeness, Structure, Accuracy, Engagement, Evidence, Balance, Actionability, Depth scores (1-5)

## LLM-as-Judge Evaluation

When `Evaluate` is enabled, a separate LLM call evaluates each output. Two evaluator types are available:

### Content Evaluator (default)
For content generation tasks like articles and summaries. Set `EvaluatorType: "content"`.

### Agent Task Evaluator
For tool-use and multi-step agent workflows. Set `EvaluatorType: "task"`. Focuses on task completion rather than writing quality.

Both evaluators score on 8 dimensions (1-5 scale):

| Dimension | Description |
|-----------|-------------|
| Completeness | Coverage of topic/task completion |
| Structure | Organization and logical flow |
| Accuracy | Factual/decision correctness |
| Engagement | Communication quality |
| Evidence Quality | Use of data/tool responses |
| Balance | Appropriate scope |
| Actionability | Practical guidance/actions taken |
| Depth | Handling of details and edge cases |

Scores are 1-5 with an overall average.
