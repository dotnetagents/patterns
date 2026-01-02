using System.ComponentModel;
using DotNetAgents.Patterns.ToolUse.Models;
using DotNetAgents.Patterns.ToolUse.Services;

namespace DotNetAgents.Patterns.ToolUse.Tools;

public class ShoppingCartTool(CartService cartService, string customerId)
{
    [Description("Add a product to the shopping cart. Returns the result with updated cart total.")]
    public async Task<CartOperationResult> AddToCart(
        [Description("The product ID to add to cart")] int productId,
        [Description("Quantity to add (default: 1)")] int quantity = 1)
    {
        return await cartService.AddToCartAsync(customerId, productId, quantity);
    }

    [Description("Remove a product from the shopping cart. Can remove all or a specific quantity.")]
    public async Task<CartOperationResult> RemoveFromCart(
        [Description("The product ID to remove from cart")] int productId,
        [Description("Quantity to remove. If not specified or greater than quantity in cart, removes all.")] int? quantity = null)
    {
        return await cartService.RemoveFromCartAsync(customerId, productId, quantity);
    }

    [Description("View the current contents of the shopping cart including all items, quantities, and total.")]
    public async Task<ShoppingCart?> ViewCart()
    {
        return await cartService.GetCartAsync(customerId);
    }

    [Description("Clear all items from the shopping cart.")]
    public async Task ClearCart()
    {
        await cartService.ClearCartAsync(customerId);
    }
}
