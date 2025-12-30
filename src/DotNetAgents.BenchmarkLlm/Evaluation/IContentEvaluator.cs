namespace DotNetAgents.BenchmarkLlm.Evaluation;

/// <summary>
/// Interface for content quality evaluation.
/// </summary>
public interface IContentEvaluator
{
    /// <summary>
    /// Evaluates the quality of generated content.
    /// </summary>
    Task<QualityScore> EvaluateAsync(
        string topic,
        string content,
        CancellationToken cancellationToken = default
    );
}
