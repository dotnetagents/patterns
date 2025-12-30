# DotNetAgents Patterns

Benchmark framework for comparing LLM agent patterns using Microsoft Agent Framework for .NET.

## What's Included

- **Pattern implementations** comparing multi-agent vs single-agent approaches
- **BenchmarkLlm framework** for measuring quality, tokens, and latency
- **LLM-as-Judge evaluation** with 8 quality dimensions
- **Support for multiple LLM providers**: Ollama, OpenAI, Azure OpenAI, OpenRouter

## Prerequisites

- .NET 8.0+ SDK
- An LLM provider configured (see below)

## LLM Provider Setup

Configure your preferred provider in `appsettings.json`:

### Ollama (Free, Local)
```json
{
  "LlmProvider": "ollama",
  "OllamaEndpoint": "http://localhost:11434",
  "OllamaModel": "llama3.2"
}
```

### OpenAI
```json
{
  "LlmProvider": "openai",
  "OpenAiApiKey": "sk-...",
  "OpenAiModel": "gpt-4.1"
}
```

### Azure OpenAI
```json
{
  "LlmProvider": "azure",
  "AzureOpenAiEndpoint": "https://your-resource.openai.azure.com/",
  "AzureOpenAiDeployment": "gpt-4o"
}
```

### OpenRouter
```json
{
  "LlmProvider": "openrouter",
  "OpenRouterApiKey": "sk-or-...",
  "OpenRouterModel": "openai/gpt-4.1"
}
```

## Patterns

| # | Pattern | Description | Status |
|---|---------|-------------|--------|
| 1 | [Prompt Chaining](./01-prompt-chaining/) | Sequential agents where output feeds the next | ✅ Complete |
| 2 | Tool Use | Agents calling external functions/APIs | 🔜 Coming |
| 3 | Routing | Conditional logic to select execution paths | 🔜 Coming |
| 4 | Parallelization | Running multiple agent tasks concurrently | 🔜 Coming |

## Quick Start

```bash
cd 01-prompt-chaining/src/PromptChaining

# Run the default workflow
dotnet run

# List available benchmarks
dotnet run -- --list-benchmarks

# Run benchmarks with evaluation
dotnet run -- --benchmark
```

## Running Benchmarks

The BenchmarkLlm framework measures and compares agent approaches:

```bash
# Run all benchmarks
dotnet run -- --benchmark

# Evaluate a previous run
dotnet run -- --evaluate ./runs/<run-id>
```

Results are saved to `runs/` with:
- `output.md` - Generated content from each approach
- `comparison.md` - Side-by-side metrics
- `analysis.md` - LLM-as-Judge quality evaluation

### Quality Dimensions

The LLM-as-Judge evaluates content across 8 dimensions:
- Completeness, Structure, Accuracy, Engagement
- Evidence Quality, Balance, Actionability, Depth

## Project Structure

```
patterns/
├── 01-prompt-chaining/          # Pattern 1: Prompt Chaining
│   └── src/PromptChaining/
│       ├── UseCases/            # Multi-agent and single-agent implementations
│       └── appsettings.json     # LLM provider configuration
├── src/
│   ├── DotNetAgents.Infrastructure/   # ChatClientFactory, WorkflowRunner
│   └── DotNetAgents.BenchmarkLlm/     # Benchmarking framework
└── runs/                        # Benchmark results (gitignored)
```

## Learn More

- [DotNetAgents.net](https://dotnetagents.net) - Full tutorials and explanations
- [Microsoft Agent Framework](https://learn.microsoft.com/en-us/agent-framework/) - Official documentation

## License

MIT License

---

*Inspired by [Agentic Design Patterns](https://www.amazon.com/Agentic-Design-Patterns-Hands-Intelligent/dp/3032014018) by Antonio Gulli. Implementations are original content using Microsoft Agent Framework.*
