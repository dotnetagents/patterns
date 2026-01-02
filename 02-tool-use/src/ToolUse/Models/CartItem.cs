using System.Text.Json.Serialization;

namespace DotNetAgents.Patterns.ToolUse.Models;

public class CartItem
{
    public int Id { get; set; }
    public int ShoppingCartId { get; set; }

    [JsonIgnore]
    public ShoppingCart ShoppingCart { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
}
