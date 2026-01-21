using System.Text;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace DotNetAgents.Infrastructure;

public static class WorkflowRunner
{
    public static async Task<string> RunAsync(
        Workflow workflow,
        string prompt,
        string? keepExecutorIdOnly = null,
        CancellationToken cancellationToken = default
    )
    {
        var run = await InProcessExecution.StreamAsync(
            workflow,
            new ChatMessage(ChatRole.User, prompt),
            cancellationToken: cancellationToken
        );
        await run.TrySendMessageAsync(new TurnToken(true));

        // Buffer output per agent, only keep the last agent's output
        string? currentAgent = null;
        var currentOutput = new StringBuilder();

        // For Group Chat workflows, we track by author name
        // We keep both the current agent's output and the previous agent's output
        // This allows returning the content agent (e.g., PitchWriter) when a critic approves
        var agentOutputs = new Dictionary<string, StringBuilder>();
        string? lastGroupChatAuthor = null;

        await foreach (var evt in run.WatchStreamAsync().WithCancellation(cancellationToken))
        {
            if (evt is ExecutorCompletedEvent updateEvent)
            {
                // When agent changes, reset buffer to only keep last agent's output
                if (currentAgent != updateEvent.ExecutorId)
                {
                    currentAgent = updateEvent.ExecutorId;
                    currentOutput.Clear();
                }
                currentOutput.Append(updateEvent.Data?.ToString() ?? string.Empty);
            }
            else if (evt is AgentRunUpdateEvent agentUpdate)
            {
                // Handle Group Chat streaming output
                var text = agentUpdate.Update?.Text;
                var author = agentUpdate.Update?.AuthorName ?? "Unknown";

                if (!string.IsNullOrEmpty(text))
                {
                    // Track output per agent
                    if (!agentOutputs.TryGetValue(author, out var sb))
                    {
                        sb = new StringBuilder();
                        agentOutputs[author] = sb;
                    }

                    // If author changed, clear this agent's buffer (new turn)
                    if (lastGroupChatAuthor != author)
                    {
                        sb.Clear();
                        lastGroupChatAuthor = author;
                    }
                    sb.Append(text);
                }
            }
            else if (evt is WorkflowOutputEvent)
            {
                break;
            }
        }

        // For Group Chat: prefer content agent (PitchWriter) over critic (VCCritic)
        // Return the first non-critic agent's output if available
        if (agentOutputs.Count > 0)
        {
            // Try to find a content-producing agent (not named *Critic)
            var contentAgent = agentOutputs
                .Where(kv => !kv.Key.Contains("Critic", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(kv => kv.Value.Length)
                .FirstOrDefault();

            if (contentAgent.Value != null && contentAgent.Value.Length > 0)
            {
                return contentAgent.Value.ToString();
            }

            // Fallback to the last agent's output
            if (lastGroupChatAuthor != null && agentOutputs.TryGetValue(lastGroupChatAuthor, out var lastOutput))
            {
                return lastOutput.ToString();
            }
        }

        return currentOutput.ToString();
    }
}
