using System.ComponentModel;
using DotNetAgents.Patterns.ToolUse.Models;
using DotNetAgents.Patterns.ToolUse.Services;

namespace DotNetAgents.Patterns.ToolUse.Tools;

public class ProductFilteringTool(ProductService productService)
{
    [Description("Search and filter products by name, category, or price range. Returns a list of matching products with their details including ID, name, description, price, category, and stock status.")]
    public async Task<IEnumerable<Product>> SearchProducts(
        [Description("Optional search term to filter by product name or description")] string? searchTerm = null,
        [Description("Optional category name to filter by (e.g., 'Electronics', 'Clothing', 'Books')")] string? category = null,
        [Description("Optional minimum price filter")] decimal? minPrice = null,
        [Description("Optional maximum price filter")] decimal? maxPrice = null)
    {
        return await productService.SearchProductsAsync(searchTerm, category, minPrice, maxPrice);
    }

    [Description("Get all available product categories in the store.")]
    public async Task<IEnumerable<Category>> GetCategories()
    {
        return await productService.GetCategoriesAsync();
    }

    [Description("Get detailed information for a specific product by its ID.")]
    public async Task<Product?> GetProductById(
        [Description("The product ID to look up")] int productId)
    {
        return await productService.GetProductByIdAsync(productId);
    }
}
