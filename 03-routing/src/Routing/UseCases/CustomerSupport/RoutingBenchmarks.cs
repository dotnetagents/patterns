using DotNetAgents.BenchmarkLlm.Core;
using DotNetAgents.Infrastructure;

namespace DotNetAgents.Patterns.Routing.UseCases.CustomerSupport;

/// <summary>
/// Benchmarks comparing routing vs single-agent approaches for customer support.
/// </summary>
[WorkflowBenchmark(
    "routing",
    Prompt = SampleTickets.DefaultBenchmarkTicket,
    Description = "Customer support routing: classify tickets and delegate to specialists"
)]
public class RoutingBenchmarks
{
    [BenchmarkLlm(
        "single-agent",
        Baseline = true,
        Description = "Single generic support agent handling all ticket types"
    )]
    public async Task<BenchmarkOutput> SingleAgent(string prompt)
    {
        var config = new SingleAgentSupportConfig
        {
            Provider = "azure",
            Model = "gpt-4.1",
        };

        var (workflow, agentModels) = SingleAgentSupportPipeline.Create(config);
        var response = await WorkflowRunner.RunAsync(workflow, prompt);

        return BenchmarkOutput.WithModels(response, agentModels);
    }

    [BenchmarkLlm(
        "routing",
        Description = "Classifier routes to specialist agents (gpt-4.1)"
    )]
    public async Task<BenchmarkOutput> Routing(string prompt)
    {
        var config = new RoutingPipelineConfig
        {
            Provider = "azure",
            ClassifierModel = "gpt-4.1",
            SpecialistModel = "gpt-4.1",
        };

        var (workflow, agentModels) = RoutingWorkflow.Create(config);
        var response = await WorkflowRunner.RunAsync(workflow, prompt);

        return BenchmarkOutput.WithModels(response, agentModels);
    }

    [BenchmarkLlm(
        "routing-mini-classifier",
        Description = "Fast classifier (gpt-4o-mini) routes to specialist (gpt-4.1)"
    )]
    public async Task<BenchmarkOutput> RoutingMiniClassifier(string prompt)
    {
        var config = new RoutingPipelineConfig
        {
            Provider = "azure",
            ClassifierModel = "gpt-4o-mini",
            SpecialistModel = "gpt-4.1",
        };

        var (workflow, agentModels) = RoutingWorkflow.Create(config);
        var response = await WorkflowRunner.RunAsync(workflow, prompt);

        return BenchmarkOutput.WithModels(response, agentModels);
    }
}
