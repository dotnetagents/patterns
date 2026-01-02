namespace DotNetAgents.Patterns.ToolUse.Models;

public class Order
{
    public int Id { get; set; }
    public required string CustomerId { get; set; }
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public required string ItemsSummary { get; set; }
}
