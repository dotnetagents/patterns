using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace DotNetAgents.Infrastructure;

/// <summary>
/// Custom executor that wraps an IChatClient and ensures
/// explicit message passing for providers like Ollama.
/// </summary>
public sealed class AgentExecutor : Executor<ChatMessage, ChatMessage>
{
    private readonly ChatClientAgent _chatClientAgent;

    public string Model { get; init; }
    public string Name { get; init; }

    public AgentExecutor(AgentConfig config) : base(config.Name)
    {
        _chatClientAgent = new ChatClientAgent(
            ChatClientFactory.Create(config.Provider, config.Model),
            config.Instructions,
            config.Name);
        Model = config.Model;
        Name = config.Name;
    }
    
    public override async ValueTask<ChatMessage> HandleAsync(ChatMessage message, IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        var response = await _chatClientAgent.RunAsync(new ChatMessage(ChatRole.User, message.Text),
            cancellationToken: cancellationToken);
        return new ChatMessage(ChatRole.Assistant, response.Text);
    }
}