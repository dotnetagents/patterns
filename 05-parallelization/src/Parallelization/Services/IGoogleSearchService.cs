namespace DotNetAgents.Patterns.Parallelization.Services;

/// <summary>
/// Represents a single search result from Google Custom Search API.
/// </summary>
public record GoogleSearchResult
{
    public required string Title { get; init; }
    public required string Snippet { get; init; }
    public required string Link { get; init; }
}

/// <summary>
/// Interface for Google search functionality.
/// Allows switching between real API and mock implementations.
/// </summary>
public interface IGoogleSearchService
{
    /// <summary>
    /// Searches the web using Google Custom Search API.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="maxResults">Maximum number of results to return (default: 5, max: 10).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of search results.</returns>
    Task<IReadOnlyList<GoogleSearchResult>> SearchAsync(
        string query,
        int maxResults = 5,
        CancellationToken cancellationToken = default);
}
