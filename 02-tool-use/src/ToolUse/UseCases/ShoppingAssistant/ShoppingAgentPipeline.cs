using DotNetAgents.Infrastructure;
using DotNetAgents.Patterns.ToolUse.Services;
using DotNetAgents.Patterns.ToolUse.Tools;
using Microsoft.Extensions.AI;

namespace DotNetAgents.Patterns.ToolUse.UseCases.ShoppingAssistant;

public sealed class ShoppingAgentPipelineConfig
{
    public required string Provider { get; init; }
    public required string Model { get; init; }
    public required ProductService ProductService { get; init; }
    public required CartService CartService { get; init; }
    public required OrderService OrderService { get; init; }
    public string CustomerId { get; init; } = "customer-001";
}

public static class ShoppingAgentPipeline
{
    public static (IChatClient Client, ChatOptions Options, Dictionary<string, string> AgentModels) Create(
        ShoppingAgentPipelineConfig config)
    {
        // Create tool instances with dependencies
        var productTool = new ProductFilteringTool(config.ProductService);
        var cartTool = new ShoppingCartTool(config.CartService, config.CustomerId);
        var checkoutTool = new CheckoutTool(config.OrderService, config.CartService, config.CustomerId);

        // Create tool definitions using AIFunctionFactory
        var tools = new List<AITool>
        {
            // Product Tools
            AIFunctionFactory.Create(productTool.SearchProducts),
            AIFunctionFactory.Create(productTool.GetCategories),
            AIFunctionFactory.Create(productTool.GetProductById),

            // Cart Tools
            AIFunctionFactory.Create(cartTool.AddToCart),
            AIFunctionFactory.Create(cartTool.RemoveFromCart),
            AIFunctionFactory.Create(cartTool.ViewCart),
            AIFunctionFactory.Create(cartTool.ClearCart),

            // Checkout Tools
            AIFunctionFactory.Create(checkoutTool.Checkout),
            AIFunctionFactory.Create(checkoutTool.GetOrderHistory),
        };

        // Create chat client with function invocation
        var baseClient = ChatClientFactory.Create(config.Provider, config.Model);
        var client = new ChatClientBuilder(baseClient)
            .UseFunctionInvocation()
            .Build();

        var options = new ChatOptions
        {
            Tools = tools
        };

        var agentModels = new Dictionary<string, string>
        {
            ["ShoppingAssistant"] = config.Model
        };

        return (client, options, agentModels);
    }

    public static string GetSystemPrompt() => """
        You are a helpful shopping assistant for an e-commerce store.

        You have access to the following capabilities:
        1. **Product Search**: Search and filter products by name, category, or price range
        2. **Shopping Cart**: Add items to cart, remove items, view cart contents, clear cart
        3. **Checkout**: Complete orders and view order history

        Guidelines:
        - Always search for products before recommending them to ensure accurate, up-to-date information
        - When showing products, include the product ID so customers can easily reference them
        - Act immediately on user requests - do NOT ask for confirmation before adding to cart or checking out
        - If the user asks to buy something, search for it, add it to cart, and complete checkout in one go
        - Be helpful, concise, and friendly

        Available categories: Electronics, Clothing, Books
        """;
}
