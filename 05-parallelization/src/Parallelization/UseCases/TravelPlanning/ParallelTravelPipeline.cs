using DotNetAgents.Infrastructure;
using DotNetAgents.Patterns.Parallelization.Services;
using Microsoft.Agents.AI.Workflows;

namespace DotNetAgents.Patterns.Parallelization.UseCases.TravelPlanning;

/// <summary>
/// Pipeline that runs 3 specialized travel agents in parallel using the Microsoft Agent Framework's
/// native Fan-Out/Fan-In workflow pattern, then aggregates results.
/// Uses ChatClientAgent with Google Search tool for the parallel agents.
/// </summary>
public static class ParallelTravelPipeline
{
    /// <summary>
    /// Creates a parallel travel planning workflow using AddFanOutEdge and AddFanInEdge
    /// with ChatClientAgent instances that have Google Search tool.
    /// </summary>
    public static (Workflow Workflow, Dictionary<string, string> AgentModels) Create(
        TravelPlanningConfig config,
        IGoogleSearchService searchService)
    {
        // Create ChatClientAgent instances using factory
        var hotelsAgent = TravelAgentFactory.CreateHotelsAgent(config.Provider, config.HotelsModel, searchService);
        var transportAgent = TravelAgentFactory.CreateTransportAgent(config.Provider, config.TransportModel, searchService);
        var activitiesAgent = TravelAgentFactory.CreateActivitiesAgent(config.Provider, config.ActivitiesModel, searchService);

        // Create fan-out executor (broadcasts query to all agents)
        var fanOutExecutor = new FanOutExecutor();

        // Create fan-in executor (collects results and synthesizes)
        var fanInExecutor = new FanInExecutor(
            ChatClientFactory.Create(config.Provider, config.AggregatorModel),
            config.AggregatorModel,
            TravelAgentFactory.ExpectedAgentNames);

        // Build workflow with native parallel execution using Fan-Out/Fan-In pattern
        var workflow = new WorkflowBuilder(fanOutExecutor)
            .AddFanOutEdge(fanOutExecutor, targets: [hotelsAgent, transportAgent, activitiesAgent])
            .AddFanInEdge([hotelsAgent, transportAgent, activitiesAgent], fanInExecutor)
            .WithOutputFrom(fanInExecutor)
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
