using System.ComponentModel;
using DotNetAgents.Patterns.ToolUse.Models;
using DotNetAgents.Patterns.ToolUse.Services;

namespace DotNetAgents.Patterns.ToolUse.Tools;

public class CheckoutTool(OrderService orderService, CartService cartService, string customerId)
{
    [Description("Complete the checkout process and create an order from the current shopping cart. The cart will be cleared after successful checkout.")]
    public async Task<Order?> Checkout()
    {
        var cart = await cartService.GetCartAsync(customerId);

        if (cart == null || !cart.Items.Any())
            return null;

        var order = await orderService.CreateOrderAsync(customerId, cart);

        if (order != null)
            await cartService.ClearCartAsync(customerId);

        return order;
    }

    [Description("Get the order history showing all previous orders for the current customer.")]
    public async Task<IEnumerable<Order>> GetOrderHistory()
    {
        return await orderService.GetOrdersAsync(customerId);
    }
}
