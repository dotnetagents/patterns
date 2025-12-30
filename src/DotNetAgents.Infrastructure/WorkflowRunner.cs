using System.Text;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace DotNetAgents.Infrastructure;

public static class WorkflowRunner
{
    public static async Task<string> RunAsync(
        Workflow workflow,
        string prompt,
        CancellationToken cancellationToken = default
    )
    {
        var messages = new List<ChatMessage> { new(ChatRole.User, prompt) };

        var run = await InProcessExecution.StreamAsync(
            workflow,
            messages,
            cancellationToken: cancellationToken
        );
        await run.TrySendMessageAsync(new TurnToken(true));

        // Buffer output per agent, only keep the last agent's output
        string? currentAgent = null;
        var currentOutput = new StringBuilder();

        await foreach (var evt in run.WatchStreamAsync().WithCancellation(cancellationToken))
        {
            if (evt is AgentRunUpdateEvent updateEvent)
            {
                // When agent changes, reset buffer to only keep last agent's output
                if (currentAgent != updateEvent.ExecutorId)
                {
                    currentAgent = updateEvent.ExecutorId;
                    currentOutput.Clear();
                }
                currentOutput.Append(updateEvent.Data?.ToString() ?? string.Empty);
            }
            else if (evt is WorkflowOutputEvent)
            {
                break;
            }
        }

        return currentOutput.ToString();
    }
}
