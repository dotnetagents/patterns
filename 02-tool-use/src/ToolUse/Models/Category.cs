using System.Text.Json.Serialization;

namespace DotNetAgents.Patterns.ToolUse.Models;

public class Category
{
    public int Id { get; set; }
    public required string Name { get; set; }

    [JsonIgnore]
    public ICollection<Product> Products { get; set; } = [];
}
