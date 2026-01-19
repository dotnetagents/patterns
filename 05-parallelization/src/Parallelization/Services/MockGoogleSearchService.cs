namespace DotNetAgents.Patterns.Parallelization.Services;

/// <summary>
/// Mock implementation of Google Search for benchmarks and testing.
/// Returns realistic but static results based on query keywords.
/// </summary>
public sealed class MockGoogleSearchService : IGoogleSearchService
{
    public Task<IReadOnlyList<GoogleSearchResult>> SearchAsync(
        string query,
        int maxResults = 5,
        CancellationToken cancellationToken = default)
    {
        var queryLower = query.ToLowerInvariant();
        var results = new List<GoogleSearchResult>();

        // Hotel-related queries
        if (queryLower.Contains("hotel") || queryLower.Contains("accommodation") || queryLower.Contains("stay"))
        {
            results.AddRange(GetHotelResults(query));
        }
        // Flight/transport-related queries
        else if (queryLower.Contains("flight") || queryLower.Contains("transport") ||
                 queryLower.Contains("train") || queryLower.Contains("travel to"))
        {
            results.AddRange(GetTransportResults(query));
        }
        // Activity-related queries
        else if (queryLower.Contains("activity") || queryLower.Contains("attraction") ||
                 queryLower.Contains("things to do") || queryLower.Contains("restaurant") ||
                 queryLower.Contains("visit") || queryLower.Contains("tour"))
        {
            results.AddRange(GetActivityResults(query));
        }
        // Generic travel queries
        else
        {
            results.AddRange(GetGenericTravelResults(query));
        }

        return Task.FromResult<IReadOnlyList<GoogleSearchResult>>(
            results.Take(maxResults).ToList());
    }

    private static IEnumerable<GoogleSearchResult> GetHotelResults(string query)
    {
        var destination = ExtractDestination(query);

        return
        [
            new GoogleSearchResult
            {
                Title = $"Top 10 Hotels in {destination} - TripAdvisor",
                Snippet = $"Find and compare the best hotels in {destination}. Read reviews, see photos, and book with confidence. Prices from $89/night.",
                Link = $"https://www.tripadvisor.com/Hotels-{destination.Replace(" ", "_")}"
            },
            new GoogleSearchResult
            {
                Title = $"Best Family Hotels in {destination} 2025 - Booking.com",
                Snippet = $"Family-friendly hotels in {destination} with pools, kid's clubs, and spacious rooms. Free cancellation available on most rooms.",
                Link = $"https://www.booking.com/family-hotels-{destination.Replace(" ", "-").ToLower()}"
            },
            new GoogleSearchResult
            {
                Title = $"Luxury Hotels & Resorts in {destination} - Hotels.com",
                Snippet = $"Discover premium accommodations in {destination}. 5-star hotels with spas, fine dining, and stunning views. Member prices available.",
                Link = $"https://www.hotels.com/{destination.Replace(" ", "-").ToLower()}-luxury"
            },
            new GoogleSearchResult
            {
                Title = $"Budget-Friendly Hotels in {destination} - Expedia",
                Snippet = $"Save on your {destination} trip with affordable hotel options. Clean, comfortable stays from $45/night. Bundle and save more.",
                Link = $"https://www.expedia.com/Hotels-{destination.Replace(" ", "-")}"
            },
            new GoogleSearchResult
            {
                Title = $"{destination} Hotel Guide - Lonely Planet",
                Snippet = $"Expert recommendations for where to stay in {destination}. From boutique hotels to budget hostels, find your perfect accommodation.",
                Link = $"https://www.lonelyplanet.com/{destination.Replace(" ", "-").ToLower()}/hotels"
            }
        ];
    }

    private static IEnumerable<GoogleSearchResult> GetTransportResults(string query)
    {
        var destination = ExtractDestination(query);

        return
        [
            new GoogleSearchResult
            {
                Title = $"Cheap Flights to {destination} - Skyscanner",
                Snippet = $"Compare flights to {destination} from all major airlines. Find the best deals with flexible dates. Prices from $299 round trip.",
                Link = $"https://www.skyscanner.com/flights-to-{destination.Replace(" ", "-").ToLower()}"
            },
            new GoogleSearchResult
            {
                Title = $"Flights to {destination} - Google Flights",
                Snippet = $"Track prices for flights to {destination}. Get alerts when prices drop. Compare airlines and find the best route for your trip.",
                Link = $"https://www.google.com/flights?q=flights+to+{destination.Replace(" ", "+")}"
            },
            new GoogleSearchResult
            {
                Title = $"{destination} Airport Guide - Complete Transfer Options",
                Snippet = $"Getting from {destination} airport to city center. Taxis, buses, trains, and private transfers. Average journey time: 30-45 minutes.",
                Link = $"https://www.airport-{destination.Replace(" ", "").ToLower()}.com/transfers"
            },
            new GoogleSearchResult
            {
                Title = $"Train Travel to {destination} - Trainline",
                Snippet = $"Book train tickets to {destination}. High-speed rail options available. Save up to 61% when you book in advance.",
                Link = $"https://www.thetrainline.com/destinations/{destination.Replace(" ", "-").ToLower()}"
            },
            new GoogleSearchResult
            {
                Title = $"Best Airlines Flying to {destination} 2025",
                Snippet = $"Ranked list of airlines with routes to {destination}. Reviews, amenities, and on-time performance compared.",
                Link = $"https://www.airlinequality.com/flights-to-{destination.Replace(" ", "-").ToLower()}"
            }
        ];
    }

    private static IEnumerable<GoogleSearchResult> GetActivityResults(string query)
    {
        var destination = ExtractDestination(query);

        return
        [
            new GoogleSearchResult
            {
                Title = $"Top 25 Things to Do in {destination} - TripAdvisor",
                Snippet = $"The best attractions and activities in {destination}. Tours, museums, outdoor adventures, and hidden gems. Ranked by traveler reviews.",
                Link = $"https://www.tripadvisor.com/Attractions-{destination.Replace(" ", "_")}"
            },
            new GoogleSearchResult
            {
                Title = $"{destination} Travel Guide - Lonely Planet",
                Snippet = $"Expert tips for visiting {destination}. Best neighborhoods, restaurants, nightlife, and cultural experiences. Updated for 2025.",
                Link = $"https://www.lonelyplanet.com/{destination.Replace(" ", "-").ToLower()}"
            },
            new GoogleSearchResult
            {
                Title = $"Best Restaurants in {destination} - Michelin Guide",
                Snippet = $"Discover top-rated restaurants in {destination}. From street food to fine dining. Michelin-starred options and local favorites.",
                Link = $"https://guide.michelin.com/en/{destination.Replace(" ", "-").ToLower()}/restaurants"
            },
            new GoogleSearchResult
            {
                Title = $"Family Activities in {destination} - Viator",
                Snippet = $"Book family-friendly tours and activities in {destination}. Skip-the-line tickets, guided tours, and unique experiences for all ages.",
                Link = $"https://www.viator.com/{destination.Replace(" ", "-")}-family-tours"
            },
            new GoogleSearchResult
            {
                Title = $"{destination} Walking Tours & Day Trips - GetYourGuide",
                Snippet = $"Explore {destination} with local guides. Walking tours, food tours, and day trips to nearby attractions. Free cancellation up to 24h.",
                Link = $"https://www.getyourguide.com/{destination.Replace(" ", "-").ToLower()}"
            }
        ];
    }

    private static IEnumerable<GoogleSearchResult> GetGenericTravelResults(string query)
    {
        var destination = ExtractDestination(query);

        return
        [
            new GoogleSearchResult
            {
                Title = $"Complete {destination} Travel Guide 2025",
                Snippet = $"Everything you need to know about visiting {destination}. Best time to visit, what to pack, visa requirements, and local tips.",
                Link = $"https://www.lonelyplanet.com/{destination.Replace(" ", "-").ToLower()}"
            },
            new GoogleSearchResult
            {
                Title = $"Plan Your Trip to {destination} - TripAdvisor",
                Snippet = $"Hotels, flights, and things to do in {destination}. Read reviews from millions of travelers and plan your perfect trip.",
                Link = $"https://www.tripadvisor.com/{destination.Replace(" ", "_")}"
            },
            new GoogleSearchResult
            {
                Title = $"{destination} Tourism Official Website",
                Snippet = $"Official tourism board for {destination}. Events, attractions, maps, and travel resources. Plan your visit with insider knowledge.",
                Link = $"https://www.visit{destination.Replace(" ", "").ToLower()}.com"
            }
        ];
    }

    private static string ExtractDestination(string query)
    {
        // Common destinations that might appear in queries
        string[] knownDestinations =
        [
            "Barcelona", "Spain", "Tokyo", "Japan", "Paris", "France",
            "London", "Portugal", "Italy", "Rome", "New York", "Thailand",
            "Bali", "Greece", "Amsterdam", "Berlin", "Dubai", "Singapore"
        ];

        foreach (var dest in knownDestinations)
        {
            if (query.Contains(dest, StringComparison.OrdinalIgnoreCase))
            {
                return dest;
            }
        }

        // Default to a generic destination
        return "your destination";
    }
}
