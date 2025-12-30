namespace DotNetAgents.BenchmarkLlm.Evaluation;

/// <summary>
/// Quality evaluation scores from LLM-as-Judge assessment.
/// Each dimension is scored 1-5 (1=Poor, 5=Excellent).
/// </summary>
public sealed record QualityScore
{
    /// <summary>
    /// How well the content covers all key aspects of the topic (1-5).
    /// </summary>
    public required int Completeness { get; init; }

    /// <summary>
    /// How well-organized the content is with clear sections and flow (1-5).
    /// </summary>
    public required int Structure { get; init; }

    /// <summary>
    /// How accurate and well-reasoned the facts/claims are (1-5).
    /// </summary>
    public required int Accuracy { get; init; }

    /// <summary>
    /// How readable and engaging the content is (1-5).
    /// </summary>
    public required int Engagement { get; init; }

    /// <summary>
    /// Quality of evidence: citations, statistics, real-world examples (1-5).
    /// </summary>
    public required int EvidenceQuality { get; init; }

    /// <summary>
    /// Balance: addresses pros AND cons, not just benefits (1-5).
    /// </summary>
    public required int Balance { get; init; }

    /// <summary>
    /// Practical implementation guidance and actionable advice (1-5).
    /// </summary>
    public required int Actionability { get; init; }

    /// <summary>
    /// Depth of analysis vs surface-level summary (1-5).
    /// </summary>
    public required int Depth { get; init; }

    /// <summary>
    /// Average of all quality dimensions.
    /// </summary>
    public double Average =>
        (
            Completeness
            + Structure
            + Accuracy
            + Engagement
            + EvidenceQuality
            + Balance
            + Actionability
            + Depth
        ) / 8.0;

    /// <summary>
    /// Brief explanation of the scores from the judge.
    /// </summary>
    public string? Reasoning { get; init; }

    public override string ToString() =>
        $"Quality: {Average:F1}/5 (C:{Completeness} S:{Structure} A:{Accuracy} E:{Engagement} Ev:{EvidenceQuality} B:{Balance} Ac:{Actionability} D:{Depth})";
}
