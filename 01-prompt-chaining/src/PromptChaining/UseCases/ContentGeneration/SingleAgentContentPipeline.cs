using DotNetAgents.Infrastructure;
using Microsoft.Agents.AI.Workflows;

namespace DotNetAgents.Patterns.PromptChaining.UseCases.ContentGeneration;

/// <summary>
///     Alternative implementation using a single agent with combined instructions.
///     Used as a baseline for comparison against the multi-agent prompt chaining approach.
/// </summary>
public static class SingleAgentContentPipeline
{
    public static (Workflow, Dictionary<string, string>) Create(string provider, string model)
    {
        var writer = new AgentExecutor(
            new AgentConfig
            {
                Name = "CombinedContentAgent",
                Provider = provider,
                Model = model,
                Instructions = """
                You are a professional content writer who creates engaging, well-structured articles.

                When given a topic, follow these steps internally:

                ## Step 1: Research
                - Identify the main concepts and subtopics
                - List 3-5 key points that should be covered
                - Note any important facts, statistics, or examples
                - Identify the target audience and appropriate tone

                ## Step 2: Outline
                - Create a hierarchical outline with main sections and subsections
                - Include brief descriptions for each section (1-2 sentences)
                - Suggest an introduction hook and conclusion
                - Recommend word count per section
                - Focus on logical flow and reader engagement

                ## Step 3: Write
                - Write polished content following the outline structure
                - Use clear, engaging language appropriate for the target audience
                - Include transitions between sections
                - Add a compelling introduction and satisfying conclusion

                IMPORTANT: Output ONLY the final article. Do not include the research notes, outline, or any previous context in your output.

                Your output should be a complete, polished article with headers and formatted paragraphs.
                Maintain consistent tone throughout.
                """,
            }
        );
        var workflow = new WorkflowBuilder(writer).Build();
        var agentModels = new Dictionary<string, string> { { writer.Name, writer.Model } };
        return (workflow, agentModels);
    }
}
