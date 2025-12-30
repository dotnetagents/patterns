using DotNetAgents.Infrastructure;
using Microsoft.Agents.AI.Workflows;

namespace DotNetAgents.Patterns.PromptChaining.UseCases.ContentGeneration;

public sealed class MultiAgentContentPipelineConfig
{
    public required string ResearcherModel { get; init; }

    public required string OutlinerModel { get; init; }

    public required string WriterModel { get; init; }
}

public static class MultiAgentContentPipeline
{
    public static (Workflow, Dictionary<string, string>) Create(
        MultiAgentContentPipelineConfig config
    )
    {
        // Create specialized agents with configured models
        // Use factory if provided for metrics tracking, otherwise create directly
        var researcher = ChatClientFactory.CreateAgent(
            new AgentConfig
            {
                Name = "Researcher",
                Model = config.ResearcherModel,
                Instructions = """
                You are a research assistant specializing in gathering and synthesizing information.

                When given a topic:
                1. Identify the main concepts and subtopics
                2. List 3-5 key points that should be covered
                3. Note any important facts, statistics, or examples
                4. Identify the target audience and appropriate tone

                Output format: Structured research notes with clear sections.
                Keep your response concise but comprehensive.
                """,
            }
        );

        var outliner = ChatClientFactory.CreateAgent(
            new AgentConfig
            {
                Name = "Outliner",
                Model = config.OutlinerModel,
                Instructions = """
                You are a content strategist who creates clear, logical outlines.

                Based on the research notes provided:
                1. Create a hierarchical outline with main sections and subsections
                2. Include brief descriptions for each section (1-2 sentences)
                3. Suggest an introduction hook and conclusion
                4. Recommend word count per section

                Output format: Numbered outline with clear hierarchy.
                Focus on logical flow and reader engagement.
                """,
            }
        );

        var writer = ChatClientFactory.CreateAgent(
            new AgentConfig
            {
                Name = "Writer",
                Model = config.WriterModel,
                Instructions = """
                You are a professional content writer who creates engaging, well-structured articles.

                Based on the outline provided:
                1. Write polished content following the outline structure
                2. Use clear, engaging language appropriate for the target audience
                3. Include transitions between sections
                4. Add a compelling introduction and satisfying conclusion

                IMPORTANT: Output ONLY the final article. Do not include the research notes, outline, or any previous context in your output.

                Your output should be a complete, polished article with headers and formatted paragraphs.
                Maintain consistent tone throughout.
                """,
            }
        );

        // Track model configuration
        var agentModels = new List<ConfiguredAgent> { researcher, outliner, writer }.ToDictionary(
            agent => agent.Name,
            agent => agent.Model
        );

        // Build workflow
        return (
            new WorkflowBuilder(researcher.ChatClientAgent)
                .AddEdge(researcher.ChatClientAgent, outliner.ChatClientAgent)
                .AddEdge(outliner.ChatClientAgent, writer.ChatClientAgent)
                .Build(),
            agentModels
        );
    }
}
