namespace DotNetAgents.BenchmarkLlm.Core;

/// <summary>
/// Configuration for a benchmark run.
/// </summary>
public sealed record BenchmarkLlmConfig
{
    public string? Filter { get; init; }
    public string? RunId { get; init; }
    public string ArtifactsPath { get; init; } = "./runs";
    public bool Evaluate { get; init; }
    public IReadOnlyList<string> Exporters { get; init; } = ["console"];
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the effective run ID, using the provided prompt for slug generation.
    /// </summary>
    public string GetEffectiveRunId(string prompt) =>
        RunId ?? $"{Timestamp:yyyy-MM-dd_HHmmss}_{Slugify(prompt)}";

    private static string Slugify(string text)
    {
        var slug = text.ToLowerInvariant().Replace(" ", "-").Replace("_", "-");

        // Keep only alphanumeric and hyphens
        slug = new string(slug.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());

        // Limit length
        if (slug.Length > 30)
            slug = slug[..30];

        return slug.Trim('-');
    }
}
