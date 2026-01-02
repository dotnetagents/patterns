namespace DotNetAgents.Infrastructure;

public class AgentConfig
{
    public required string Name { get; init; }

    public required string Instructions { get; init; }

    /// <summary>
    /// Provider name: "ollama", "openai", "azure", "openrouter", "github".
    /// </summary>
    public required string Provider { get; init; }

    public required string Model { get; init; }
}
