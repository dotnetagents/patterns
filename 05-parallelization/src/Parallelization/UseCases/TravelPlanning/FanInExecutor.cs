using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace DotNetAgents.Patterns.Parallelization.UseCases.TravelPlanning;

/// <summary>
///     Executor that collects results from all parallel agents and synthesizes them
///     into a comprehensive travel plan.
///     Part of the Fan-In pattern in Microsoft Agent Framework.
/// </summary>
internal sealed class FanInExecutor : Executor<List<ChatMessage>, ChatMessage>
{
    private const string SynthesisSystemPrompt = """
         You are an expert travel planner who creates comprehensive, cohesive travel itineraries.

         You will receive research from three specialized agents:
         1. Hotels Agent - with accommodation recommendations
         2. Transport Agent - with flight and transportation options
         3. Activities Agent - with things to do, restaurants, and day plans

         Your job is to synthesize all of this research into a single, well-organized travel plan that:
         - Presents a coherent narrative, not just bullet points
         - Resolves any conflicts between recommendations
         - Prioritizes based on the traveler's apparent needs
         - Creates a realistic, day-by-day itinerary
         - Includes practical logistics (how to get from hotel to activities, etc.)
         - Provides a budget estimate

         Structure your response as a complete travel guide that the traveler can follow.
         Be enthusiastic but practical. Make the trip feel exciting while keeping logistics manageable.
         """;

    private readonly Dictionary<string, ChatMessage> _agentResults = new();
    private readonly IChatClient _chatClient;
    private readonly HashSet<string> _expectedAgents;

    public FanInExecutor(IChatClient chatClient, string model, IEnumerable<string> expectedAgents)
        : base("FanInExecutor")
    {
        _chatClient = chatClient;
        Model = model;
        _expectedAgents = expectedAgents.ToHashSet();
    }

    public string Model { get; }

    public override async ValueTask<ChatMessage> HandleAsync(List<ChatMessage> messages,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"\n[FanIn] Received {messages.Count} messages:");

        // Collect results by agent name, filtering out tool call messages
        foreach (var msg in messages)
        {
            var agentName = msg.AuthorName;
            var hasToolContent = msg.Contents.Any(c => c is FunctionCallContent or FunctionResultContent);
            var textLength = msg.Text?.Length ?? 0;

            Console.WriteLine($"  - Author: '{agentName}', Role: {msg.Role}, TextLen: {textLength}, HasToolContent: {hasToolContent}");

            if (string.IsNullOrEmpty(agentName))
            {
                Console.WriteLine("    -> Skipped: no author name");
                continue;
            }

            // Skip tool call messages (function requests and results)
            if (hasToolContent)
            {
                Console.WriteLine("    -> Skipped: tool call message");
                continue;
            }

            if (string.IsNullOrEmpty(msg.Text))
            {
                Console.WriteLine("    -> Skipped: empty text");
                continue;
            }

            if (_expectedAgents.Contains(agentName))
            {
                _agentResults[agentName] = msg;
                Console.WriteLine($"    -> Collected! Now have: {string.Join(", ", _agentResults.Keys)}");
            }
            else
            {
                Console.WriteLine($"    -> Skipped: not in expected agents ({string.Join(", ", _expectedAgents)})");
            }
        }

        Console.WriteLine($"[FanIn] Collected: [{string.Join(", ", _agentResults.Keys)}], Waiting for: [{string.Join(", ", _expectedAgents.Except(_agentResults.Keys))}]");

        // Wait until all expected agents have responded
        if (!_expectedAgents.All(a => _agentResults.ContainsKey(a)))
        {
            Console.WriteLine("[FanIn] Not all agents responded yet, waiting...\n");
            return null!;
        }

        Console.WriteLine("[FanIn] All agents responded! Synthesizing...");

        // Format collected research for synthesis
        var researchSummary = string.Join("\n\n---\n\n",
            _agentResults.Values.Select(m => $"## Research from {m.AuthorName}\n\n{m.Text}"));

        var synthesisPrompt = $"""
                               Synthesize this travel research into a comprehensive travel plan:

                               {researchSummary}

                               Create a cohesive itinerary that combines the best hotel, transportation, and activity recommendations.
                               Include a day-by-day plan and budget estimate.
                               """;

        var chatMessages = new List<ChatMessage>
        {
            new(ChatRole.System, SynthesisSystemPrompt),
            new(ChatRole.User, synthesisPrompt)
        };

        var response = await _chatClient.GetResponseAsync(chatMessages, cancellationToken: cancellationToken);

        Console.WriteLine($"[FanIn] Synthesis complete, returning {response.Text?.Length ?? 0} chars");
        return new ChatMessage(ChatRole.Assistant, response.Text ?? string.Empty)
        {
            AuthorName = "Aggregator"
        };
    }
}