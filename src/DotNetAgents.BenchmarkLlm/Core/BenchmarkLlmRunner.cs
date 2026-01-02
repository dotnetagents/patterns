using DotNetAgents.BenchmarkLlm.Evaluation;
using DotNetAgents.BenchmarkLlm.Metrics;

namespace DotNetAgents.BenchmarkLlm.Core;

/// <summary>
/// Coordinates benchmark execution using pluggable strategies.
/// </summary>
public sealed class BenchmarkLlmRunner
{
    private readonly DefaultBenchmarkExecutionStrategy _strategy;
    private readonly IContentEvaluator? _evaluator;
    private readonly bool _verbose;

    public BenchmarkLlmRunner(
        IContentEvaluator? evaluator = null,
        bool verbose = true)
    {
        _evaluator = evaluator;
        _verbose = verbose;
        _strategy = new DefaultBenchmarkExecutionStrategy();
    }

    /// <summary>
    /// Runs all specified benchmarks.
    /// </summary>
    public async Task<IReadOnlyList<BenchmarkLlmResult>> RunAsync(
        IEnumerable<BenchmarkInfo> benchmarks,
        BenchmarkLlmConfig config,
        CancellationToken cancellationToken = default)
    {
        var benchmarkList = benchmarks.ToList();
        var results = new List<BenchmarkLlmResult>();
        var context = CreateContext(config);

        Trace($"Starting benchmark run: {benchmarkList.Count} benchmarks");

        foreach (var benchmark in benchmarkList)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Console.WriteLine($"Running {benchmark.FullName}...");

            try
            {
                var result = await _strategy.ExecuteAsync(benchmark, context, cancellationToken);
                results.Add(result);
                LogResult(result);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("  Cancelled");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  CRITICAL ERROR: {ex.Message}");
                Trace($"Stack trace: {ex.StackTrace}");
                results.Add(new BenchmarkLlmResult
                {
                    Category = benchmark.Category,
                    BenchmarkName = benchmark.Name,
                    Prompt = benchmark.Prompt,
                    Success = false,
                    Error = ex.Message,
                    ErrorDetails = ex.StackTrace,
                    Duration = TimeSpan.Zero,
                    Metrics = AggregatedMetrics.Empty,
                    IsBaseline = benchmark.IsBaseline,
                });
            }
        }

        Trace($"Benchmark run complete: {results.Count(r => r.Success)}/{results.Count} succeeded");
        return results;
    }

    private BenchmarkExecutionContext CreateContext(BenchmarkLlmConfig config) => new()
    {
        Evaluate = config.Evaluate,
        Evaluator = _evaluator,
        TraceOutput = _verbose ? Trace : null,
    };

    private static void LogResult(BenchmarkLlmResult result)
    {
        if (result.Success)
        {
            var metricsInfo = result.Metrics.TotalCalls > 0
                ? $"{result.Metrics.TotalTokens} tokens, {result.Metrics.TotalCalls} calls"
                : "no metrics";
            Console.WriteLine($"  Completed in {result.Duration.TotalSeconds:F1}s, {metricsInfo}");

            if (result.AgentModels?.Count > 0)
            {
                Console.WriteLine(
                    $"  Agents: {string.Join(", ", result.AgentModels.Select(kv => $"{kv.Key}={kv.Value}"))}");
            }
        }
        else
        {
            Console.WriteLine($"  FAILED: {result.Error}");
            if (result.ErrorDetails != null)
            {
                Console.WriteLine($"  Details: {result.ErrorDetails}");
            }
        }
    }

    private void Trace(string message)
    {
        if (_verbose)
        {
            Console.WriteLine($"  [TRACE] {message}");
        }
    }
}
