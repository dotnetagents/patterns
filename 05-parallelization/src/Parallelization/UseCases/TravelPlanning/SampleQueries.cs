namespace DotNetAgents.Patterns.Parallelization.UseCases.TravelPlanning;

/// <summary>
/// Sample travel planning queries for benchmarks and testing.
/// </summary>
public static class SampleQueries
{
    /// <summary>
    /// Default query used for benchmarks.
    /// </summary>
    public const string DefaultBenchmarkQuery =
        "Plan a 7-day vacation to Barcelona, Spain in June 2025 for a family of 4 with kids aged 8 and 12. " +
        "We're interested in beaches, cultural attractions, and good food. Our budget is moderate.";

    /// <summary>
    /// Collection of sample queries for various travel scenarios.
    /// </summary>
    public static readonly (string Name, string Query)[] All =
    [
        ("Barcelona Family", DefaultBenchmarkQuery),

        ("Tokyo Solo",
            "Plan a 10-day trip to Tokyo, Japan in cherry blossom season (late March) for a solo traveler. " +
            "I'm interested in traditional culture, anime/gaming, and trying local cuisine. Budget is flexible."),

        ("Portugal Budget",
            "Plan a budget-friendly 5-day trip to Portugal for a solo traveler in September. " +
            "I want to see Lisbon and Porto. Prefer hostels and public transport. Maximum budget $800 total."),

        ("Paris Romantic",
            "Plan a romantic 4-day weekend getaway to Paris, France for a couple celebrating their anniversary. " +
            "We want luxury experiences, fine dining, and romantic activities. Budget is not a concern."),

        ("Thailand Adventure",
            "Plan a 2-week adventure trip to Thailand for 2 adults in November. " +
            "We want beaches, temples, and outdoor activities like snorkeling and hiking. Mix of comfort and budget stays."),
    ];
}
