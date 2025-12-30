using DotNetAgents.BenchmarkLlm.Core;
using DotNetAgents.Infrastructure;

namespace DotNetAgents.Patterns.PromptChaining.UseCases.ContentGeneration;

[WorkflowBenchmark(
    "prompt-chaining",
    Prompt = "The benefits of test-driven development in software engineering",
    Description = "Content generation with prompt chaining"
)]
public class PromptChainingBenchmarks
{
    [BenchmarkLlm(
        "multi-agent",
        Description = "3-agent pipeline: Researcher -> Outliner -> Writer"
    )]
    public async Task<BenchmarkOutput> MultiAgent(string prompt)
    {
        var (workflow, agentModels) = MultiAgentContentPipeline.Create(
            new MultiAgentContentPipelineConfig
            {
                ResearcherModel = "gpt-4.1",
                OutlinerModel = "gpt-4.1",
                WriterModel = "gpt-4.1",
            }
        );

        var content = await WorkflowRunner.RunAsync(workflow, prompt);
        return BenchmarkOutput.WithModels(content, agentModels);
    }

    [BenchmarkLlm(
        "single-agent",
        Baseline = true,
        Description = "Single agent with combined instructions"
    )]
    public async Task<BenchmarkOutput> SingleAgent(string prompt)
    {
        var (workflow, agentModels) = SingleAgentContentPipeline.Create("gpt-4.1");
        var content = await WorkflowRunner.RunAsync(workflow, prompt);
        return BenchmarkOutput.WithModels(content, agentModels);
    }
}
