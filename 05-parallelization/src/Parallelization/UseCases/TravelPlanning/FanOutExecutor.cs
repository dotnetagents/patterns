using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace DotNetAgents.Patterns.Parallelization.UseCases.TravelPlanning;

/// <summary>
/// Executor that broadcasts the incoming query to all connected parallel agents.
/// Part of the Fan-Out pattern in Microsoft Agent Framework.
/// Receives ChatMessage from WorkflowRunner and forwards it to all fan-out targets.
/// </summary>
internal sealed class FanOutExecutor() : Executor<ChatMessage>("FanOutExecutor")
{
    public override async ValueTask HandleAsync(
        ChatMessage message,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        // Broadcast the query to all connected agents (Hotels, Transport, Activities)
        await context.SendMessageAsync(message, cancellationToken);

        // Send turn token to kick off all parallel agents simultaneously
        await context.SendMessageAsync(new TurnToken(emitEvents: true), cancellationToken);
    }
}
