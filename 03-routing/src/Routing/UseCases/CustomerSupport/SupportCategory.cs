namespace DotNetAgents.Patterns.Routing.UseCases.CustomerSupport;

/// <summary>
/// Categories for customer support ticket routing.
/// </summary>
public enum SupportCategory
{
    /// <summary>
    /// Billing and payment related issues.
    /// </summary>
    Billing,

    /// <summary>
    /// Technical support and troubleshooting.
    /// </summary>
    Technical,

    /// <summary>
    /// Account security and access issues.
    /// </summary>
    Account,

    /// <summary>
    /// Product information and features.
    /// </summary>
    Product,

    /// <summary>
    /// General inquiries that don't fit other categories.
    /// </summary>
    General
}

/// <summary>
/// Result of the routing classification containing the category and reasoning.
/// </summary>
public sealed record RoutingDecision
{
    /// <summary>
    /// The classified category for the support ticket.
    /// </summary>
    public required SupportCategory Category { get; init; }

    /// <summary>
    /// The reasoning behind the classification decision.
    /// </summary>
    public required string Reasoning { get; init; }

    /// <summary>
    /// The original input ticket that was classified.
    /// </summary>
    public required string OriginalInput { get; init; }
}
