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

### Benchmark Setup

**Prompt:** "The benefits of test-driven development in software engineering"

| Configuration | Provider | Model | Agents |
|--------------|----------|-------|--------|
| single-agent | Azure OpenAI | gpt-4.1 | 1 (CombinedContentAgent) |
| multi-agent | Azure OpenAI | gpt-4.1 | 3 (Researcher → Outliner → Writer) |

**Environment:**
- Runtime: .NET 10.0.1
- Evaluation Model: gpt-4o (LLM-as-Judge)
- Date: December 30, 2025

### Results Summary

| Benchmark | LLM Calls | Tokens | Latency | Quality Score |
|-----------|:---------:|-------:|--------:|:-------------:|
| **single-agent** (baseline) | 1 | 1,029 | 8.7s | 3.63/5 |
| **multi-agent** | 3 | 3,774 | 24.8s | **4.75/5** |

**Token Breakdown:**

| Benchmark | Input Tokens | Output Tokens | Total |
|-----------|-------------:|--------------:|------:|
| single-agent | 238 | 791 | 1,029 |
| multi-agent | 1,450 | 2,324 | 3,774 |

### Quality Breakdown (1-5 scale)

| Dimension | single-agent | multi-agent | Delta |
|-----------|:------------:|:-----------:|:-----:|
| Completeness | 4 | **5** | +1 |
| Structure | 5 | 5 | 0 |
| Accuracy | 5 | 5 | 0 |
| Engagement | 4 | **5** | +1 |
| Evidence | 2 | **5** | +3 |
| Balance | 3 | **5** | +2 |
| Actionability | 2 | 3 | +1 |
| Depth | 4 | **5** | +1 |
| **Average** | **3.63** | **4.75** | **+1.12** |

### Key Findings

**Multi-agent pipeline delivers +31% higher quality scores:**
- **Evidence Quality** jumps from 2→5: Cites specific research (IBM 40% bug reduction, Microsoft 60-90% fewer defects)
- **Balance** improves from 3→5: Covers both benefits AND challenges/limitations of TDD
- **Completeness** and **Engagement** reach perfect 5s with richer, more comprehensive content

**Trade-offs:**
- Multi-agent uses 3.7× more tokens (3,774 vs 1,029)
- Multi-agent takes 2.8× longer (24.8s vs 8.7s)
- ROI: +31% quality for 3.7× token cost

### Agent Instructions

<details>
<summary><strong>Single Agent Setup</strong></summary>

```csharp
var writer = new AgentExecutor(new AgentConfig
{
    Name = "CombinedContentAgent",
    Provider = "azure",
    Model = "gpt-4.1",
    Instructions = """
        You are a professional content writer who creates engaging, well-structured articles.

        When given a topic, follow these steps internally:

        ## Step 1: Research
        - Identify the main concepts and subtopics
        - List 3-5 key points that should be covered
        - Note any important facts, statistics, or examples
        - Identify the target audience and appropriate tone

        ## Step 2: Outline
        - Create a hierarchical outline with main sections and subsections
        - Include brief descriptions for each section (1-2 sentences)
        - Suggest an introduction hook and conclusion
        - Recommend word count per section
        - Focus on logical flow and reader engagement

        ## Step 3: Write
        - Write polished content following the outline structure
        - Use clear, engaging language appropriate for the target audience
        - Include transitions between sections
        - Add a compelling introduction and satisfying conclusion

        IMPORTANT: Output ONLY the final article.
        """
});
```

</details>

<details>
<summary><strong>Multi-Agent Setup (3 agents)</strong></summary>

**Agent 1: Researcher**
```csharp
new AgentConfig
{
    Name = "Researcher",
    Provider = "azure",
    Model = "gpt-4.1",
    Instructions = """
        You are a research assistant specializing in gathering and synthesizing information.

        When given a topic:
        1. Identify the main concepts and subtopics
        2. List 3-5 key points that should be covered
        3. Note any important facts, statistics, or examples
        4. Identify the target audience and appropriate tone

        Output format: Structured research notes with clear sections.
        """
}
```

**Agent 2: Outliner**
```csharp
new AgentConfig
{
    Name = "Outliner",
    Provider = "azure",
    Model = "gpt-4.1",
    Instructions = """
        You are a content strategist who creates clear, logical outlines.

        Based on the research notes provided:
        1. Create a hierarchical outline with main sections and subsections
        2. Include brief descriptions for each section (1-2 sentences)
        3. Suggest an introduction hook and conclusion
        4. Recommend word count per section

        Output format: Numbered outline with clear hierarchy.
        """
}
```

**Agent 3: Writer**
```csharp
new AgentConfig
{
    Name = "Writer",
    Provider = "azure",
    Model = "gpt-4.1",
    Instructions = """
        You are a professional content writer who creates engaging, well-structured articles.

        Based on the outline provided:
        1. Write polished content following the outline structure
        2. Use clear, engaging language appropriate for the target audience
        3. Include transitions between sections
        4. Add a compelling introduction and satisfying conclusion

        IMPORTANT: Output ONLY the final article.
        """
}
```

**Workflow:**
```csharp
new WorkflowBuilder(researcher)
    .AddEdge(researcher, outliner)
    .AddEdge(outliner, writer)
    .Build();
```

</details>

### Generated Outputs

<details>
<summary><strong>Single Agent Output</strong> (791 tokens, 3.63/5 quality)</summary>

# The Benefits of Test-Driven Development in Software Engineering

In the fast-paced world of software engineering, where product quality and development speed are paramount, methodologies that support robust, maintainable code are essential. Test-driven development (TDD) is one such approach, widely embraced by agile teams and forward-thinking developers. By prioritizing tests before the actual code, TDD streamlines the development process and cultivates a disciplined engineering culture. But what are the real benefits of test-driven development, and why should software teams consider making it a core part of their workflow?

## Understanding Test-Driven Development

Test-driven development flips traditional programming on its head. Rather than writing code first and testing afterward, TDD requires developers to begin by writing well-defined tests for a feature or module. Only after these tests are established does the coding process begin. The cycle is clear: write a failing test, write the minimum code necessary to make the test pass, and then refactor the code while ensuring that all tests continue to succeed.

## Enhanced Code Quality and Reliability

One of TDD's most profound benefits is the notable improvement in code quality. Because tests guide the development process, developers are more likely to write clean, modular, and loosely-coupled code. Each functional unit is accompanied by a corresponding test, ensuring that every piece operates as expected.

Furthermore, TDD reduces the likelihood of bugs surfacing late in the development cycle. As new features or changes are introduced, previously written tests provide a safety net, immediately catching regressions and preventing unintentional disruptions. This test-first mindset builds reliability into every layer of the codebase.

## Faster and Safer Refactoring

Software inevitably evolves. Whether adapting to new requirements or optimizing existing features, code often needs to be refactored. TDD empowers developers to refactor with confidence. Since a comprehensive test suite is already in place, engineers can make changes knowing that any breakage will be quickly spotted.

This streamlines the refactoring process, eliminates the fear of hidden side effects, and encourages a culture where continuous improvement is not only possible but also practical.

## Lower Long-term Maintenance Costs

While TDD may seem time-intensive upfront, it usually results in lower maintenance costs over the long haul. Well-tested code is easier to understand, debug, and extend. New developers onboarding into the project can use existing tests to grasp the system's behavior, while legacy issues are minimized thanks to predictable, documented functionality.

Additionally, TDD helps in documenting the intended use of each part of the system through test cases, reducing the reliance on extensive external documentation and making it easier to identify if something is broken or out of sync.

## Improved Design and Requirements Clarity

The discipline of writing tests before implementation forces developers to clarify requirements and edge cases from the outset. TDD compels teams to break complex features into smaller, testable units, resulting in better-architected solutions. It becomes easier to identify ambiguities and potential issues early, reducing miscommunication between developers, testers, and stakeholders.

Moreover, TDD can bridge gaps between technical and non-technical team members, as tests serve as executable specifications. This alignment ensures that what gets built matches what was intended.

## Boosted Developer Confidence and Productivity

TDD doesn't just benefit the software—it benefits the people creating it. Developers report increased satisfaction and confidence when working with well-tested code. Frequent feedback loops keep progress visible and issues manageable. As a result, teams often deliver software more quickly, with higher morale and reduced stress.

## Conclusion

Test-driven development is more than just a testing technique; it's a philosophy that places quality, clarity, and confidence at the heart of software engineering. By writing tests first, developers and teams enjoy heightened code reliability, easier maintenance, and a more agile approach to change. For anyone seeking to build dependable, adaptable software in today's demanding environment, TDD remains a proven and invaluable ally.

</details>

<details>
<summary><strong>Multi-Agent Output</strong> (2,324 tokens, 4.75/5 quality)</summary>

# The Benefits of Test-Driven Development in Software Engineering

Imagine a world where most software bugs are caught before they ever reach your customers. For development teams, this promise is not just wishful thinking—it's increasingly a reality, thanks to Test-Driven Development (TDD). As the software landscape grows ever more complex and the demand for reliability soars, TDD has emerged as a keystone practice among leading organizations. But what makes this methodology so transformative, and why are modern software teams embracing it in droves?

## Introduction to Test-Driven Development

Test-Driven Development, commonly known as TDD, is a software engineering approach that flips the traditional development process on its head. Instead of writing code first and tests later (if at all), TDD mandates that developers begin by writing automated tests for each small unit of functionality. Only after the test is created—and fails, because the feature doesn't exist yet—does the actual code get written and refined until the test passes. This "test-first" cycle repeats for every new requirement, embedding quality checks directly into the DNA of the software. As the digital industry faces rising pressure to deliver robust, maintainable systems rapidly, TDD's appeal and adoption have grown significantly across startups and enterprises alike.

## Key Benefits of TDD

Test-Driven Development isn't just a shift in workflow—it's a mindset that brings a wealth of concrete benefits to software teams. From higher code quality to a smoother development lifecycle, TDD delivers value at every stage.

### Enhanced Code Quality & Reliability

Frequent, automated testing is the hallmark of TDD. Because each code segment is rigorously tested before it's ever committed, defects and edge cases are caught early—long before software reaches production. Numerous studies affirm the impact: for example, IBM reported a 40% reduction in bug rates after rolling out TDD practices, while Microsoft teams experienced up to a 60% drop in post-release defects. By building quality into the process, TDD yields software that ships with greater stability and user trust.

### Streamlined Debugging & Maintenance

With TDD, every new feature comes with its own suite of regression tests. These automated checks act as sentinels, immediately highlighting when a recent change might have inadvertently broken existing functionality. For developers, this means fewer late-stage surprises and significantly reduced time spent tracking down bugs. Teams can quickly pinpoint and resolve issues, leading to more predictable release cycles and dramatically lower maintenance overhead in the long run.

### Superior Software Design

Another often-overlooked advantage of TDD is improved design. When developers write tests before code, they're forced to clarify specifications and think deeply about how components should interact. This upfront rigor encourages modular, loosely coupled code structures that are easier to extend and adapt as requirements evolve. TDD naturally deters "big ball of mud" architectures and fosters cleaner interfaces, ensuring the application's foundations are sound right from the start.

### Living Documentation

In the world of agile, fast-moving teams, documentation often falls behind, leaving new developers scrambling to understand complex codebases. TDD's automated tests double as executable documentation—actual, up-to-date scenarios that describe how code should behave. For onboarding, knowledge transfer, and cross-team communication, this "living documentation" eases the learning curve and helps safeguard institutional knowledge as projects scale or staff changes.

### Facilitated Refactoring

Software must continuously evolve, and TDD makes that evolution safe and stress-free. When developers need to refactor—whether for optimization, code readability, or new requirements—having a comprehensive test suite ensures regressions are caught instantly. Refactoring becomes less of a risky endeavor and more an opportunity for improvement, empowering teams to iterate confidently and maintain code health over the project's life.

## Challenges and Limitations of TDD

Like any practice, TDD is not without its hurdles. One common concern is the perceived increase in initial development time. Writing tests before any implementation can slow progress at the outset, especially for teams new to the methodology. Additionally, there's a learning curve; effective TDD requires developers to not only master testing frameworks but also recalibrate how they approach problem-solving.

In some domains, TDD's benefits may be limited. For example, projects heavily reliant on rapid prototyping, UI experimentation, or integrations with legacy systems can encounter friction when writing tests upfront isn't practical. Poorly written or overly rigid tests can further stunt progress, resulting in "test paralysis" where making changes becomes daunting.

Despite these challenges, many teams discover that the upfront investment pays exponential dividends as projects mature. By embracing TDD's discipline, common pitfalls fade, especially when combined with coaching, pair programming, and incremental adoption strategies.

## Real-World Results and Industry Examples

The real measure of TDD's value lies in the outcome, and leading technology companies provide compelling evidence. A widely cited Microsoft case study revealed that teams practicing TDD recorded 60–90% fewer defects. Similarly, a group at IBM observed significant reductions in critical post-release bugs and noted improved responsiveness to changing business needs.

Industry surveys, such as those published by the IEEE and Agile Alliance, consistently find that TDD correlates with higher team morale, better predictability of software releases, and measurable long-term cost savings in maintenance. From small startups to tech giants, TDD has become a catalyst for quality and innovation, driving both customer satisfaction and operational efficiency.

## Conclusion

Test-Driven Development isn't just a passing trend—it's a proven, transformative discipline that empowers software teams to deliver cleaner, more reliable code. By embedding automated quality checks into the heart of daily work, TDD reduces bugs, eases maintenance, and fosters smart design—all while creating living documentation for the future. For organizations ready to level up their software process, embracing TDD is an investment that pays off in quality, confidence, and long-term agility.

</details>

### Evaluation Reasoning

**Single-agent (3.63/5):**
> "The content is well-organized, accurate, and provides a coherent explanation of the core benefits of TDD. However, it lacks concrete examples, data, or case studies to support claims, and while it briefly notes up-front time investment as a cost, it does not thoroughly address limitations or challenges. Practical implementation advice is mostly absent."

**Multi-agent (4.75/5):**
> "The content delivers comprehensive coverage of TDD's benefits, structure, real-world evidence, and challenges, with excellent organization and rich, engaging writing. It provides concrete examples and statistics from authoritative sources (IBM, Microsoft), ensuring accuracy and persuasive support for its claims. Both benefits and limitations are articulated with nuance."

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
