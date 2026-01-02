using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using DotNetAgents.Patterns.ToolUse.Data;
using DotNetAgents.Patterns.ToolUse.Models;

namespace DotNetAgents.Patterns.ToolUse.Services;

public class OrderService(ECommerceDbContext context)
{
    public async Task<Order?> CreateOrderAsync(string customerId, ShoppingCart cart)
    {
        var itemsSummary = cart.Items.Select(i => new
        {
            ProductId = i.ProductId,
            Name = i.Product.Name,
            Quantity = i.Quantity,
            Price = i.Product.Price
        });

        var order = new Order
        {
            CustomerId = customerId,
            Total = cart.Total,
            ItemsSummary = JsonSerializer.Serialize(itemsSummary),
            CreatedAt = DateTime.UtcNow
        };

        context.Orders.Add(order);
        await context.SaveChangesAsync();

        return order;
    }

    public async Task<IEnumerable<Order>> GetOrdersAsync(string customerId)
    {
        return await context.Orders
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }
}
