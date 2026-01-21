using DotNetAgents.BenchmarkLlm.Core;
using DotNetAgents.Infrastructure;

namespace DotNetAgents.Patterns.Reflection.UseCases.StartupPitch;

[WorkflowBenchmark(
    "reflection",
    Prompt = "An AI-powered platform connecting travelers with sustainable Alpine experiences in Switzerland. Features eco-certified accommodations, local mountain guides, farm-to-table restaurants, and carbon-neutral transport options. Uses AI to create personalized itineraries that minimize environmental impact while maximizing authentic Swiss mountain culture experiences. Target market: environmentally-conscious international travelers seeking premium sustainable tourism in the Swiss Alps.",
    Description = "Startup pitch generation with reflection pattern"
)]
public class ReflectionBenchmarks
{
    [BenchmarkLlm(
        "single-shot",
        Baseline = true,
        Description = "Direct pitch generation without reflection"
    )]
    public async Task<BenchmarkOutput> SingleShot(string prompt)
    {
        var (workflow, agentModels) = SingleShotPipeline.Create("azure", "gpt-4.1");
        var content = await WorkflowRunner.RunAsync(workflow, prompt);
        return BenchmarkOutput.WithModels(content, agentModels);
    }

    [BenchmarkLlm(
        "self-reflection",
        Description = "Single agent with internal self-critique loop"
    )]
    public async Task<BenchmarkOutput> SelfReflection(string prompt)
    {
        var (workflow, agentModels) = SelfReflectionPipeline.Create("azure", "gpt-4.1");
        var content = await WorkflowRunner.RunAsync(workflow, prompt);
        return BenchmarkOutput.WithModels(content, agentModels);
    }

    [BenchmarkLlm(
        "two-agent",
        Description = "PitchWriter + VCCritic iterative refinement (max 3 iterations)"
    )]
    public async Task<BenchmarkOutput> TwoAgent(string prompt)
    {
        var (workflow, agentModels) = ReflectionPipeline.Create(
            new ReflectionPipelineConfig
            {
                Provider = "azure",
                WriterModel = "gpt-4.1",
                CriticModel = "gpt-4.1",
                MaxIterations = 3
            }
        );

        var content = await WorkflowRunner.RunAsync(workflow, prompt);
        return BenchmarkOutput.WithModels(content, agentModels);
    }

}
