using System.Diagnostics;
using System.Reflection;
using DotNetAgents.BenchmarkLlm.Evaluation;
using DotNetAgents.BenchmarkLlm.Metrics;
using DotNetAgents.Infrastructure;
using Microsoft.Extensions.AI;

namespace DotNetAgents.BenchmarkLlm.Core;

/// <summary>
/// Orchestrates benchmark execution with detailed tracing and error handling.
/// </summary>
public sealed class BenchmarkLlmRunner
{
    private readonly IContentEvaluator? _evaluator;
    private readonly bool _verbose;

    public BenchmarkLlmRunner(IContentEvaluator? evaluator = null, bool verbose = true)
    {
        _evaluator = evaluator;
        _verbose = verbose;
    }

    /// <summary>
    /// Runs all specified benchmarks with fail-fast behavior on critical errors.
    /// </summary>
    public async Task<IReadOnlyList<BenchmarkLlmResult>> RunAsync(
        IEnumerable<BenchmarkInfo> benchmarks,
        BenchmarkLlmConfig config,
        CancellationToken cancellationToken = default
    )
    {
        var benchmarkList = benchmarks.ToList();
        var results = new List<BenchmarkLlmResult>();

        Trace($"Starting benchmark run: {benchmarkList.Count} benchmarks");

        foreach (var benchmark in benchmarkList)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Console.WriteLine($"Running {benchmark.FullName}...");

            try
            {
                var result = await RunSingleAsync(benchmark, config, cancellationToken);
                results.Add(result);

                if (result.Success)
                {
                    var metricsInfo =
                        result.Metrics.TotalCalls > 0
                            ? $"{result.Metrics.TotalTokens} tokens, {result.Metrics.TotalCalls} calls"
                            : "no metrics";

                    Console.WriteLine(
                        $"  Completed in {result.Duration.TotalSeconds:F1}s, {metricsInfo}"
                    );
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
            catch (OperationCanceledException)
            {
                Console.WriteLine($"  Cancelled");
                throw;
            }
            catch (Exception ex)
            {
                // Log unexpected error but continue with other benchmarks
                Console.WriteLine($"  CRITICAL ERROR: {ex.Message}");
                Trace($"Stack trace: {ex.StackTrace}");

                results.Add(
                    new BenchmarkLlmResult
                    {
                        Category = benchmark.Category,
                        BenchmarkName = benchmark.Name,
                        Prompt = benchmark.Prompt,
                        Success = false,
                        Error = ex.Message,
                        ErrorDetails = ex.StackTrace,
                        Duration = TimeSpan.Zero,
                        Metrics = new AggregatedMetrics(new List<CallMetrics>()),
                        IsBaseline = benchmark.IsBaseline,
                    }
                );
            }
        }

        Trace($"Benchmark run complete: {results.Count(r => r.Success)}/{results.Count} succeeded");
        return results;
    }

    private async Task<BenchmarkLlmResult> RunSingleAsync(
        BenchmarkInfo benchmark,
        BenchmarkLlmConfig config,
        CancellationToken cancellationToken
    )
    {
        var metricsClients = new List<MetricsCollectingChatClient>();
        var stopwatch = Stopwatch.StartNew();

        AggregatedMetrics GetMetrics() =>
            new(metricsClients.SelectMany(c => c.CallMetrics).ToList());

        try
        {
            Trace($"Creating instance of {benchmark.DeclaringType.Name}");

            var instance =
                Activator.CreateInstance(benchmark.DeclaringType)
                ?? throw new InvalidOperationException(
                    $"Could not create instance of {benchmark.DeclaringType.Name}"
                );

            var parameters = benchmark.Method.GetParameters();
            Trace(
                $"Method signature: {string.Join(", ", parameters.Select(p => $"{p.ParameterType.Name} {p.Name}"))}"
            );
            Trace($"Prompt: {benchmark.Prompt}");

            object? taskResult;

            // Wrap all ChatClientFactory.Create() calls to collect metrics
            using (
                ChatClientFactory.UseWrapper(
                    (client, model) =>
                    {
                        var wrapped = new MetricsCollectingChatClient(client, model);
                        metricsClients.Add(wrapped);
                        return wrapped;
                    }
                )
            )
            {
                // Invoke benchmark - signature: () or (string prompt) for backwards compatibility
                if (parameters.Length == 0)
                {
                    taskResult = benchmark.Method.Invoke(instance, []);
                }
                else if (parameters.Length == 1 && parameters[0].ParameterType == typeof(string))
                {
                    taskResult = benchmark.Method.Invoke(instance, [benchmark.Prompt]);
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Benchmark method {benchmark.FullName} has unsupported signature. "
                            + "Expected: () or (string prompt)"
                    );
                }
            }

            if (taskResult == null)
            {
                throw new InvalidOperationException(
                    $"Benchmark method {benchmark.FullName} returned null"
                );
            }

            string content;
            IReadOnlyDictionary<string, string>? agentModels = null;

            Trace("Awaiting benchmark result...");

            if (taskResult is Task<BenchmarkOutput> outputTask)
            {
                var output = await outputTask.ConfigureAwait(false);
                content =
                    output.Content
                    ?? throw new InvalidOperationException("BenchmarkOutput.Content is null");
                agentModels = output.AgentModels;
                Trace(
                    $"Got BenchmarkOutput: {content.Length} chars, {agentModels?.Count ?? 0} agent models"
                );
            }
            else if (taskResult is Task<string> stringTask)
            {
                content =
                    await stringTask.ConfigureAwait(false)
                    ?? throw new InvalidOperationException("Benchmark returned null string");
                Trace($"Got string result: {content.Length} chars");
            }
            else
            {
                throw new InvalidOperationException(
                    $"Benchmark method must return Task<string> or Task<BenchmarkOutput>, got {taskResult.GetType().Name}"
                );
            }

            stopwatch.Stop();

            var metrics = GetMetrics();
            Trace($"Metrics: {metrics.TotalCalls} calls, {metrics.TotalTokens} tokens");

            if (agentModels != null && agentModels.Count > 0)
            {
                Console.WriteLine(
                    $"  Agents: {string.Join(", ", agentModels.Select(kv => $"{kv.Key}={kv.Value}"))}"
                );
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                throw new InvalidOperationException("Benchmark produced empty content");
            }

            QualityScore? qualityScore = null;
            if (config.Evaluate && _evaluator != null)
            {
                try
                {
                    Console.WriteLine($"  Evaluating quality...");
                    qualityScore = await _evaluator.EvaluateAsync(
                        benchmark.Prompt,
                        content,
                        cancellationToken
                    );
                    Console.WriteLine($"  Quality: {qualityScore.Average:F1}/5");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  Evaluation failed: {ex.Message}");
                    Trace($"Evaluation error: {ex}");
                }
            }

            return new BenchmarkLlmResult
            {
                Category = benchmark.Category,
                BenchmarkName = benchmark.Name,
                Prompt = benchmark.Prompt,
                Success = true,
                Content = content,
                Duration = stopwatch.Elapsed,
                Metrics = metrics,
                QualityScore = qualityScore,
                IsBaseline = benchmark.IsBaseline,
                AgentModels = agentModels,
            };
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            stopwatch.Stop();
            var innerEx = ex.InnerException;
            Trace($"Benchmark failed with {innerEx.GetType().Name}: {innerEx.Message}");

            return new BenchmarkLlmResult
            {
                Category = benchmark.Category,
                BenchmarkName = benchmark.Name,
                Prompt = benchmark.Prompt,
                Success = false,
                Error = innerEx.Message,
                ErrorDetails = innerEx.StackTrace,
                Duration = stopwatch.Elapsed,
                Metrics = GetMetrics(),
                IsBaseline = benchmark.IsBaseline,
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Trace($"Benchmark failed with {ex.GetType().Name}: {ex.Message}");

            return new BenchmarkLlmResult
            {
                Category = benchmark.Category,
                BenchmarkName = benchmark.Name,
                Prompt = benchmark.Prompt,
                Success = false,
                Error = ex.Message,
                ErrorDetails = ex.StackTrace,
                Duration = stopwatch.Elapsed,
                Metrics = GetMetrics(),
                IsBaseline = benchmark.IsBaseline,
            };
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
