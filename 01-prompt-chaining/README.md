# Pattern 1: Prompt Chaining

Sequential LLM calls where each agent's output feeds into the next, creating a processing pipeline.

## Overview

Prompt Chaining is a foundational agentic pattern that breaks complex tasks into simpler, focused steps. Each step is handled by a specialized agent, and the output automatically flows to the next agent in the sequence.

```
[User Input] → [Researcher] → [Outliner] → [Writer] → [Final Content]
```

## Quick Start

```bash
cd src/PromptChaining

# Run the default workflow
dotnet run

# List available benchmarks
dotnet run -- --list-benchmarks

# Run benchmarks with evaluation
dotnet run -- --benchmark
```

## Prerequisites

- .NET 8.0 SDK or later
- An LLM provider configured in `appsettings.json` (see [main README](../../README.md#llm-provider-setup))

## Project Structure

```
01-prompt-chaining/
├── src/PromptChaining/
│   ├── Program.cs                    # Entry point with BenchmarkLlmHost
│   ├── appsettings.json              # LLM provider & benchmark config
│   └── UseCases/ContentGeneration/
│       ├── MultiAgentContentPipeline.cs    # 3-agent pipeline
│       ├── SingleAgentContentPipeline.cs   # Single-agent baseline
│       └── PromptChainingBenchmarks.cs     # Benchmark definitions
└── README.md
```

## Implementation

### Multi-Agent Pipeline

Creates three specialized agents chained together:

```csharp
var researcher = ChatClientFactory.CreateAgent(new AgentConfig
{
    Name = "Researcher",
    Model = "gpt-4.1",
    Instructions = """
        You are a research assistant. Given a topic:
        1. Identify 3-5 key concepts
        2. List important facts and statistics
        3. Note the target audience
        """
});

var outliner = ChatClientFactory.CreateAgent(new AgentConfig
{
    Name = "Outliner",
    Model = "gpt-4.1",
    Instructions = "Create a structured outline from the research..."
});

var writer = ChatClientFactory.CreateAgent(new AgentConfig
{
    Name = "Writer",
    Model = "gpt-4.1",
    Instructions = "Write polished content following the outline..."
});

var workflow = new WorkflowBuilder(researcher.ChatClientAgent)
    .AddEdge(researcher.ChatClientAgent, outliner.ChatClientAgent)
    .AddEdge(outliner.ChatClientAgent, writer.ChatClientAgent)
    .Build();
```

### Benchmark Class

```csharp
[WorkflowBenchmark(
    "prompt-chaining",
    Prompt = "The benefits of test-driven development in software engineering",
    Description = "Content generation with prompt chaining"
)]
public class PromptChainingBenchmarks
{
    [BenchmarkLlm("multi-agent", Description = "3-agent pipeline: Researcher -> Outliner -> Writer")]
    public async Task<BenchmarkOutput> MultiAgent(string prompt)
    {
        var (workflow, agentModels) = MultiAgentContentPipeline.Create(config);
        var content = await WorkflowRunner.RunAsync(workflow, prompt);
        return BenchmarkOutput.WithModels(content, agentModels);
    }

    [BenchmarkLlm("single-agent", Baseline = true, Description = "Single agent with combined instructions")]
    public async Task<BenchmarkOutput> SingleAgent(string prompt)
    {
        var (workflow, agentModels) = SingleAgentContentPipeline.Create("gpt-4.1");
        var content = await WorkflowRunner.RunAsync(workflow, prompt);
        return BenchmarkOutput.WithModels(content, agentModels);
    }
}
```

## Benchmark Results

### Cloud vs Local Comparison

| Benchmark | Model | Words | Tokens | Latency | Quality |
|-----------|-------|------:|-------:|--------:|:-------:|
| single-agent (baseline) | gpt-4.1 | 681 | 1,082 | 30s | 3.6/5 |
| multi-agent | gpt-4.1 | 1,433 | 4,506 | 27s | **4.4/5** |
| multi-agent-local | llama3.2 | 507 | 2,945 | 138s | 3.2/5 |

### Quality Breakdown

| Dimension | single-agent | multi-agent | multi-agent-local |
|-----------|:------------:|:-----------:|:-----------------:|
| Completeness | 4 | 4 | 4 |
| Structure | 5 | 5 | 4 |
| Accuracy | 5 | 5 | 4 |
| Engagement | 4 | 4 | 3 |
| Evidence | 3 | 4 | 3 |
| Balance | 2 | **5** | 2 |
| Actionability | 2 | 4 | 3 |
| Depth | 4 | 4 | 3 |

### Key Findings

**Multi-agent (gpt-4.1)** delivers the best quality:
- +110% more words with richer, more comprehensive content
- Balance score of 5 (vs 2) - covers pros AND cons of TDD
- Cites specific research (Microsoft, 40-60% defect reduction)

**Multi-agent-local (Ollama llama3.2)** enables local development:
- Runs entirely offline, no API costs
- Concise and practical with best practices
- Quality gap (-0.4 vs baseline) shows room for improvement

## When to Use This Pattern

**Use Prompt Chaining when:**
- Breaking complex tasks into focused steps
- Applying different expertise at each stage
- Quality matters more than speed/cost
- You need auditable intermediate outputs

**Avoid when:**
- Tasks require dynamic routing between steps
- Steps need to run in parallel
- Single-step processing is sufficient
- Cost/latency is the primary concern

## Learn More

- [Full Tutorial on DotNetAgents.net](https://dotnetagents.net/tutorials/01-prompt-chaining)
- [BenchmarkLlm Documentation](../../src/DotNetAgents.BenchmarkLlm/README.md)
- [Microsoft Agent Framework](https://learn.microsoft.com/en-us/agent-framework/)

---

*Inspired by [Agentic Design Patterns](https://www.amazon.com/Agentic-Design-Patterns-Hands-Intelligent/dp/3032014018) by Antonio Gulli. Implementation uses Microsoft Agent Framework.*
