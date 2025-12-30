# DotNetAgents Patterns

Code examples for 21 agentic AI patterns using Microsoft Agent Framework for .NET.

## Prerequisites

- .NET 8.0+ SDK
- Azure OpenAI resource or OpenAI API key
- Azure CLI (for Azure OpenAI authentication)

## Environment Setup

Set the following environment variables:

```bash
# Windows PowerShell
$env:AZURE_OPENAI_ENDPOINT = "https://your-resource.openai.azure.com/"
$env:AZURE_OPENAI_DEPLOYMENT_NAME = "gpt-4o-mini"

# Linux/macOS
export AZURE_OPENAI_ENDPOINT=https://your-resource.openai.azure.com/
export AZURE_OPENAI_DEPLOYMENT_NAME=gpt-4o-mini
```

## Patterns

### Foundation Patterns (Beginner)

| # | Pattern | Description |
|---|---------|-------------|
| 1 | [Prompt Chaining](./01-prompt-chaining/) | Sequential LLM calls where output feeds the next prompt |
| 2 | Tool Use | Agents calling external functions/APIs |
| 3 | Routing | Conditional logic to select execution paths |
| 4 | Parallelization | Running multiple agent tasks concurrently |

### Core Patterns (Intermediate)

| # | Pattern | Description |
|---|---------|-------------|
| 5 | Reflection | Agents critiquing and improving their outputs |
| 6 | Planning | Breaking down complex tasks into subtasks |
| 7 | Multi-Agent Collaboration | Multiple specialized agents working together |
| 8 | Memory Management | Short-term and long-term memory for agents |

### Advanced Patterns (Advanced)

| # | Pattern | Description |
|---|---------|-------------|
| 9+ | Coming Soon | More patterns being added... |

## Running a Pattern

```bash
cd 01-prompt-chaining/src/PromptChaining
dotnet run
```

## Running Tests

```bash
cd 01-prompt-chaining/tests/PromptChaining.Tests
dotnet test
```

## Learn More

- [DotNetAgents.net](https://dotnetagents.net) - Full tutorials and explanations
- [Microsoft Agent Framework](https://learn.microsoft.com/en-us/agent-framework/) - Official documentation
- [GitHub](https://github.com/microsoft/agent-framework) - Framework source code

## License

MIT License - See [LICENSE](./LICENSE) for details.

---

*Inspired by "Agentic Design Patterns" by Antonio Gulli. Implementations are original content using Microsoft Agent Framework.*
