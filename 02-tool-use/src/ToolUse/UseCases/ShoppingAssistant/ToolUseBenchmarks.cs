using DotNetAgents.BenchmarkLlm.Core;
using DotNetAgents.Patterns.ToolUse.Data;
using DotNetAgents.Patterns.ToolUse.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;

namespace DotNetAgents.Patterns.ToolUse.UseCases.ShoppingAssistant;

[WorkflowBenchmark(
    "tool-use",
    Prompt = "Search for C# books in the shop, if you find one under $50 then buy it.",
    Description = "E-commerce shopping assistant demonstrating tool use pattern with multi-step reasoning"
)]
public class ToolUseBenchmarks
{
    [BenchmarkLlm(
        "gpt-4.1",
        Baseline = true,
        Description = "GPT-4.1: Agent with product search, cart, and checkout tools"
    )]
    public async Task<BenchmarkOutput> Gpt41(string prompt)
    {
        return await RunBenchmark(prompt, "gpt-4.1");
    }

    [BenchmarkLlm(
        "gpt-4o-mini",
        Baseline = true,
        Description = "GPT-4o-mini: Agent with product search, cart, and checkout tools"
    )]
    public async Task<BenchmarkOutput> Gpt4oMini(string prompt)
    {
        return await RunBenchmark(prompt, "gpt-4o-mini");
    }

    private static async Task<BenchmarkOutput> RunBenchmark(string prompt, string model)
    {
        // Setup in-memory database for benchmark isolation
        var options = new DbContextOptionsBuilder<ECommerceDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        await using var context = new ECommerceDbContext(options);
        await context.Database.OpenConnectionAsync();
        await context.Database.EnsureCreatedAsync();
        DbInitializer.Initialize(context);

        var productService = new ProductService(context);
        var cartService = new CartService(context);
        var orderService = new OrderService(context);
        
        var (client, chatOptions, agentModels) = ShoppingAgentPipeline.Create(
            new ShoppingAgentPipelineConfig
            {
                Provider = "azure",
                Model = model,
                ProductService = productService,
                CartService = cartService,
                OrderService = orderService,
            }
        );

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, ShoppingAgentPipeline.GetSystemPrompt()),
            new(ChatRole.User, prompt)
        };

        var response = await client.GetResponseAsync(messages, chatOptions);

        // Verify that an order with the C# book was created
        var orders = await context.Orders.ToListAsync();
        var csharpBookOrder = orders.FirstOrDefault(o =>
            o.ItemsSummary.Contains("C# in Depth", StringComparison.OrdinalIgnoreCase));

        var verificationResult = csharpBookOrder != null
            ? $"\n\n✓ Order #{csharpBookOrder.Id} verified: C# in Depth purchased for ${csharpBookOrder.Total:F2}"
            : "\n\n✗ VERIFICATION FAILED: No order with 'C# in Depth' was created";

        return BenchmarkOutput.WithModels(response.Text + verificationResult, agentModels);
    }
}
