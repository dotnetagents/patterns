using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotNetAgents.Patterns.Parallelization.Services;

/// <summary>
/// Real implementation of Google Custom Search API.
/// Requires GOOGLE_SEARCH_API_KEY and GOOGLE_SEARCH_ENGINE_ID environment variables.
/// HttpClient is injected via IHttpClientFactory for proper lifecycle management.
/// </summary>
public sealed class GoogleSearchService : IGoogleSearchService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _searchEngineId;
    private const string BaseUrl = "https://www.googleapis.com/customsearch/v1";

    public GoogleSearchService(HttpClient httpClient)
    {
        _httpClient = httpClient;

        _apiKey = Environment.GetEnvironmentVariable("GOOGLE_SEARCH_API_KEY")
            ?? throw new InvalidOperationException(
                "GOOGLE_SEARCH_API_KEY environment variable is not set. " +
                "Get an API key from https://developers.google.com/custom-search/v1/introduction");

        _searchEngineId = Environment.GetEnvironmentVariable("GOOGLE_SEARCH_ENGINE_ID")
            ?? throw new InvalidOperationException(
                "GOOGLE_SEARCH_ENGINE_ID environment variable is not set. " +
                "Create a search engine at https://programmablesearchengine.google.com/");
    }

    public async Task<IReadOnlyList<GoogleSearchResult>> SearchAsync(
        string query,
        int maxResults = 5,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        maxResults = Math.Clamp(maxResults, 1, 10);

        var url = $"{BaseUrl}?key={_apiKey}&cx={_searchEngineId}&q={Uri.EscapeDataString(query)}&num={maxResults}";

        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var searchResponse = await response.Content.ReadFromJsonAsync<GoogleSearchResponse>(
                cancellationToken: cancellationToken);

            if (searchResponse?.Items is null)
            {
                return [];
            }

            return searchResponse.Items
                .Select(item => new GoogleSearchResult
                {
                    Title = item.Title ?? "Untitled",
                    Snippet = item.Snippet ?? "",
                    Link = item.Link ?? ""
                })
                .ToList();
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Google Search API error: {ex.Message}");
            return [];
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Error parsing Google Search response: {ex.Message}");
            return [];
        }
    }

    private sealed class GoogleSearchResponse
    {
        [JsonPropertyName("items")]
        public List<GoogleSearchItem>? Items { get; set; }
    }

    private sealed class GoogleSearchItem
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("snippet")]
        public string? Snippet { get; set; }

        [JsonPropertyName("link")]
        public string? Link { get; set; }
    }
}
