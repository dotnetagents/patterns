using DotNetAgents.Infrastructure;
using Microsoft.Agents.AI.Workflows;

namespace DotNetAgents.Patterns.Routing.UseCases.CustomerSupport;

/// <summary>
/// Baseline pipeline using a single generic support agent.
/// Used for comparison against the routing approach.
/// </summary>
public static class SingleAgentSupportPipeline
{
    private const string GenericSupportInstructions = """
        You are a customer support representative for a software company.
        You handle all types of customer inquiries including:

        - Billing: Payment issues, refunds, subscriptions, pricing
        - Technical: Bugs, errors, integrations, performance, APIs
        - Account: Password resets, security, 2FA, profile settings
        - Product: Features, capabilities, usage guidance

        Guidelines:
        - Identify the type of issue first
        - Provide clear, actionable solutions
        - Be professional and empathetic
        - Escalate complex issues appropriately

        Respond helpfully to whatever the customer needs.
        """;

    /// <summary>
    /// Creates a single-agent support pipeline.
    /// </summary>
    public static (Workflow, Dictionary<string, string>) Create(SingleAgentSupportConfig config)
    {
        var agent = new AgentExecutor(
            new AgentConfig
            {
                Name = "GenericSupport",
                Provider = config.Provider,
                Model = config.Model,
                Instructions = GenericSupportInstructions,
            }
        );

        var workflow = new WorkflowBuilder(agent).Build();

        var agentModels = new Dictionary<string, string> { ["GenericSupport"] = config.Model };

        return (workflow, agentModels);
    }
}
