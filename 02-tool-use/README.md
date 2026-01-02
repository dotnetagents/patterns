# Pattern 2: Tool Use

Demonstrates the Tool Use pattern where AI agents call external functions to interact with databases, APIs, and real-world systems.

## Scenario

An e-commerce shopping assistant that can:
- **Search products** by name, category, or price range
- **Manage shopping cart** (add, remove, view items)
- **Process checkout** and view order history

## Quick Start

```bash
# Interactive mode
dotnet run -- --model gpt-4o

# Run benchmarks
dotnet run -- --benchmark
```

## Tools Implemented

| Tool | Description |
|------|-------------|
| `SearchProducts` | Filter products by name, category, price range |
| `GetCategories` | List available product categories |
| `GetProductById` | Get details for a specific product |
| `AddToCart` | Add product to shopping cart |
| `RemoveFromCart` | Remove product from cart |
| `ViewCart` | View current cart contents |
| `ClearCart` | Empty the shopping cart |
| `Checkout` | Create order from cart |
| `GetOrderHistory` | View past orders |

## Project Structure

```
02-tool-use/src/ToolUse/
  Data/           - EF Core DbContext and seed data
  Models/         - Domain models (Product, Cart, Order)
  Services/       - Business logic layer
  Tools/          - AI tool definitions with [Description] attributes
  UseCases/       - Agent pipelines and benchmarks
```

## Learn More

See the full tutorial at [dotnetagents.net/tutorials/02-tool-use](https://dotnetagents.net/tutorials/02-tool-use)
