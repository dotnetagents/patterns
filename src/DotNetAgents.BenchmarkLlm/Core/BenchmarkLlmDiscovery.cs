using System.Reflection;
using System.Text.RegularExpressions;

namespace DotNetAgents.BenchmarkLlm.Core;

/// <summary>
/// Discovers benchmarks by scanning assemblies for attributed methods.
/// </summary>
public sealed class BenchmarkLlmDiscovery
{
    /// <summary>
    /// Discovers all benchmarks from loaded assemblies.
    /// </summary>
    public IReadOnlyList<BenchmarkInfo> DiscoverAll()
    {
        var benchmarks = new List<BenchmarkInfo>();

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                benchmarks.AddRange(DiscoverFromAssembly(assembly));
            }
            catch
            {
                // Skip assemblies that can't be scanned
            }
        }

        return benchmarks;
    }

    /// <summary>
    /// Discovers benchmarks from a specific assembly.
    /// </summary>
    public IReadOnlyList<BenchmarkInfo> DiscoverFromAssembly(Assembly assembly)
    {
        var benchmarks = new List<BenchmarkInfo>();

        try
        {
            foreach (var type in assembly.GetTypes())
            {
                var workflowAttr = type.GetCustomAttribute<WorkflowBenchmarkAttribute>();
                if (workflowAttr == null)
                    continue;

                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                {
                    var benchmarkAttr = method.GetCustomAttribute<BenchmarkLlmAttribute>();
                    if (benchmarkAttr == null)
                        continue;

                    benchmarks.Add(
                        new BenchmarkInfo
                        {
                            Category = workflowAttr.Category,
                            Name = benchmarkAttr.Name,
                            Prompt = workflowAttr.Prompt,
                            Description = benchmarkAttr.Description ?? workflowAttr.Description,
                            IsBaseline = benchmarkAttr.Baseline,
                            Method = method,
                            DeclaringType = type,
                        }
                    );
                }
            }
        }
        catch
        {
            // Skip types that can't be loaded
        }

        return benchmarks;
    }

    /// <summary>
    /// Filters benchmarks by glob pattern.
    /// </summary>
    public IReadOnlyList<BenchmarkInfo> Filter(
        IEnumerable<BenchmarkInfo> benchmarks,
        string? pattern
    )
    {
        if (string.IsNullOrEmpty(pattern) || pattern == "*")
            return benchmarks.ToList();

        var regex = GlobToRegex(pattern);
        return benchmarks.Where(b => regex.IsMatch(b.FullName)).ToList();
    }

    private static Regex GlobToRegex(string pattern)
    {
        var regexPattern =
            "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
        return new Regex(regexPattern, RegexOptions.IgnoreCase);
    }
}
