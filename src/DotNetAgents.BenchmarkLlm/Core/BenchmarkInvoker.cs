namespace DotNetAgents.BenchmarkLlm.Core;

/// <summary>
/// Handles reflection-based benchmark method invocation.
/// </summary>
public sealed class BenchmarkInvoker
{
    /// <summary>
    /// Invokes a benchmark method and returns content + agent models.
    /// </summary>
    public async Task<(string Content, IReadOnlyDictionary<string, string>? AgentModels)> InvokeAsync(
        BenchmarkInfo benchmark,
        CancellationToken cancellationToken = default)
    {
        var instance = Activator.CreateInstance(benchmark.DeclaringType)
            ?? throw new InvalidOperationException(
                $"Could not create instance of {benchmark.DeclaringType.Name}");

        var parameters = benchmark.Method.GetParameters();

        object? taskResult = parameters.Length switch
        {
            0 => benchmark.Method.Invoke(instance, []),
            1 when parameters[0].ParameterType == typeof(string)
                => benchmark.Method.Invoke(instance, [benchmark.Prompt]),
            _ => throw new InvalidOperationException(
                $"Benchmark {benchmark.FullName} has unsupported signature. Expected: () or (string prompt)")
        };

        return await UnwrapResultAsync(taskResult, benchmark.FullName);
    }

    private static async Task<(string, IReadOnlyDictionary<string, string>?)> UnwrapResultAsync(
        object? taskResult,
        string benchmarkName)
    {
        return taskResult switch
        {
            Task<BenchmarkOutput> outputTask => await UnwrapOutputTask(outputTask),
            Task<string> stringTask => (
                await stringTask ?? throw new InvalidOperationException("Benchmark returned null string"),
                null),
            null => throw new InvalidOperationException($"Benchmark {benchmarkName} returned null"),
            _ => throw new InvalidOperationException(
                $"Benchmark method must return Task<string> or Task<BenchmarkOutput>, got {taskResult.GetType().Name}")
        };
    }

    private static async Task<(string, IReadOnlyDictionary<string, string>?)> UnwrapOutputTask(
        Task<BenchmarkOutput> task)
    {
        var output = await task;
        return (
            output.Content ?? throw new InvalidOperationException("BenchmarkOutput.Content is null"),
            output.AgentModels);
    }
}
