using DotNetAgents.Infrastructure;
using Microsoft.Agents.AI.Workflows;

namespace DotNetAgents.Patterns.Reflection.UseCases.StartupPitch;

/// <summary>
/// Configuration for the two-agent reflection pipeline.
/// </summary>
public record ReflectionPipelineConfig
{
    public required string Provider { get; init; }
    public required string WriterModel { get; init; }
    public required string CriticModel { get; init; }
    public int MaxIterations { get; init; } = 3;
}

/// <summary>
/// Two-agent reflection pipeline using PitchWriter and VCCritic agents.
/// The VCCritic evaluates the pitch and provides feedback until approved.
/// </summary>
public static class ReflectionPipeline
{
    public static (Workflow, Dictionary<string, string>) Create(ReflectionPipelineConfig config)
    {
        var writerClient = ChatClientFactory.Create(config.Provider, config.WriterModel);
        var criticClient = ChatClientFactory.Create(config.Provider, config.CriticModel);

        var reflectionLoop = new ReflectionLoopExecutor(
            writerClient,
            criticClient,
            config.MaxIterations
        );

        var workflow = new WorkflowBuilder(reflectionLoop).Build();

        var agentModels = new Dictionary<string, string>
        {
            ["PitchWriter"] = config.WriterModel,
            ["VCCritic"] = config.CriticModel
        };

        return (workflow, agentModels);
    }
}
