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
        "single-agent",
        Baseline = true,
        Description = "Single agent with combined instructions"
    )]
    public async Task<BenchmarkOutput> SingleAgent(string prompt)
    {
        var (workflow, agentModels) = SingleAgentContentPipeline.Create("azure", "gpt-4.1");
        var content = await WorkflowRunner.RunAsync(workflow, prompt);
        return BenchmarkOutput.WithModels(content, agentModels);
    }
    
    [BenchmarkLlm(
        "multi-agent",
        Description = "3-agent pipeline: Researcher -> Outliner -> Writer"
    )]
    public async Task<BenchmarkOutput> MultiAgent(string prompt)
    {
        var (workflow, agentModels) = MultiAgentContentPipeline.Create(
            new MultiAgentContentPipelineConfig
            {
                Provider = "azure",
                ResearcherModel = "gpt-4.1",
                OutlinerModel = "gpt-4.1",
                WriterModel = "gpt-4.1",
            }
        );

        var content = await WorkflowRunner.RunAsync(workflow, prompt);
        return BenchmarkOutput.WithModels(content, agentModels);
    }
    
    [BenchmarkLlm(
        "multi-agent-local",
        Description = "3-agent pipeline (ollam): Researcher -> Outliner -> Writer"
    )]
    public async Task<BenchmarkOutput> MultiAgentOllama(string prompt)
    {
        var (workflow, agentModels) = MultiAgentContentPipeline.Create(
            new MultiAgentContentPipelineConfig
            {
                Provider = "ollama",
                ResearcherModel = "llama3.2",
                OutlinerModel = "llama3.2",
                WriterModel = "llama3.2",
            }
        );
    
        var content = await WorkflowRunner.RunAsync(workflow, prompt);
        return BenchmarkOutput.WithModels(content, agentModels);
    }
}
