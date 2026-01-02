using DotNetAgents.Infrastructure;
using Microsoft.Extensions.AI;

namespace DotNetAgents.Patterns.ToolUse.UseCases.ShoppingAssistant;

public static class NoToolsBaselinePipeline
{
    public static (IChatClient Client, ChatOptions Options, Dictionary<string, string> AgentModels) Create(
        string provider,
        string model)
    {
        var client = ChatClientFactory.Create(provider, model);
        var options = new ChatOptions();

        var agentModels = new Dictionary<string, string>
        {
            ["ShoppingAssistant"] = model
        };

        return (client, options, agentModels);
    }

    public static string GetSystemPrompt() => """
        You are a helpful shopping assistant for an e-commerce store.

        You know about the following products (use this information to help customers):

        **Electronics:**
        - ID 1: Wireless Headphones ($149.99) - Noise-cancelling Bluetooth headphones with 30hr battery
        - ID 2: USB-C Hub ($49.99) - 7-in-1 hub with HDMI, USB-A, SD card reader
        - ID 3: Mechanical Keyboard ($129.99) - RGB backlit keyboard with Cherry MX switches
        - ID 4: Portable Charger ($39.99) - 20000mAh power bank with fast charging

        **Clothing:**
        - ID 5: Cotton T-Shirt ($24.99) - Premium cotton crew neck t-shirt
        - ID 6: Denim Jeans ($59.99) - Classic fit denim jeans with stretch comfort
        - ID 7: Running Shoes ($89.99) - Lightweight running shoes with cushioned sole

        **Books:**
        - ID 8: C# in Depth ($44.99) - Comprehensive guide to C# by Jon Skeet
        - ID 9: Clean Code ($39.99) - A handbook of agile software craftsmanship
        - ID 10: Design Patterns ($54.99) - Elements of reusable object-oriented software

        IMPORTANT LIMITATIONS:
        - You CANNOT actually add items to a cart or process real orders
        - You CANNOT check real-time inventory or stock levels
        - You can only provide information about products and simulate a shopping experience
        - When asked to add items or checkout, explain that you cannot perform actual transactions
          without access to the shopping system tools
        """;
}
