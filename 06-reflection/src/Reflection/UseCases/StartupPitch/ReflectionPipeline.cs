using DotNetAgents.Infrastructure;
using Microsoft.Agents.AI;
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
/// Uses Microsoft Agent Framework's Group Chat orchestration with RoundRobin pattern.
/// The VCCritic evaluates the pitch and provides feedback until approved.
/// </summary>
public static class ReflectionPipeline
{
    private const string PitchWriterPrompt = """
        You are an expert startup pitch writer. Given a startup idea, create a
        compelling pitch that covers: Problem, Solution, Market Size, Business Model,
        Traction, Competition, Team, and The Ask.

        When given feedback from investors, thoughtfully address their concerns
        while maintaining a compelling narrative. Focus on making the pitch stronger
        with each revision.

        Structure your pitch with clear sections and make it persuasive yet realistic.
        """;

    private const string VCCriticPrompt = """
        You are a skeptical VC partner evaluating startup pitches. Your job is to
        find weaknesses and ask tough questions. Evaluate the pitch on:
        - Problem clarity and market pain
        - Solution-market fit
        - TAM/SAM/SOM credibility
        - Competitive differentiation
        - Business model viability
        - Traction and validation
        - Team execution ability

        Provide specific, actionable feedback. Be constructive but rigorous.

        IMPORTANT: At the end of your critique, you MUST include one of these verdicts:
        - "APPROVED" - if the pitch is strong enough for an investor meeting
        - "NEEDS_REVISION" - if the pitch needs more work

        Only approve if the pitch genuinely addresses the core investor concerns.
        """;

    public static (Workflow, Dictionary<string, string>) Create(ReflectionPipelineConfig config)
    {
        var writerClient = ChatClientFactory.Create(config.Provider, config.WriterModel);
        var criticClient = ChatClientFactory.Create(config.Provider, config.CriticModel);

        // Create ChatClientAgent instances
        var writer = new ChatClientAgent(writerClient, PitchWriterPrompt, "PitchWriter", "Pitch writer agent");
        var critic = new ChatClientAgent(criticClient, VCCriticPrompt, "VCCritic", "VC critic agent");

        // Create workflow with Group Chat orchestration using custom approval-based termination
        var workflow = AgentWorkflowBuilder
            .CreateGroupChatBuilderWith(agents =>
                new ApprovalBasedGroupChatManager(agents, "VCCritic")
                {
                    MaximumIterationCount = config.MaxIterations * 2 // 2 turns per iteration (writer + critic)
                })
            .AddParticipants(writer, critic)
            .Build();

        var agentModels = new Dictionary<string, string>
        {
            ["PitchWriter"] = config.WriterModel,
            ["VCCritic"] = config.CriticModel
        };

        return (workflow, agentModels);
    }
}
