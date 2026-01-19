using DotNetAgents.Infrastructure;
using DotNetAgents.Patterns.Parallelization.Services;
using Microsoft.Agents.AI.Workflows;

namespace DotNetAgents.Patterns.Parallelization.UseCases.TravelPlanning;

/// <summary>
/// Pipeline that runs 3 specialized travel agents sequentially using AddEdge,
/// then aggregates results. Used as a baseline to compare latency against the parallel approach.
/// </summary>
public static class SequentialTravelPipeline
{
    /// <summary>
    /// Creates a sequential travel planning workflow using AddEdge chain.
    /// </summary>
    public static (Workflow Workflow, Dictionary<string, string> AgentModels) Create(
        TravelPlanningConfig config,
        IGoogleSearchService searchService)
    {
        // Create ChatClientAgent instances using factory
        var hotelsAgent = TravelAgentFactory.CreateHotelsAgent(config.Provider, config.HotelsModel, searchService);
        var transportAgent = TravelAgentFactory.CreateTransportAgent(config.Provider, config.TransportModel, searchService);
        var activitiesAgent = TravelAgentFactory.CreateActivitiesAgent(config.Provider, config.ActivitiesModel, searchService);

        // Create aggregation executor (collects results and synthesizes)
        var aggregationExecutor = new FanInExecutor(
            ChatClientFactory.Create(config.Provider, config.AggregatorModel),
            config.AggregatorModel,
            TravelAgentFactory.ExpectedAgentNames);

        // Build workflow with sequential execution using AddEdge chain
        // Hotels → Transport → Activities → Aggregator
        var workflow = new WorkflowBuilder(hotelsAgent)
            .AddEdge(hotelsAgent, transportAgent)
            .AddEdge(transportAgent, activitiesAgent)
            .AddEdge(activitiesAgent, aggregationExecutor)
            .WithOutputFrom(aggregationExecutor)
            .Build();

        var agentModels = new Dictionary<string, string>
        {
            [TravelAgentFactory.HotelsAgentName] = config.HotelsModel,
            [TravelAgentFactory.TransportAgentName] = config.TransportModel,
            [TravelAgentFactory.ActivitiesAgentName] = config.ActivitiesModel,
            ["Aggregator"] = config.AggregatorModel
        };

        return (workflow, agentModels);
    }
}
