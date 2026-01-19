using System.ComponentModel;
using DotNetAgents.Patterns.Parallelization.Services;

namespace DotNetAgents.Patterns.Parallelization.Tools;

/// <summary>
/// Tool for performing web searches using Google Custom Search API.
/// Used by travel planning agents to research hotels, transportation, and activities.
/// </summary>
public class GoogleSearchTool(IGoogleSearchService searchService)
{
    [Description("Search the web for information using Google. Returns relevant search results with titles, descriptions, and links. Use this to find current information about hotels, flights, activities, restaurants, and travel recommendations.")]
    public async Task<string> Search(
        [Description("The search query to find relevant information")] string query,
        [Description("Maximum number of results to return (default: 5, max: 10)")] int maxResults = 5)
    {
        var results = await searchService.SearchAsync(query, Math.Min(maxResults, 10));

        if (results.Count == 0)
        {
            return "No search results found for the query.";
        }

        var formattedResults = results.Select((r, i) =>
            $"{i + 1}. **{r.Title}**\n   {r.Snippet}\n   Link: {r.Link}");

        return string.Join("\n\n", formattedResults);
    }
}
