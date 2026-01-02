using Microsoft.EntityFrameworkCore;
using DotNetAgents.Patterns.ToolUse.Data;
using DotNetAgents.Patterns.ToolUse.Models;

namespace DotNetAgents.Patterns.ToolUse.Services;

public class ProductService(ECommerceDbContext context)
{
    public async Task<IEnumerable<Product>> SearchProductsAsync(
        string? searchTerm = null,
        string? category = null,
        decimal? minPrice = null,
        decimal? maxPrice = null)
    {
        var query = context.Products.Include(p => p.Category).AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(term) ||
                p.Description.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(p =>
                p.Category.Name.ToLower() == category.ToLower());
        }

        if (minPrice.HasValue)
            query = query.Where(p => p.Price >= minPrice.Value);

        if (maxPrice.HasValue)
            query = query.Where(p => p.Price <= maxPrice.Value);

        return await query.ToListAsync();
    }

    public async Task<Product?> GetProductByIdAsync(int productId)
    {
        return await context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == productId);
    }

    public async Task<IEnumerable<Category>> GetCategoriesAsync()
    {
        return await context.Categories.ToListAsync();
    }
}
