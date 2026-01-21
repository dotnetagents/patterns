using DotNetAgents.Infrastructure;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace DotNetAgents.Patterns.Reflection.UseCases.StartupPitch;

/// <summary>
/// Custom executor that manages the generate-critique-refine loop internally.
/// Similar to FanInExecutor's stateful approach, but for iterative refinement.
/// </summary>
internal sealed class ReflectionLoopExecutor : Executor<ChatMessage, ChatMessage>
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

    private readonly IChatClient _writerClient;
    private readonly IChatClient _criticClient;
    private readonly int _maxIterations;
    private readonly List<ChatMessage> _writerHistory = [];

    public ReflectionLoopExecutor(
        IChatClient writerClient,
        IChatClient criticClient,
        int maxIterations = 3)
        : base("ReflectionLoop")
    {
        _writerClient = writerClient;
        _criticClient = criticClient;
        _maxIterations = maxIterations;
    }

    public int IterationsUsed { get; private set; }
    public bool WasApproved { get; private set; }

    public override async ValueTask<ChatMessage> HandleAsync(
        ChatMessage input,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        // Initialize writer conversation with system prompt
        _writerHistory.Clear();
        _writerHistory.Add(new ChatMessage(ChatRole.System, PitchWriterPrompt));
        _writerHistory.Add(new ChatMessage(ChatRole.User, $"Create a startup pitch for: {input.Text}"));

        // Generate initial pitch
        Console.WriteLine("\n[Iteration 1] Generating initial pitch...");
        var pitchResponse = await _writerClient.GetResponseAsync(_writerHistory, cancellationToken: cancellationToken);
        var pitch = pitchResponse.Text ?? string.Empty;
        _writerHistory.Add(new ChatMessage(ChatRole.Assistant, pitch));
        Console.WriteLine($"[Iteration 1] Generated initial pitch ({pitch.Length} chars)");

        for (int i = 0; i < _maxIterations; i++)
        {
            // Get critique from VC
            var criticMessages = new List<ChatMessage>
            {
                new(ChatRole.System, VCCriticPrompt),
                new(ChatRole.User, $"Please evaluate this startup pitch:\n\n{pitch}")
            };

            Console.WriteLine($"\n[Iteration {i + 1}] Getting VC critique...");
            var critiqueResponse = await _criticClient.GetResponseAsync(criticMessages, cancellationToken: cancellationToken);
            var critique = critiqueResponse.Text ?? string.Empty;

            var critiquePreview = critique.Length > 200
                ? critique[..200] + "..."
                : critique;
            Console.WriteLine($"[Iteration {i + 1}] VC Critique: {critiquePreview}");

            // Check for approval
            if (critique.Contains("APPROVED", StringComparison.OrdinalIgnoreCase) &&
                !critique.Contains("NOT APPROVED", StringComparison.OrdinalIgnoreCase) &&
                !critique.Contains("NEEDS_REVISION", StringComparison.OrdinalIgnoreCase))
            {
                IterationsUsed = i + 1;
                WasApproved = true;
                Console.WriteLine($"\n[APPROVED after {IterationsUsed} iteration(s)]");
                return new ChatMessage(ChatRole.Assistant, pitch) { AuthorName = "PitchWriter" };
            }

            // If this is the last iteration, return regardless
            if (i == _maxIterations - 1)
            {
                break;
            }

            // Refine based on feedback
            Console.WriteLine($"\n[Iteration {i + 2}] Refining pitch based on feedback...");
            _writerHistory.Add(new ChatMessage(ChatRole.User,
                $"The VC provided this feedback on your pitch:\n\n{critique}\n\nPlease revise the pitch to address these concerns while maintaining a compelling narrative."));

            pitchResponse = await _writerClient.GetResponseAsync(_writerHistory, cancellationToken: cancellationToken);
            pitch = pitchResponse.Text ?? string.Empty;
            _writerHistory.Add(new ChatMessage(ChatRole.Assistant, pitch));
            Console.WriteLine($"[Iteration {i + 2}] Refined pitch ({pitch.Length} chars)");
        }

        IterationsUsed = _maxIterations;
        WasApproved = false;
        Console.WriteLine($"\n[Max iterations ({_maxIterations}) reached]");
        return new ChatMessage(ChatRole.Assistant, pitch) { AuthorName = "PitchWriter" };
    }
}
