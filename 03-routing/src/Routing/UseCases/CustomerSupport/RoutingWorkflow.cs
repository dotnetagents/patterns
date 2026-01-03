using Microsoft.Agents.AI.Workflows;

namespace DotNetAgents.Patterns.Routing.UseCases.CustomerSupport;

/// <summary>
/// Routing workflow that classifies tickets and routes to specialist agents
/// using the framework's native AddSwitch() for conditional routing.
/// </summary>
public static class RoutingWorkflow
{
    /// <summary>
    /// Creates the routing workflow with native AddSwitch() for conditional routing.
    /// </summary>
    /// <remarks>
    /// The workflow flow:
    /// 1. ClassifierExecutor analyzes the ticket and returns a RoutingDecision
    /// 2. AddSwitch() routes based on RoutingDecision.Category to the appropriate specialist
    /// 3. SpecialistExecutor handles the ticket with domain expertise
    /// </remarks>
    public static (Workflow, Dictionary<string, string>) Create(RoutingPipelineConfig config)
    {
        // Create classifier executor (classifies tickets into categories)
        var classifier = new ClassifierExecutor(config.Provider, config.ClassifierModel);

        // Create specialist executors for each category
        var billing = new SpecialistExecutor(config.Provider, config.SpecialistModel, SupportCategory.Billing);
        var technical = new SpecialistExecutor(config.Provider, config.SpecialistModel, SupportCategory.Technical);
        var account = new SpecialistExecutor(config.Provider, config.SpecialistModel, SupportCategory.Account);
        var product = new SpecialistExecutor(config.Provider, config.SpecialistModel, SupportCategory.Product);
        var general = new SpecialistExecutor(config.Provider, config.SpecialistModel, SupportCategory.General);

        // Build workflow with switch-case routing
        var builder = new WorkflowBuilder(classifier);

        builder.AddSwitch(classifier, switchBuilder =>
            switchBuilder
                .AddCase<RoutingDecision>(result => result!.Category == SupportCategory.Billing, billing)
                .AddCase<RoutingDecision>(result => result!.Category == SupportCategory.Technical, technical)
                .AddCase<RoutingDecision>(result => result!.Category == SupportCategory.Account, account)
                .AddCase<RoutingDecision>(result => result!.Category == SupportCategory.Product, product)
                .AddCase<RoutingDecision>(_ => true, general) // Default case
        );

        var workflow = builder.Build();

        var agentModels = new Dictionary<string, string>
        {
            ["Classifier"] = config.ClassifierModel,
            ["BillingSpecialist"] = config.SpecialistModel,
            ["TechnicalSpecialist"] = config.SpecialistModel,
            ["AccountSpecialist"] = config.SpecialistModel,
            ["ProductSpecialist"] = config.SpecialistModel,
            ["GeneralSpecialist"] = config.SpecialistModel,
        };

        return (workflow, agentModels);
    }
}
