namespace DotNetAgents.BenchmarkLlm.Core;

/// <summary>
/// Marks a class as containing LLM workflow benchmarks.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class WorkflowBenchmarkAttribute : Attribute
{
    public string Category { get; }
    public required string Prompt { get; set; }
    public string? Description { get; set; }

    public WorkflowBenchmarkAttribute(string category)
    {
        Category = category;
    }
}
