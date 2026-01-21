using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace DotNetAgents.Patterns.Reflection.UseCases.StartupPitch;

/// <summary>
/// Custom group chat manager that terminates when the critic approves the pitch.
/// Extends RoundRobinGroupChatManager to alternate between PitchWriter and VCCritic.
/// </summary>
public class ApprovalBasedGroupChatManager : RoundRobinGroupChatManager
{
    private readonly string _approverName;

    public ApprovalBasedGroupChatManager(
        IReadOnlyList<AIAgent> agents,
        string approverName)
        : base(agents)
    {
        _approverName = approverName;
    }

    protected override ValueTask<bool> ShouldTerminateAsync(
        IReadOnlyList<ChatMessage> history,
        CancellationToken cancellationToken = default)
    {
        var lastMessage = history.LastOrDefault();

        // Terminate if the approver says "APPROVED" (but not "NOT APPROVED" or "NEEDS_REVISION")
        bool shouldTerminate = lastMessage?.AuthorName == _approverName &&
            lastMessage.Text.Contains("APPROVED", StringComparison.OrdinalIgnoreCase) &&
            !lastMessage.Text.Contains("NOT APPROVED", StringComparison.OrdinalIgnoreCase) &&
            !lastMessage.Text.Contains("NEEDS_REVISION", StringComparison.OrdinalIgnoreCase);

        if (shouldTerminate)
        {
            Console.WriteLine($"\n[APPROVED after {history.Count} messages]");
        }

        return ValueTask.FromResult(shouldTerminate);
    }
}
