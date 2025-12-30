namespace DotNetAgents.Infrastructure;

public class AgentConfig
{
    public required string Name { get; init; }

    public required string Instructions { get; init; }

    public required string Model { get; init; }
}
