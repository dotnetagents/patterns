using Microsoft.EntityFrameworkCore;
using DotNetAgents.Patterns.ToolUse.Data;
using DotNetAgents.Patterns.ToolUse.Models;

namespace DotNetAgents.Patterns.ToolUse.Services;

public record CartOperationResult(
    bool Success,
    string? ProductName = null,
    decimal CartTotal = 0,
    string? ErrorMessage = null);

public class CartService(ECommerceDbContext context)
{
    public async Task<ShoppingCart?> GetCartAsync(string customerId)
    {
        return await context.ShoppingCarts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .ThenInclude(p => p.Category)
            .FirstOrDefaultAsync(c => c.CustomerId == customerId);
    }

    public async Task<CartOperationResult> AddToCartAsync(string customerId, int productId, int quantity)
    {
        var product = await context.Products.FindAsync(productId);
        if (product == null)
            return new CartOperationResult(false, ErrorMessage: $"Product with ID {productId} not found.");

        if (product.Stock < quantity)
            return new CartOperationResult(false, ErrorMessage: $"Insufficient stock. Only {product.Stock} available.");

        var cart = await GetCartAsync(customerId);
        if (cart == null)
        {
            cart = new ShoppingCart { CustomerId = customerId };
            context.ShoppingCarts.Add(cart);
            await context.SaveChangesAsync();
            cart = await GetCartAsync(customerId);
        }

        var existingItem = cart!.Items.FirstOrDefault(i => i.ProductId == productId);
        if (existingItem != null)
        {
            existingItem.Quantity += quantity;
        }
        else
        {
            context.CartItems.Add(new CartItem
            {
                ShoppingCartId = cart.Id,
                ProductId = productId,
                Quantity = quantity
            });
        }

        await context.SaveChangesAsync();

        // Reload to get accurate total
        cart = await GetCartAsync(customerId);
        return new CartOperationResult(true, product.Name, cart?.Total ?? 0);
    }

    public async Task<CartOperationResult> RemoveFromCartAsync(string customerId, int productId, int? quantity)
    {
        var cart = await GetCartAsync(customerId);
        if (cart == null)
            return new CartOperationResult(false, ErrorMessage: "Cart not found.");

        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
        if (item == null)
            return new CartOperationResult(false, ErrorMessage: "Product not in cart.");

        var productName = item.Product.Name;

        if (quantity.HasValue && quantity.Value < item.Quantity)
        {
            item.Quantity -= quantity.Value;
        }
        else
        {
            context.CartItems.Remove(item);
        }

        await context.SaveChangesAsync();

        cart = await GetCartAsync(customerId);
        return new CartOperationResult(true, productName, cart?.Total ?? 0);
    }

    public async Task ClearCartAsync(string customerId)
    {
        var cart = await GetCartAsync(customerId);
        if (cart != null)
        {
            context.CartItems.RemoveRange(cart.Items);
            await context.SaveChangesAsync();
        }
    }
}
