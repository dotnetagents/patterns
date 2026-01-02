# DotNetAgents Patterns

Educational repository demonstrating **21 agentic AI patterns** using Microsoft's Agent Framework for .NET.

- **Framework**: Microsoft.Agents.AI (preview)
- **Runtime**: .NET 10.0
- **Current Status**: Pattern 1 (Prompt Chaining) implemented

## Repository Structure

```
patterns/
├── DotNetAgents.Patterns.sln          # Main solution file
├── 01-prompt-chaining/                # Pattern 1: Sequential LLM calls
│   └── src/PromptChaining/            # Console app with benchmarks
├── src/
│   ├── DotNetAgents.Infrastructure/   # Shared LLM provider abstraction
│   └── DotNetAgents.BenchmarkLlm/     # Benchmarking & evaluation framework
├── .env                               # Environment config (DO NOT COMMIT)
└── README.md                          # Project documentation
```

## Security

- `.env` files contain API keys - **NEVER commit to version control**
- The `.gitignore` excludes `.env` files
- Copy from `.env.example` when creating new environment files

## Build & Run Commands

```bash
# Build all projects
dotnet build DotNetAgents.Patterns.sln

# Run Pattern 1 (interactive mode)
dotnet run --project 01-prompt-chaining/src/PromptChaining

# Run with specific model
dotnet run --project 01-prompt-chaining/src/PromptChaining -- --model gpt-4o-mini

# Run benchmarks
dotnet run --project 01-prompt-chaining/src/PromptChaining -- --benchmark

# Evaluate existing benchmark run
dotnet run --project 01-prompt-chaining/src/PromptChaining -- --evaluate <run-path>

# List available benchmarks
dotnet run --project 01-prompt-chaining/src/PromptChaining -- --list-benchmarks

# Format code
dotnet tool restore && dotnet csharpier .
```

## LLM Provider Configuration

Provider is specified explicitly in code via `AgentConfig.Provider` or `ChatClientFactory.Create(provider, model)`.
Each provider uses its own environment variables (set via shell or `.env` file):

| Provider | Environment Variables |
|----------|----------------------|
| `azure` | `AZURE_OPENAI_API_KEY`, `AZURE_OPENAI_ENDPOINT` |
| `openai` | `OPENAI_API_KEY`, `OPENAI_ENDPOINT` (optional) |
| `ollama` | `OLLAMA_ENDPOINT` (optional, defaults to `http://localhost:11434`) |
| `openrouter` | `OPENROUTER_API_KEY`, `OPENROUTER_ENDPOINT` (optional) |
| `github` | `GITHUB_TOKEN`, `GITHUB_MODELS_ENDPOINT` (optional) |

Configuration is loaded from `.env` files (searches up the directory tree).

## Key Architecture Patterns

| Pattern | File | Purpose |
|---------|------|---------|
| ChatClientFactory | `src/DotNetAgents.Infrastructure/ChatClientFactory.cs` | Static factory for creating IChatClient with provider abstraction |
| ConfiguredAgent | `src/DotNetAgents.Infrastructure/ConfiguredAgent.cs` | Agent wrapper combining config with ChatClientAgent |
| WorkflowRunner | `src/DotNetAgents.Infrastructure/WorkflowRunner.cs` | Executes workflows with streaming, returns final agent output |
| MetricsCollectingChatClient | `src/DotNetAgents.BenchmarkLlm/Metrics/MetricsCollectingChatClient.cs` | Decorator for collecting timing/token metrics |
| Attribute Discovery | `src/DotNetAgents.BenchmarkLlm/Core/BenchmarkLlmDiscovery.cs` | Reflection-based benchmark discovery via `[WorkflowBenchmark]`, `[BenchmarkLlm]` |

## Coding Conventions

- **.NET 10.0** with `ImplicitUsings` and `Nullable` enabled
- **PascalCase** for classes and public members
- **camelCase** for local variables and parameters
- **Async suffix** on async methods (`RunAsync`, `EvaluateAsync`)
- **Record types** for immutable data (`BenchmarkOutput`, `QualityScore`)
- **Required init properties** for configuration: `public required string Name { get; init; }`
- **Top-level statements** in `Program.cs`
- **CSharpier** for code formatting

## Adding New Patterns

### Folder Structure

```
XX-pattern-name/
├── src/
│   └── PatternName/
│       ├── PatternName.csproj
│       ├── Program.cs
│       ├── appsettings.json
│       └── UseCases/
│           └── UseCaseName/
│               ├── XxxPipeline.cs
│               └── PatternNameBenchmarks.cs
└── README.md
```

### Project File Template

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\DotNetAgents.Infrastructure\DotNetAgents.Infrastructure.csproj" />
    <ProjectReference Include="..\..\src\DotNetAgents.BenchmarkLlm\DotNetAgents.BenchmarkLlm.csproj" />
  </ItemGroup>
</Project>
```

### Benchmark Definition Pattern

```csharp
[WorkflowBenchmark("pattern-name", Prompt = "Test prompt here", Description = "Pattern description")]
public class PatternNameBenchmarks
{
    [BenchmarkLlm("multi-agent", Description = "Multi-agent approach")]
    public async Task<BenchmarkOutput> MultiAgent(string prompt)
    {
        var (workflow, agentModels) = MyPipeline.Create(new MyPipelineConfig { Model = "gpt-4o" });
        var output = await WorkflowRunner.RunAsync(workflow, prompt);
        return new BenchmarkOutput { Content = output, AgentModels = agentModels };
    }

    [BenchmarkLlm("single-agent", Baseline = true, Description = "Single agent baseline")]
    public async Task<BenchmarkOutput> SingleAgent(string prompt)
    {
        // Baseline implementation for comparison
    }
}
```

### Adding to Solution

```bash
dotnet sln DotNetAgents.Patterns.sln add XX-pattern-name/src/PatternName/PatternName.csproj
```

## Key Files Reference

| File | Purpose |
|------|---------|
| `src/DotNetAgents.Infrastructure/ChatClientFactory.cs` | LLM client creation with multi-provider support |
| `src/DotNetAgents.Infrastructure/AgentConfig.cs` | Agent configuration model (Name, Instructions, Model) |
| `src/DotNetAgents.Infrastructure/WorkflowRunner.cs` | Workflow execution with streaming output |
| `src/DotNetAgents.BenchmarkLlm/BenchmarkLlmHost.cs` | Main benchmark orchestration entry point |
| `src/DotNetAgents.BenchmarkLlm/Core/BenchmarkLlmRunner.cs` | Benchmark execution engine |
| `src/DotNetAgents.BenchmarkLlm/Evaluation/LlmJudgeEvaluator.cs` | LLM-as-Judge quality scoring (8 dimensions) |
| `01-prompt-chaining/src/PromptChaining/Program.cs` | Pattern 1 entry point with CLI parsing |
| `01-prompt-chaining/src/PromptChaining/UseCases/ContentGeneration/MultiAgentContentPipeline.cs` | 3-agent workflow: Researcher → Outliner → Writer |

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.Agents.AI | 1.0.0-preview | Core agent framework |
| Microsoft.Agents.AI.Workflows | 1.0.0-preview | Workflow orchestration |
| Azure.AI.OpenAI | 2.* | Azure OpenAI provider |
| OllamaSharp | 5.* | Local Ollama model support |
| DotNetEnv | 3.* | .env file loading |
| Microsoft.Extensions.Configuration | 9.0.* | App configuration |

## Benchmark Output Structure

```
runs/
└── <timestamp>_<prompt-slug>/
    ├── run-config.json          # Input configuration
    ├── environment.json         # Provider, model, runtime info
    ├── <pattern-name>/
    │   └── <benchmark-name>/
    │       ├── output.md        # Generated content
    │       └── metrics.json     # Timing and token metrics
    ├── results.json             # Full structured results
    ├── comparison.md            # Markdown comparison table
    └── evaluation.json          # Quality scores (if Evaluate=true)
```

## Benchmark Configuration

Configure in `appsettings.json`:

```json
{
  "BenchmarkLlm": {
    "EvaluationModel": "gpt-4o",
    "Filter": "*",
    "ArtifactsPath": "C:/dev/dotnetagents/runs",
    "Evaluate": true,
    "Exporters": ["console", "markdown", "json"]
  }
}
```

## Quality Evaluation Dimensions

The LLM-as-Judge evaluates content on 8 dimensions (1-5 scale):
- **Completeness**: Coverage of topic
- **Structure**: Organization and flow
- **Accuracy**: Factual correctness
- **Engagement**: Writing quality
- **Evidence Quality**: Supporting facts/examples
- **Balance**: Perspective coverage
- **Actionability**: Practical value
- **Depth**: Topic exploration depth
