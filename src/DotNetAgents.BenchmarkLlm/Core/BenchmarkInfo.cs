using System.Reflection;

namespace DotNetAgents.BenchmarkLlm.Core;

/// <summary>
/// Information about a discovered benchmark method.
/// </summary>
public sealed record BenchmarkInfo
{
    public required string Category { get; init; }
    public required string Name { get; init; }
    public required string Prompt { get; init; }
    public string? Description { get; init; }
    public bool IsBaseline { get; init; }
    public required MethodInfo Method { get; init; }
    public required Type DeclaringType { get; init; }

    public string FullName => $"{Category}.{Name}";
}
