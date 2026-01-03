# Pattern 3: Routing

Conditional routing of requests to specialized agents based on LLM classification.

## Overview

The Routing pattern uses an LLM to analyze incoming requests, classify them into categories, and delegate to specialized agents with domain-specific knowledge. This enables better response quality through specialization.

```
                         Routing Workflow
┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│  [Support Ticket] ──► [Classifier] ──► Classification          │
│                            │                                    │
│              ┌─────────────┼─────────────┬─────────────┐       │
│              ▼             ▼             ▼             ▼       │
│         [Billing]    [Technical]    [Account]     [Product]    │
│         Specialist   Specialist    Specialist    Specialist    │
│              │             │             │             │       │
│              └─────────────┴─────────────┴─────────────┘       │
│                            │                                    │
│                       [Response]                                │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

## Quick Start

```bash
cd src/Routing

# Run the default workflow (interactive mode)
dotnet run -- --provider azure --model gpt-4.1

# List available benchmarks
dotnet run -- --list-benchmarks

# Run benchmarks with evaluation
dotnet run -- --benchmark
```

## Prerequisites

- .NET 10.0 SDK or later
- An LLM provider configured in `.env` (see [main README](../../README.md#llm-provider-setup))

## Project Structure

```
03-routing/
├── src/Routing/
│   ├── Program.cs                    # Entry point with CLI & interactive mode
│   ├── appsettings.json              # LLM provider & benchmark config
│   └── UseCases/CustomerSupport/
│       ├── SupportCategory.cs        # Enum + RoutingDecision record
│       ├── RoutingConfig.cs          # Pipeline configuration classes
│       ├── ClassifierExecutor.cs     # LLM-based ticket classification
│       ├── SpecialistExecutor.cs     # Domain-specific response handler
│       ├── SpecialistInstructions.cs # System prompts for each specialist
│       ├── RoutingWorkflow.cs        # Workflow with conditional edges
│       ├── SingleAgentSupportPipeline.cs  # Baseline for comparison
│       ├── SampleTickets.cs          # Test data
│       └── RoutingBenchmarks.cs      # Benchmark definitions
└── README.md
```

## Implementation

### Classifier Executor

Uses LLM to classify tickets with XML output for reliable parsing:

```csharp
public sealed class ClassifierExecutor : Executor<ChatMessage, RoutingDecision>
{
    public override async ValueTask<RoutingDecision> HandleAsync(
        ChatMessage message,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        // Call LLM with classification prompt
        // Parse XML: <reasoning> and <selection> tags
        // Return RoutingDecision with category
    }
}
```

### Routing Workflow with AddSwitch

```csharp
var builder = new WorkflowBuilder(classifier);

// Use AddSwitch for conditional routing based on classification
builder.AddSwitch(classifier, switchBuilder =>
    switchBuilder
        .AddCase<RoutingDecision>(r => r.Category == SupportCategory.Billing, billing)
        .AddCase<RoutingDecision>(r => r.Category == SupportCategory.Technical, technical)
        .AddCase<RoutingDecision>(r => r.Category == SupportCategory.Account, account)
        .AddCase<RoutingDecision>(r => r.Category == SupportCategory.Product, product)
        .AddCase<RoutingDecision>(_ => true, general) // Default case
);

var workflow = builder.Build();
```

### Benchmark Class

```csharp
[WorkflowBenchmark("routing", Prompt = SampleTickets.DefaultBenchmarkTicket)]
public class RoutingBenchmarks
{
    [BenchmarkLlm("single-agent", Baseline = true)]
    public async Task<BenchmarkOutput> SingleAgent(string prompt) { ... }

    [BenchmarkLlm("routing")]
    public async Task<BenchmarkOutput> Routing(string prompt) { ... }

    [BenchmarkLlm("routing-mini-classifier")]
    public async Task<BenchmarkOutput> RoutingMiniClassifier(string prompt) { ... }
}
```

## Categories

| Category | Handles |
|----------|---------|
| **Billing** | Payment issues, refunds, subscriptions, pricing |
| **Technical** | API errors, bugs, integration issues, performance |
| **Account** | Password resets, 2FA, security, access issues |
| **Product** | Features, capabilities, usage guidance, onboarding |
| **General** | Fallback for unclassified requests |

## Sample Tickets

The `SampleTickets` class provides test tickets for each category:

- **Billing**: Duplicate charge complaint
- **Technical**: 403 API error
- **Account**: Password reset issue
- **Product**: SSO integration question
- **Ambiguous**: Multi-category issue (billing + account)

## When to Use This Pattern

**Use Routing when:**
- Requests fall into distinct categories with different handling needs
- Specialized knowledge improves response quality
- You need clear audit trails of classification decisions
- Different categories require different response formats or expertise

**Avoid when:**
- Categories overlap significantly
- A single generalist prompt suffices
- Classification adds unacceptable latency
- Request types are unpredictable

## Trade-offs

| Approach | Latency | Quality | Cost |
|----------|---------|---------|------|
| Single Agent | Lower (1 LLM call) | Generic responses | Lower |
| Routing | Higher (2+ LLM calls) | Specialized responses | Higher |
| Routing (mini classifier) | Medium | Specialized responses | Medium |

## Learn More

- [Full Tutorial on DotNetAgents.net](https://dotnetagents.net/tutorials/03-routing)
- [Microsoft Agent Framework Edges](https://learn.microsoft.com/en-us/agent-framework/user-guide/workflows/core-concepts/edges)
- [BenchmarkLlm Documentation](../../src/DotNetAgents.BenchmarkLlm/README.md)

---

*Inspired by [Agentic Design Patterns](https://www.amazon.com/Agentic-Design-Patterns-Hands-Intelligent/dp/3032014018) by Antonio Gulli. Implementation uses Microsoft Agent Framework.*
