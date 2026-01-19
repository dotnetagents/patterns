using DotNetAgents.BenchmarkLlm.Core;
using DotNetAgents.Infrastructure;
using DotNetAgents.Patterns.Parallelization.Services;

namespace DotNetAgents.Patterns.Parallelization.UseCases.TravelPlanning;

/// <summary>
/// Benchmarks comparing parallel vs sequential vs single-agent approaches for travel planning.
/// Uses Microsoft Agent Framework's native Fan-Out/Fan-In pattern for parallel execution.
/// Measures both quality and latency differences.
/// </summary>
[WorkflowBenchmark(
    "parallelization",
    Prompt = SampleQueries.DefaultBenchmarkQuery,
    Description = "Travel planning with parallel agent execution using Fan-Out/Fan-In pattern"
)]
public class ParallelizationBenchmarks
{
    [BenchmarkLlm(
        "single-agent",
        Baseline = true,
        Description = "Single agent handling all travel research"
    )]
    public async Task<BenchmarkOutput> SingleAgent(string prompt)
    {
        var searchService = new MockGoogleSearchService();
        var config = new SingleAgentTravelConfig
        {
            Provider = "azure",
            Model = "gpt-4.1"
        };

        var (result, agentModels) = await SingleAgentTravelPipeline.RunAsync(
            config, searchService, prompt);

        return BenchmarkOutput.WithModels(result, agentModels);
    }

    [BenchmarkLlm(
        "sequential",
        Description = "3 specialized agents running sequentially (AddEdge chain) + aggregator"
    )]
    public async Task<BenchmarkOutput> Sequential(string prompt)
    {
        var searchService = new MockGoogleSearchService();
        var config = new TravelPlanningConfig
        {
            Provider = "azure",
            HotelsModel = "gpt-4.1",
            TransportModel = "gpt-4.1",
            ActivitiesModel = "gpt-4.1",
            AggregatorModel = "gpt-4.1"
        };

        // Create workflow using AddEdge chain for sequential execution
        var (workflow, agentModels) = SequentialTravelPipeline.Create(config, searchService);

        // Run using WorkflowRunner
        var result = await WorkflowRunner.RunAsync(workflow, prompt);

        return BenchmarkOutput.WithModels(result, agentModels);
    }

    [BenchmarkLlm(
        "parallel",
        Description = "3 specialized agents running in parallel (AddFanOutEdge/AddFanInEdge) + aggregator"
    )]
    public async Task<BenchmarkOutput> Parallel(string prompt)
    {
        var searchService = new MockGoogleSearchService();
        var config = new TravelPlanningConfig
        {
            Provider = "azure",
            HotelsModel = "gpt-4.1",
            TransportModel = "gpt-4.1",
            ActivitiesModel = "gpt-4.1",
            AggregatorModel = "gpt-4.1"
        };

        // Create workflow using AddFanOutEdge/AddFanInEdge for parallel execution
        var (workflow, agentModels) = ParallelTravelPipeline.Create(config, searchService);

        // Run using WorkflowRunner
        var result = await WorkflowRunner.RunAsync(workflow, prompt);

        return BenchmarkOutput.WithModels(result, agentModels);
    }

    [BenchmarkLlm(
        "parallel-mini",
        Description = "3 mini models in parallel (Fan-Out) + gpt-4.1 aggregator (Fan-In)"
    )]
    public async Task<BenchmarkOutput> ParallelMini(string prompt)
    {
        var searchService = new MockGoogleSearchService();
        var config = new TravelPlanningConfig
        {
            Provider = "azure",
            HotelsModel = "gpt-4o-mini",
            TransportModel = "gpt-4o-mini",
            ActivitiesModel = "gpt-4o-mini",
            AggregatorModel = "gpt-4.1"
        };

        // Create workflow using AddFanOutEdge/AddFanInEdge for parallel execution
        var (workflow, agentModels) = ParallelTravelPipeline.Create(config, searchService);

        // Run using WorkflowRunner
        var result = await WorkflowRunner.RunAsync(workflow, prompt);

        return BenchmarkOutput.WithModels(result, agentModels);
    }
}
