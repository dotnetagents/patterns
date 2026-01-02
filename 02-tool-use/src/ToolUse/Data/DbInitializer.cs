using DotNetAgents.Patterns.ToolUse.Models;

namespace DotNetAgents.Patterns.ToolUse.Data;

public static class DbInitializer
{
    public static void Initialize(ECommerceDbContext context)
    {
        if (context.Products.Any())
            return; // Already seeded

        // Categories
        var electronics = new Category { Name = "Electronics" };
        var clothing = new Category { Name = "Clothing" };
        var books = new Category { Name = "Books" };

        context.Categories.AddRange(electronics, clothing, books);
        context.SaveChanges();

        // Products (~10 items across 3 categories)
        var products = new List<Product>
        {
            // Electronics (4 items)
            new()
            {
                Name = "Wireless Headphones",
                Description = "Noise-cancelling Bluetooth headphones with 30hr battery",
                Price = 149.99m,
                CategoryId = electronics.Id,
                Stock = 50
            },
            new()
            {
                Name = "USB-C Hub",
                Description = "7-in-1 hub with HDMI, USB-A, SD card reader",
                Price = 49.99m,
                CategoryId = electronics.Id,
                Stock = 100
            },
            new()
            {
                Name = "Mechanical Keyboard",
                Description = "RGB backlit keyboard with Cherry MX switches",
                Price = 129.99m,
                CategoryId = electronics.Id,
                Stock = 30
            },
            new()
            {
                Name = "Portable Charger",
                Description = "20000mAh power bank with fast charging",
                Price = 39.99m,
                CategoryId = electronics.Id,
                Stock = 75
            },

            // Clothing (3 items)
            new()
            {
                Name = "Cotton T-Shirt",
                Description = "Premium cotton crew neck t-shirt, available in multiple colors",
                Price = 24.99m,
                CategoryId = clothing.Id,
                Stock = 200
            },
            new()
            {
                Name = "Denim Jeans",
                Description = "Classic fit denim jeans with stretch comfort",
                Price = 59.99m,
                CategoryId = clothing.Id,
                Stock = 80
            },
            new()
            {
                Name = "Running Shoes",
                Description = "Lightweight running shoes with cushioned sole",
                Price = 89.99m,
                CategoryId = clothing.Id,
                Stock = 45
            },

            // Books (3 items)
            new()
            {
                Name = "C# in Depth",
                Description = "Comprehensive guide to C# by Jon Skeet",
                Price = 44.99m,
                CategoryId = books.Id,
                Stock = 60
            },
            new()
            {
                Name = "Clean Code",
                Description = "A handbook of agile software craftsmanship by Robert C. Martin",
                Price = 39.99m,
                CategoryId = books.Id,
                Stock = 40
            },
            new()
            {
                Name = "Design Patterns",
                Description = "Elements of reusable object-oriented software by Gang of Four",
                Price = 54.99m,
                CategoryId = books.Id,
                Stock = 35
            },
        };

        context.Products.AddRange(products);
        context.SaveChanges();
    }
}
