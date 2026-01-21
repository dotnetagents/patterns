using DotNetAgents.Infrastructure;
using Microsoft.Agents.AI.Workflows;

namespace DotNetAgents.Patterns.Reflection.UseCases.StartupPitch;

/// <summary>
/// Baseline implementation using a single agent without any reflection.
/// Used for comparison against the reflection-based approaches.
/// </summary>
public static class SingleShotPipeline
{
    public static (Workflow, Dictionary<string, string>) Create(string provider, string model)
    {
        var pitchWriter = new AgentExecutor(
            new AgentConfig
            {
                Name = "SingleShotPitchWriter",
                Provider = provider,
                Model = model,
                Instructions = """
                    You are an expert startup pitch writer. Given a startup idea, create a
                    compelling pitch that covers all the essential elements investors look for.

                    Structure your pitch with these sections:
                    1. **Problem**: What pain point are you solving? Make it compelling.
                    2. **Solution**: How does your product/service solve this problem?
                    3. **Market Size**: TAM/SAM/SOM - be specific with numbers.
                    4. **Business Model**: How will you make money?
                    5. **Traction**: Any validation, users, or revenue?
                    6. **Competition**: Who else is in this space? What's your moat?
                    7. **Team**: Why is this the right team? (hypothetical if not specified)
                    8. **The Ask**: What funding are you seeking and what will you use it for?

                    Make the pitch persuasive yet realistic. Use specific numbers and examples
                    where possible. The pitch should be ready for an investor presentation.
                    """,
            }
        );

        var workflow = new WorkflowBuilder(pitchWriter).Build();
        var agentModels = new Dictionary<string, string> { { pitchWriter.Name, pitchWriter.Model } };
        return (workflow, agentModels);
    }
}
