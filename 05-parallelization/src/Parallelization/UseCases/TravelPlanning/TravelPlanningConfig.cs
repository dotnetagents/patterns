namespace DotNetAgents.Patterns.Parallelization.UseCases.TravelPlanning;

/// <summary>
/// Configuration for the parallel travel planning pipeline.
/// Allows different models for each specialized agent.
/// </summary>
public sealed class TravelPlanningConfig
{
    public required string Provider { get; init; }
    public required string HotelsModel { get; init; }
    public required string TransportModel { get; init; }
    public required string ActivitiesModel { get; init; }
    public required string AggregatorModel { get; init; }
}

/// <summary>
/// Configuration for single-agent travel planning (baseline).
/// </summary>
public sealed class SingleAgentTravelConfig
{
    public required string Provider { get; init; }
    public required string Model { get; init; }
}
