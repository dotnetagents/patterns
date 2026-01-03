namespace DotNetAgents.Patterns.Routing.UseCases.CustomerSupport;

/// <summary>
/// Configuration for the routing support pipeline.
/// </summary>
public sealed class RoutingPipelineConfig
{
    /// <summary>
    /// The LLM provider to use (e.g., "azure", "openai", "ollama").
    /// </summary>
    public required string Provider { get; init; }

    /// <summary>
    /// Model used for the classifier agent.
    /// Can be a smaller/faster model since classification is simpler.
    /// </summary>
    public required string ClassifierModel { get; init; }

    /// <summary>
    /// Model used for specialist agents.
    /// </summary>
    public required string SpecialistModel { get; init; }
}

/// <summary>
/// Configuration for the single-agent baseline pipeline.
/// </summary>
public sealed class SingleAgentSupportConfig
{
    /// <summary>
    /// The LLM provider to use.
    /// </summary>
    public required string Provider { get; init; }

    /// <summary>
    /// The model to use for the generic support agent.
    /// </summary>
    public required string Model { get; init; }
}
