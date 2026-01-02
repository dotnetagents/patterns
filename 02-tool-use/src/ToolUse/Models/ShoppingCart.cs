namespace DotNetAgents.Patterns.ToolUse.Models;

public class ShoppingCart
{
    public int Id { get; set; }
    public required string CustomerId { get; set; }
    public ICollection<CartItem> Items { get; set; } = [];
    public decimal Total => Items.Sum(i => i.Product.Price * i.Quantity);
}
