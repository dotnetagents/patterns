using DotNetAgents.Infrastructure;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace DotNetAgents.Patterns.Reflection.UseCases.StartupPitch;

/// <summary>
/// Custom executor for self-reflection where a single agent critiques and improves its own work.
/// Uses multi-turn conversation to generate, self-critique, and refine.
/// </summary>
internal sealed class SelfReflectionExecutor : Executor<ChatMessage, ChatMessage>
{
    private const string SelfReflectionPrompt = """
        You are an expert startup pitch writer with strong self-critique abilities.

        You will go through three phases:

        PHASE 1 - INITIAL DRAFT:
        Create a compelling startup pitch covering: Problem, Solution, Market Size,
        Business Model, Traction, Competition, Team, and The Ask.

        PHASE 2 - SELF-CRITIQUE:
        After creating the draft, put on your "skeptical VC" hat and critically evaluate
        your own pitch. Consider: Is the problem compelling? Is the market size credible?
        Is the business model viable? What are the weaknesses?

        PHASE 3 - REVISION:
        Based on your self-critique, revise and strengthen the pitch. Address the
        weaknesses you identified while maintaining a compelling narrative.

        Output ONLY the final revised pitch after completing all three phases internally.
        Do not show the intermediate steps - just provide the polished final pitch.
        """;

    private readonly IChatClient _chatClient;

    public SelfReflectionExecutor(IChatClient chatClient)
        : base("SelfReflection")
    {
        _chatClient = chatClient;
    }

    public override async ValueTask<ChatMessage> HandleAsync(
        ChatMessage input,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine("\n[SelfReflection] Generating pitch with internal self-critique...");

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, SelfReflectionPrompt),
            new(ChatRole.User, $"Create a startup pitch for: {input.Text}")
        };

        var response = await _chatClient.GetResponseAsync(messages, cancellationToken: cancellationToken);
        var pitch = response.Text ?? string.Empty;

        Console.WriteLine($"[SelfReflection] Generated self-critiqued pitch ({pitch.Length} chars)");

        return new ChatMessage(ChatRole.Assistant, pitch) { AuthorName = "SelfReflectionAgent" };
    }
}

/// <summary>
/// Self-reflection pipeline where a single agent generates, critiques, and refines its own work.
/// This is a simpler alternative to the two-agent approach with fewer API calls.
/// </summary>
public static class SelfReflectionPipeline
{
    public static (Workflow, Dictionary<string, string>) Create(string provider, string model)
    {
        var chatClient = ChatClientFactory.Create(provider, model);
        var selfReflection = new SelfReflectionExecutor(chatClient);

        var workflow = new WorkflowBuilder(selfReflection).Build();
        var agentModels = new Dictionary<string, string> { ["SelfReflectionAgent"] = model };

        return (workflow, agentModels);
    }
}
