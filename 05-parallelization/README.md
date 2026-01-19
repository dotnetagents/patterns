# Pattern 5: Parallelization

Run multiple specialized agents concurrently using the Microsoft Agent Framework's native **Fan-Out/Fan-In** workflow pattern.

## Overview

The Parallelization pattern uses `WorkflowBuilder.AddFanOutEdge()` and `AddFanInEdge()` to execute independent agent tasks simultaneously, then combines results with an aggregator. This is ideal when:

- Multiple independent research or processing tasks are needed
- Latency is a concern and tasks don't depend on each other
- You want specialized agents for different domains

## Architecture

```
                    [User Query: "Plan a vacation to Barcelona..."]
                                        │
                                        ▼
                            [FanOutExecutor]
                            (broadcasts query + TurnToken)
                                        │
                    ┌───────────────────┼───────────────────┐
                    │   AddFanOutEdge   │                   │
                    ▼                   ▼                   ▼
            [Hotels Agent]      [Transport Agent]   [Activities Agent]
            + Google Search     + Google Search     + Google Search
                    │                   │                   │
                    │     PARALLEL      │                   │
                    │   (Framework)     │                   │
                    │                   │                   │
                    └───────────────────┼───────────────────┘
                                        │ AddFanInEdge
                                        ▼
                            [AggregationExecutor]
                            (collects List<ChatMessage>)
                                        │
                                        ▼
                        [Comprehensive Travel Plan]
```

## Quick Start

```bash
# Navigate to pattern directory
cd 05-parallelization/src/Parallelization

# Run interactive mode (uses mock search if no API key)
dotnet run -- --provider azure --model gpt-4.1

# Run benchmarks
dotnet run -- --benchmark

# List available benchmarks
dotnet run -- --list-benchmarks

# Evaluate a previous run
dotnet run -- --evaluate ./runs/<run-id>
```

## Prerequisites

- .NET 10.0 SDK
- An LLM provider configured (Azure OpenAI, OpenAI, etc.)
- (Optional) Google Custom Search API credentials for real web searches

## Environment Variables

### Required for LLM
```bash
# Azure OpenAI
AZURE_OPENAI_API_KEY=your-api-key
AZURE_OPENAI_ENDPOINT=https://your-resource.openai.azure.com/

# Or OpenAI
OPENAI_API_KEY=your-api-key
```

### Optional for Real Web Search
```bash
GOOGLE_SEARCH_API_KEY=your-google-api-key
GOOGLE_SEARCH_ENGINE_ID=your-search-engine-id
```

Get Google API credentials:
1. Create a project at [Google Cloud Console](https://console.cloud.google.com/)
2. Enable the Custom Search API
3. Create an API key
4. Create a Programmable Search Engine at [programmablesearchengine.google.com](https://programmablesearchengine.google.com/)

If Google API credentials are not set, the pattern uses a mock search service that returns realistic static results.

## Project Structure

```
05-parallelization/
├── README.md
└── src/
    └── Parallelization/
        ├── Parallelization.csproj
        ├── Program.cs
        ├── appsettings.json
        ├── Services/
        │   ├── IGoogleSearchService.cs      # Search service interface
        │   ├── GoogleSearchService.cs       # Real Google API implementation
        │   └── MockGoogleSearchService.cs   # Mock for testing/benchmarks
        ├── Tools/
        │   └── GoogleSearchTool.cs          # LLM-callable search tool
        └── UseCases/
            └── TravelPlanning/
                ├── TravelPlanningConfig.cs       # Configuration classes
                ├── FanOutExecutor.cs             # Broadcasts query to parallel agents
                ├── HotelsAgentExecutor.cs        # Hotels research agent
                ├── TransportAgentExecutor.cs     # Transport research agent
                ├── ActivitiesAgentExecutor.cs    # Activities research agent
                ├── AggregationExecutor.cs        # Collects and synthesizes results
                ├── ParallelTravelPipeline.cs     # Fan-Out/Fan-In workflow
                ├── SequentialTravelPipeline.cs   # Sequential baseline (AddEdge)
                ├── SingleAgentTravelPipeline.cs  # Single-agent baseline
                ├── SampleQueries.cs              # Test queries
                └── ParallelizationBenchmarks.cs  # Benchmark definitions
```

## Key Components

### Specialized Agents

Each agent is a domain expert with access to Google Search:

| Agent | Responsibility |
|-------|---------------|
| **Hotels Agent** | Researches accommodations, prices, amenities, locations |
| **Transport Agent** | Finds flights, trains, airport transfers, local transport |
| **Activities Agent** | Discovers attractions, restaurants, tours, day plans |
| **Aggregator** | Combines all research into a cohesive travel plan |

### Fan-Out/Fan-In Pattern

The framework handles parallel execution natively:

```csharp
// Build workflow with native parallel execution
var workflow = new WorkflowBuilder(fanOutExecutor)
    .AddFanOutEdge(fanOutExecutor, [hotelsAgent, transportAgent, activitiesAgent])
    .AddFanInEdge([hotelsAgent, transportAgent, activitiesAgent], aggregationExecutor)
    .WithOutputFrom(aggregationExecutor)
    .Build();

// Run using WorkflowRunner
var result = await WorkflowRunner.RunAsync(workflow, query);
```

### FanOutExecutor

Broadcasts the query to all parallel agents:

```csharp
internal sealed class FanOutExecutor() : Executor<string>("FanOutExecutor")
{
    public override async ValueTask HandleAsync(
        string message,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        // Broadcast query to all connected agents
        await context.SendMessageAsync(new ChatMessage(ChatRole.User, message), cancellationToken);

        // Send turn token to kick off parallel execution
        await context.SendMessageAsync(new TurnToken(emitEvents: true), cancellationToken);
    }
}
```

### AggregationExecutor

Collects results from all agents and synthesizes:

```csharp
internal sealed class AggregationExecutor : Executor<List<ChatMessage>>
{
    public override async ValueTask HandleAsync(
        List<ChatMessage> messages,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        // Collect results until all 3 agents have responded
        _collectedResults.AddRange(messages);

        if (_collectedResults.Count >= 3)
        {
            // Synthesize into final travel plan
            var response = await _chatClient.GetResponseAsync(...);
            await context.YieldOutputAsync(response.Text, cancellationToken);
        }
    }
}
```

## Benchmarks

Four benchmark variants are available:

| Benchmark | Description |
|-----------|-------------|
| `single-agent` | Baseline: One agent handles all research |
| `sequential` | 3 agents with `AddEdge` chain (one after another) |
| `parallel` | 3 agents with `AddFanOutEdge/AddFanInEdge` (simultaneous) |
| `parallel-mini` | Mini models for research, gpt-4.1 for aggregation |

### Expected Results

| Approach | Agents | Latency | Quality | Cost |
|----------|--------|---------|---------|------|
| Single Agent | 1 | Medium | Good | Low |
| Sequential | 4 | High | Better | Medium |
| **Parallel** | 4 | **~3x Lower** | Better | Medium |
| Parallel-Mini | 4 | Lowest | Good | Low |

Key insight: Parallel execution achieves similar quality to sequential but with significantly lower latency.

## When to Use This Pattern

### Good For
- Independent subtasks that don't depend on each other
- Time-sensitive applications where latency matters
- Research tasks requiring multiple specialized perspectives
- Aggregating information from multiple sources

### Trade-offs
- Higher total token usage (4 agents vs 1)
- More complex orchestration
- All agents must complete before aggregation
- Requires careful prompt design for each specialist

## Example Query

```
> Plan a 7-day vacation to Barcelona, Spain in June 2025 for a family of 4
  with kids aged 8 and 12. We're interested in beaches, cultural attractions,
  and good food. Our budget is moderate.
```

The parallel agents will simultaneously research:
1. **Hotels**: Family-friendly hotels near beaches and attractions
2. **Transport**: Flights, airport transfers, metro passes
3. **Activities**: Sagrada Familia, beaches, tapas tours, kid-friendly activities

The aggregator then synthesizes everything into a day-by-day itinerary with practical logistics.

## Learn More

- [Microsoft Learn - Simple Concurrent Workflow Tutorial](https://learn.microsoft.com/en-us/agent-framework/tutorials/workflows/simple-concurrent-workflow)
- [WorkflowBuilder Class API Reference](https://learn.microsoft.com/en-us/dotnet/api/microsoft.agents.ai.workflows.workflowbuilder)
- [DotNetAgents.net](https://dotnetagents.net) - Full tutorials
- [Google Custom Search API](https://developers.google.com/custom-search/v1/introduction) - API documentation
