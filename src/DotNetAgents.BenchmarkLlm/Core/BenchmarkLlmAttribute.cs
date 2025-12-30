namespace DotNetAgents.BenchmarkLlm.Core;

/// <summary>
/// Marks a method as an LLM benchmark.
/// The method should have signature: Task&lt;string&gt; MethodName(string topic, IChatClient client)
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class BenchmarkLlmAttribute : Attribute
{
    public string Name { get; }
    public string? Description { get; set; }
    public bool Baseline { get; set; }

    public BenchmarkLlmAttribute(string name)
    {
        Name = name;
    }
}
