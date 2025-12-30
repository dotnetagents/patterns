using DotNetAgents.BenchmarkLlm.Core;

namespace DotNetAgents.BenchmarkLlm.Export;

/// <summary>
/// Interface for exporting benchmark results.
/// </summary>
public interface IResultExporter
{
    string Name { get; }
    Task ExportAsync(
        IReadOnlyList<BenchmarkLlmResult> results,
        BenchmarkLlmConfig config,
        string outputPath
    );
}
