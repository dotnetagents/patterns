using Microsoft.Agents.AI;

namespace DotNetAgents.Infrastructure;

public class ConfiguredAgent
{
    public required string Name { get; init; }

    public required string Instructions { get; init; }

    public required string Model { get; init; }

    public required ChatClientAgent ChatClientAgent { get; set; }
}
