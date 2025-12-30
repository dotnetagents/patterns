# Pattern 1: Prompt Chaining

Sequential LLM calls where each agent's output feeds into the next, creating a processing pipeline.

## Overview

Prompt Chaining is a foundational agentic pattern that breaks complex tasks into simpler, focused steps. Each step is handled by a specialized agent, and the output automatically flows to the next agent in the sequence.

```
[User Input] → [Researcher] → [Outliner] → [Writer] → [Final Content]
```

## Quick Start

### Prerequisites

- .NET 10.0 SDK or later
- One of the following LLM providers:
  - **Ollama** (free, local) - Recommended for learning
  - **OpenAI** API key
  - **Azure OpenAI** resource
  - **OpenRouter** API key

### Setup

Create a `.env` file in the project root or set environment variables:

#### Option 1: Ollama (Free, Local) - Default

```bash
# Install Ollama from https://ollama.com
ollama pull llama3.2

# Run (no env vars needed, uses defaults)
cd src/PromptChaining
dotnet run
```

#### Option 2: OpenAI

```bash
LLM_PROVIDER=openai
API_KEY=sk-...
MODEL=gpt-4o-mini
```

#### Option 3: Azure OpenAI

```bash
LLM_PROVIDER=azure
API_KEY=your-api-key
ENDPOINT=https://your-resource.openai.azure.com/
MODEL=gpt-4o-mini
```

#### Option 4: OpenRouter

```bash
LLM_PROVIDER=openrouter
API_KEY=sk-or-...
MODEL=openai/gpt-4o-mini
```

### Run

```bash
cd src/PromptChaining

# Interactive mode
dotnet run

# List available benchmarks
dotnet run -- --list-benchmarks

# Run benchmarks
dotnet run -- --benchmark

# Evaluate existing run results
dotnet run -- --evaluate ./runs/<run-id>
```

## Project Structure

```
01-prompt-chaining/
├── src/
│   └── PromptChaining/
│       ├── Program.cs                 # Entry point
│       ├── appsettings.json           # Benchmark configuration
│       ├── Agents/
│       │   ├── ResearcherAgent.cs     # Step 1: Gather key points
│       │   ├── OutlinerAgent.cs       # Step 2: Create structure
│       │   └── WriterAgent.cs         # Step 3: Write content
│       ├── Workflows/
│       │   ├── ContentPipeline.cs     # Multi-agent workflow
│       │   └── SingleAgentPipeline.cs # Single-agent baseline
│       └── Benchmarks/
│           └── PromptChainingBenchmarks.cs  # Benchmark definitions
└── README.md
```

## Key Concepts

### 1. Specialized Agents

Each agent has focused instructions for a specific task:

```csharp
public static ChatClientAgent Create(IChatClient chatClient) => new(chatClient, new()
{
    Name = "Researcher",
    Instructions = "You are a research assistant. Given a topic, identify 3-5 key points..."
});
```

### 2. Workflow Builder

Chain agents using the workflow builder:

```csharp
_workflow = new WorkflowBuilder(researcher)
    .AddEdge(researcher, outliner)
    .AddEdge(outliner, writer)
    .Build();
```

### 3. Streaming Execution

Get real-time updates as each agent processes:

```csharp
StreamingRun run = await InProcessExecution.StreamAsync(_workflow, messages);

await foreach (WorkflowEvent evt in run.WatchStreamAsync())
{
    if (evt is AgentRunUpdateEvent update)
    {
        Console.Write(update.Data);
    }
}
```

## Benchmarking

Compare multi-agent vs single-agent approaches using the built-in benchmark system.

### Configuration

Edit `appsettings.json`:

```json
{
  "BenchmarkLlm": {
    "Prompt": "The benefits of test-driven development",
    "Filter": "*",
    "ArtifactsPath": "./runs",
    "Evaluate": false,
    "Exporters": ["console", "markdown"]
  }
}
```

### Run Benchmarks

```bash
# List available benchmarks
dotnet run -- --list-benchmarks

# Run all benchmarks
dotnet run -- --benchmark

# Evaluate existing run results (runs LLM-as-Judge on saved outputs)
dotnet run -- --evaluate ./runs/2024-12-30_143022_benefits-of-tdd
```

### Available Benchmarks

| Benchmark | Description |
|-----------|-------------|
| `multi-agent` | 3-agent pipeline: Researcher → Outliner → Writer |
| `single-agent` | Single agent with combined instructions (baseline) |

Results are saved to `./runs/` with metrics, outputs, and comparison reports.

## When to Use This Pattern

**Use Prompt Chaining when:**
- Breaking complex tasks into focused steps
- Applying different expertise at each stage
- Creating reproducible, auditable pipelines
- Maintaining quality through specialization

**Avoid when:**
- Tasks require dynamic routing between steps
- Steps need to run in parallel
- Single-step processing is sufficient

## Related Patterns

- **Tool Use**: Agents calling external functions
- **Routing**: Conditional path selection
- **Parallelization**: Concurrent agent execution

## Learn More

- [BenchmarkLlm Documentation](../../src/DotNetAgents.BenchmarkLlm/README.md)
- [Microsoft Agent Framework Docs](https://learn.microsoft.com/en-us/agent-framework/)
