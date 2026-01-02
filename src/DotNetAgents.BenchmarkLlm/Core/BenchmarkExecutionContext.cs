using DotNetAgents.BenchmarkLlm.Evaluation;

namespace DotNetAgents.BenchmarkLlm.Core;

/// <summary>
/// Context passed to execution strategies.
/// </summary>
public sealed record BenchmarkExecutionContext
{
    public required bool Evaluate { get; init; }

    public required IContentEvaluator? Evaluator { get; init; }

    public required Action<string>? TraceOutput { get; init; }
}
