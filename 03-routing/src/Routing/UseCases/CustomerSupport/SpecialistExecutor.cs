using DotNetAgents.Infrastructure;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace DotNetAgents.Patterns.Routing.UseCases.CustomerSupport;

/// <summary>
/// Executor that handles classified support tickets with domain-specific expertise.
/// </summary>
public sealed class SpecialistExecutor : Executor<RoutingDecision, ChatMessage>
{
    private readonly IChatClient _chatClient;
    private readonly string _instructions;

    public string Model { get; }
    public SupportCategory Category { get; }

    public SpecialistExecutor(
        string provider,
        string model,
        SupportCategory category)
        : base($"{category}Specialist")
    {
        _chatClient = ChatClientFactory.Create(provider, model);
        _instructions = SpecialistInstructions.GetInstructions(category);
        Model = model;
        Category = category;
    }

    public override async ValueTask<ChatMessage> HandleAsync(
        RoutingDecision decision,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, _instructions),
            new(ChatRole.User, decision.OriginalInput)
        };

        var response = await _chatClient.GetResponseAsync(messages, cancellationToken: cancellationToken);

        return new ChatMessage(ChatRole.Assistant, response.Text);
    }
}
