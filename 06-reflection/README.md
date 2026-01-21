# Pattern 6: Reflection

Iterative generate-critique-refine cycles where a critic agent evaluates and provides feedback to improve output quality.

## Overview

The Reflection Pattern automates the feedback loop that humans naturally provide when reviewing and improving content. An LLM generates initial output, a critic evaluates it against quality criteria, and the generator refines based on feedback. This cycle repeats until the output meets quality standards.

```
[User Input] → [Generator] → [Critic] → [Refinement Decision]
                    ↑                            |
                    └────────────────────────────┘
                         (iterate until approved)
```

## Quick Start

```bash
cd src/Reflection

# Run the default workflow (two-agent reflection)
dotnet run -- --provider azure --model gpt-4.1

# List available benchmarks
dotnet run -- --list-benchmarks

# Run benchmarks with evaluation
dotnet run -- --benchmark
```

## Prerequisites

- .NET 10.0 SDK or later
- An LLM provider configured in `appsettings.json` (see [main README](../../README.md#llm-provider-setup))

## Project Structure

```
06-reflection/
├── src/Reflection/
│   ├── Program.cs                    # Entry point with BenchmarkLlmHost
│   ├── appsettings.json              # LLM provider & benchmark config
│   └── UseCases/StartupPitch/
│       ├── ReflectionLoopExecutor.cs # Custom executor managing the loop
│       ├── ReflectionPipeline.cs     # Two-agent workflow (PitchWriter + VCCritic)
│       ├── SelfReflectionPipeline.cs # Single-agent self-critique
│       ├── SingleShotPipeline.cs     # Baseline (no reflection)
│       └── ReflectionBenchmarks.cs   # Benchmark definitions
└── README.md
```

## Use Case: Startup Pitch Perfector

This pattern is demonstrated through a startup pitch generation use case where:
- **PitchWriter Agent**: Creates and refines startup pitches
- **VCCritic Agent**: Evaluates pitches as a skeptical VC investor

### Why This Use Case?

1. **Clear evaluation criteria** - VC investor perspective is well-defined
2. **Natural dialogue** - Back-and-forth critique feels authentic
3. **Measurable improvement** - Easy to compare initial vs. refined pitch
4. **Practical value** - Demonstrates real-world application

## Implementation

### ReflectionLoopExecutor

Custom executor that encapsulates the generate-critique-refine loop:

```csharp
internal sealed class ReflectionLoopExecutor : Executor<ChatMessage, ChatMessage>
{
    private readonly IChatClient _writerClient;
    private readonly IChatClient _criticClient;
    private readonly int _maxIterations;

    public override async ValueTask<ChatMessage> HandleAsync(
        ChatMessage input, IWorkflowContext context, CancellationToken ct)
    {
        // Generate initial pitch
        var pitch = await GenerateInitialPitch(input);

        for (int i = 0; i < _maxIterations; i++)
        {
            // Get critique from VC
            var critique = await GetCritique(pitch);

            // Check for approval
            if (critique.Contains("APPROVED"))
                return pitch;

            // Refine based on feedback
            pitch = await RefinePitch(pitch, critique);
        }

        return pitch;
    }
}
```

### Benchmark Class

```csharp
[WorkflowBenchmark(
    "reflection",
    Prompt = "A mobile app that uses AI to match busy professionals...",
    Description = "Startup pitch generation with reflection pattern"
)]
public class ReflectionBenchmarks
{
    [BenchmarkLlm("single-shot", Baseline = true,
        Description = "Direct pitch generation without reflection")]
    public async Task<BenchmarkOutput> SingleShot(string prompt) { ... }

    [BenchmarkLlm("self-reflection",
        Description = "Single agent with internal self-critique loop")]
    public async Task<BenchmarkOutput> SelfReflection(string prompt) { ... }

    [BenchmarkLlm("two-agent",
        Description = "PitchWriter + VCCritic iterative refinement")]
    public async Task<BenchmarkOutput> TwoAgent(string prompt) { ... }
}
```

## Reflection Approaches

### 1. Single-Shot (Baseline)

Direct generation without any reflection. Single agent produces the pitch in one attempt.

### 2. Self-Reflection

Single agent with internal self-critique. The agent is prompted to:
1. Generate initial pitch
2. Critique its own work as a skeptical VC
3. Revise based on self-critique

Fewer API calls but potentially less rigorous critique.

### 3. Two-Agent Reflection

Separate PitchWriter and VCCritic agents:
- **PitchWriter**: Creates compelling pitches covering Problem, Solution, Market Size, Business Model, Traction, Competition, Team, and The Ask
- **VCCritic**: Evaluates on clarity, market fit, differentiation, business model viability, and traction

Iterates until "APPROVED" verdict or maximum iterations reached.

## Agent Instructions

<details>
<summary><strong>PitchWriter Agent</strong></summary>

```csharp
var pitchWriterPrompt = """
    You are an expert startup pitch writer. Given a startup idea, create a
    compelling pitch that covers: Problem, Solution, Market Size, Business Model,
    Traction, Competition, Team, and The Ask.

    When given feedback from investors, thoughtfully address their concerns
    while maintaining a compelling narrative. Focus on making the pitch stronger
    with each revision.

    Structure your pitch with clear sections and make it persuasive yet realistic.
    """;
```

</details>

<details>
<summary><strong>VCCritic Agent</strong></summary>

```csharp
var vcCriticPrompt = """
    You are a skeptical VC partner evaluating startup pitches. Your job is to
    find weaknesses and ask tough questions. Evaluate the pitch on:
    - Problem clarity and market pain
    - Solution-market fit
    - TAM/SAM/SOM credibility
    - Competitive differentiation
    - Business model viability
    - Traction and validation
    - Team execution ability

    Provide specific, actionable feedback. Be constructive but rigorous.

    IMPORTANT: At the end of your critique, you MUST include one of these verdicts:
    - "APPROVED" - if the pitch is strong enough for an investor meeting
    - "NEEDS_REVISION" - if the pitch needs more work

    Only approve if the pitch genuinely addresses the core investor concerns.
    """;
```

</details>

## Expected Results

Based on reflection pattern research, expect:

| Benchmark | Iterations | Quality Improvement |
|-----------|:----------:|:-------------------:|
| single-shot (baseline) | 1 | - |
| self-reflection | 1 (internal) | ~10-15% |
| two-agent | 2-3 | ~15-25% |

**Trade-offs:**
- Two-agent uses more tokens (2-3x baseline per iteration)
- Each iteration adds latency
- Diminishing returns after 2-3 iterations

## When to Use This Pattern

**Use Reflection when:**
- Output quality is critical
- Clear evaluation criteria exist
- Iterative improvement is natural for the task
- You have budget for multiple LLM calls

**Avoid when:**
- Speed is the primary concern
- Tasks don't benefit from iteration
- Evaluation criteria are too subjective
- Single-pass generation is sufficient

## Best Practices

1. **Limit iterations** - 2-3 rounds typically sufficient (diminishing returns)
2. **Clear evaluation criteria** - Structured rubrics, not vague feedback
3. **Termination conditions** - Always set max iterations to prevent infinite loops
4. **Consider model tiers** - Cheaper model for generation, stronger for critique

## Learn More

- [Agentic Design Patterns Part 2: Reflection (DeepLearning.AI)](https://www.deeplearning.ai/the-batch/agentic-design-patterns-part-2-reflection/)
- [AWS Evaluator Reflect-Refine Loop Patterns](https://docs.aws.amazon.com/prescriptive-guidance/latest/agentic-ai-patterns/evaluator-reflect-refine-loop-patterns.html)
- [Microsoft Agent Framework](https://learn.microsoft.com/en-us/agent-framework/)

---

*Inspired by [Agentic Design Patterns](https://www.amazon.com/Agentic-Design-Patterns-Hands-Intelligent/dp/3032014018) by Antonio Gulli. Implementation uses Microsoft Agent Framework.*
