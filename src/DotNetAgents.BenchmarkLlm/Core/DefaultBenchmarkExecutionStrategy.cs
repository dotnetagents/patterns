using System.Diagnostics;
using System.Reflection;
using DotNetAgents.BenchmarkLlm.Evaluation;
using DotNetAgents.BenchmarkLlm.Metrics;

namespace DotNetAgents.BenchmarkLlm.Core;

/// <summary>
/// Default strategy for executing benchmarks with telemetry collection.
/// </summary>
public sealed class DefaultBenchmarkExecutionStrategy
{
    private readonly BenchmarkInvoker _invoker = new();

    public async Task<BenchmarkLlmResult> ExecuteAsync(
        BenchmarkInfo benchmark,
        BenchmarkExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        using var telemetry = new BenchmarkTelemetryCollector();

        try
        {
            context.TraceOutput?.Invoke($"Creating instance of {benchmark.DeclaringType.Name}");

            var (content, agentModels) = await _invoker.InvokeAsync(benchmark, cancellationToken);

            stopwatch.Stop();
            var metrics = telemetry.GetMetrics();

            context.TraceOutput?.Invoke($"Metrics: {metrics.TotalCalls} calls, {metrics.TotalTokens} tokens");

            if (string.IsNullOrWhiteSpace(content))
                throw new InvalidOperationException("Benchmark produced empty content");

            var qualityScore = await EvaluateIfEnabled(context, benchmark.Prompt, content, cancellationToken);

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
            return CreateFailureResult(benchmark, ex.InnerException, stopwatch, telemetry, context);
        }
        catch (Exception ex)
        {
            return CreateFailureResult(benchmark, ex, stopwatch, telemetry, context);
        }
    }

    private static async Task<QualityScore?> EvaluateIfEnabled(
        BenchmarkExecutionContext context,
        string prompt,
        string content,
        CancellationToken cancellationToken)
    {
        if (!context.Evaluate || context.Evaluator == null)
            return null;

        try
        {
            Console.WriteLine("  Evaluating quality...");
            var score = await context.Evaluator.EvaluateAsync(prompt, content, cancellationToken);
            Console.WriteLine($"  Quality: {score.Average:F1}/5");
            return score;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Evaluation failed: {ex.Message}");
            context.TraceOutput?.Invoke($"Evaluation error: {ex}");
            return null;
        }
    }

    private static BenchmarkLlmResult CreateFailureResult(
        BenchmarkInfo benchmark,
        Exception ex,
        Stopwatch stopwatch,
        BenchmarkTelemetryCollector telemetry,
        BenchmarkExecutionContext context)
    {
        stopwatch.Stop();
        context.TraceOutput?.Invoke($"Benchmark failed with {ex.GetType().Name}: {ex.Message}");
        AggregatedMetrics metrics = telemetry.GetMetrics();
        return new BenchmarkLlmResult
        {
            Category = benchmark.Category,
            BenchmarkName = benchmark.Name,
            Prompt = benchmark.Prompt,
            Success = false,
            Error = ex.Message,
            ErrorDetails = ex.StackTrace,
            Duration = stopwatch.Elapsed,
            Metrics = metrics,
            IsBaseline = benchmark.IsBaseline,
        };
    }
}
