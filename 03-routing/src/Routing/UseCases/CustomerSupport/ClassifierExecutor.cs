using System.Text.RegularExpressions;
using DotNetAgents.Infrastructure;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace DotNetAgents.Patterns.Routing.UseCases.CustomerSupport;

/// <summary>
/// Executor that classifies support tickets into categories using LLM.
/// Uses XML output format for reliable parsing.
/// </summary>
public sealed class ClassifierExecutor : Executor<ChatMessage, RoutingDecision>
{
    private readonly IChatClient _chatClient;

    public string Model { get; }

    private const string ClassifierInstructions = """
        You are a customer support triage specialist. Your job is to analyze incoming
        support tickets and classify them into the appropriate category.

        Available categories:
        - billing: Payment issues, charges, refunds, subscription changes, invoicing, pricing questions
        - technical: Software bugs, error messages, API issues, integration problems, performance issues
        - account: Password resets, account access, security concerns, profile changes, two-factor auth
        - product: Feature questions, product information, capabilities, usage guidance, feature requests

        Analyze the ticket carefully and determine the single best category.

        Respond in this exact XML format:
        <reasoning>
        [Your analysis of why this ticket belongs to a specific category]
        </reasoning>
        <selection>
        [category name in lowercase: billing, technical, account, or product]
        </selection>

        Only output the XML, nothing else.
        """;

    public ClassifierExecutor(string provider, string model)
        : base("Classifier")
    {
        _chatClient = ChatClientFactory.Create(provider, model);
        Model = model;
    }

    public override async ValueTask<RoutingDecision> HandleAsync(
        ChatMessage message,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, ClassifierInstructions),
            new(ChatRole.User, message.Text)
        };

        var response = await _chatClient.GetResponseAsync(messages, cancellationToken: cancellationToken);

        return ParseRoutingResponse(response.Text, message.Text);
    }

    private static RoutingDecision ParseRoutingResponse(string response, string originalInput)
    {
        var reasoning = ExtractXmlContent(response, "reasoning");
        var selection = ExtractXmlContent(response, "selection").Trim().ToLowerInvariant();

        var category = selection switch
        {
            "billing" => SupportCategory.Billing,
            "technical" => SupportCategory.Technical,
            "account" => SupportCategory.Account,
            "product" => SupportCategory.Product,
            _ => SupportCategory.General
        };

        return new RoutingDecision
        {
            Category = category,
            Reasoning = reasoning,
            OriginalInput = originalInput
        };
    }

    private static string ExtractXmlContent(string text, string tagName)
    {
        var pattern = $@"<{tagName}>\s*(.*?)\s*</{tagName}>";
        var match = Regex.Match(text, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

        return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
    }
}
