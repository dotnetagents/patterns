using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;

namespace DotNetAgents.BenchmarkLlm.Evaluation;

/// <summary>
/// Evaluates content quality using LLM-as-Judge methodology.
/// </summary>
public sealed class LlmJudgeEvaluator : IContentEvaluator
{
    private readonly IChatClient _judgeClient;

    private const string SystemPrompt = """
        You are an expert content evaluator assessing AI-generated educational and informational content.
        Your role is to provide objective, consistent quality assessments using the scoring rubrics provided.
        Be strict but fair - reserve 5s for truly exceptional content and 1s for content with serious deficiencies.
        """;

    private const string EvaluationPrompt = """
        Evaluate the following content written about: "{0}"

        ## Scoring Rubrics (1-5 scale)

        **COMPLETENESS** - Does the content thoroughly cover the topic?
        - 5: Comprehensive coverage of all key aspects, no significant gaps
        - 4: Covers most important points with minor omissions
        - 3: Addresses main topic but misses some relevant points
        - 2: Superficial coverage, several important aspects missing
        - 1: Severely incomplete, barely addresses the topic

        **STRUCTURE** - Is the content well-organized and logical?
        - 5: Excellent organization with clear flow, headings, and transitions
        - 4: Good structure with minor flow issues
        - 3: Adequate organization but could be clearer
        - 2: Disorganized, hard to follow the progression
        - 1: No discernible structure, chaotic presentation

        **ACCURACY** - Is the information factually correct and reliable?
        - 5: All claims are accurate, well-reasoned, no errors
        - 4: Mostly accurate with very minor issues
        - 3: Generally accurate but some questionable claims
        - 2: Contains noticeable factual errors or misleading statements
        - 1: Significantly inaccurate or contains harmful misinformation

        **ENGAGEMENT** - Is the content readable and compelling?
        - 5: Highly engaging, excellent writing quality, maintains interest throughout
        - 4: Well-written and interesting with minor dull spots
        - 3: Readable but somewhat dry or formulaic
        - 2: Difficult to read, poorly written, or boring
        - 1: Unreadable, confusing, or off-putting

        **EVIDENCE_QUALITY** - Does the content use concrete evidence to support claims?
        - 5: Rich with specific statistics, company examples, academic citations, case studies
        - 4: Good evidence with several concrete examples and some data
        - 3: Some evidence but mostly general claims without specifics
        - 2: Few examples, vague references, lacks concrete data
        - 1: No evidence, all claims unsupported or purely theoretical

        **BALANCE** - Does the content address both benefits AND limitations/challenges?
        - 5: Thoroughly discusses pros and cons, acknowledges trade-offs and challenges
        - 4: Good balance with some acknowledgment of limitations
        - 3: Mentions limitations briefly but mostly one-sided
        - 2: Almost entirely one-sided, minimal acknowledgment of downsides
        - 1: Completely one-sided, no mention of limitations or challenges

        **ACTIONABILITY** - Does the content provide practical implementation guidance?
        - 5: Clear step-by-step guidance, specific tips, implementation strategies
        - 4: Good practical advice with some specific recommendations
        - 3: Some practical elements but mostly theoretical discussion
        - 2: Vague suggestions, lacks concrete actionable advice
        - 1: Purely theoretical, no practical guidance whatsoever

        **DEPTH** - Does the content provide deep analysis or just surface-level summary?
        - 5: Deep analysis with nuanced insights, explores implications thoroughly
        - 4: Good depth with meaningful analysis beyond basics
        - 3: Moderate depth, covers basics well but lacks deeper exploration
        - 2: Shallow treatment, skims surface of topics
        - 1: Extremely superficial, barely scratches the surface

        ---

        ## Content to Evaluate

        {1}

        ---

        ## Your Evaluation

        Provide your scores and a brief justification. Format your response exactly as:

        completeness: [1-5]
        structure: [1-5]
        accuracy: [1-5]
        engagement: [1-5]
        evidence_quality: [1-5]
        balance: [1-5]
        actionability: [1-5]
        depth: [1-5]
        reasoning: [2-3 sentences explaining key strengths and weaknesses]
        """;

    public LlmJudgeEvaluator(IChatClient judgeClient)
    {
        _judgeClient = judgeClient;
    }

    public async Task<QualityScore> EvaluateAsync(
        string topic,
        string content,
        CancellationToken cancellationToken = default
    )
    {
        var cleanContent = StripAgentHeaders(content);

        if (cleanContent.Length > 8000)
        {
            cleanContent = cleanContent[..8000] + "\n\n[Content truncated for evaluation...]";
        }

        var userPrompt = string.Format(EvaluationPrompt, topic, cleanContent);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, SystemPrompt),
            new(ChatRole.User, userPrompt),
        };

        string responseText;
        try
        {
            var responseBuilder = new System.Text.StringBuilder();
            await foreach (
                var update in _judgeClient.GetStreamingResponseAsync(
                    messages,
                    cancellationToken: cancellationToken
                )
            )
            {
                responseBuilder.Append(update.Text ?? string.Empty);
            }
            responseText = responseBuilder.ToString();
        }
        catch (Exception ex)
        {
            return CreateDefaultScore($"API call failed: {ex.Message}");
        }

        return ParseEvaluationResponse(responseText);
    }

    private static string StripAgentHeaders(string content)
    {
        return Regex.Replace(content, @"\n*=== \[.*?\] ===\n*", "\n\n").Trim();
    }

    private static QualityScore ParseEvaluationResponse(string response)
    {
        return ExtractScoresFromText(response);
    }

    private static QualityScore ExtractScoresFromText(string response)
    {
        int GetScore(string text, string dimension)
        {
            var patterns = new[]
            {
                $@"{dimension}\s*[:\-=]\s*(\d)",
                $@"{dimension}\s*\(?\s*(\d)\s*/\s*5\s*\)?",
                $@"""?{dimension}""?\s*:\s*(\d)",
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
                if (match.Success && int.TryParse(match.Groups[1].Value, out var score))
                {
                    return Math.Clamp(score, 1, 5);
                }
            }
            return 3;
        }

        string GetReasoning(string text)
        {
            // Try to extract reasoning from various formats
            var patterns = new[]
            {
                @"reasoning\s*[:\-=]\s*(.+?)(?=\n\n|$)",
                @"reasoning\s*[:\-=]\s*(.+)",
                @"\*\*reasoning\*\*\s*[:\-=]\s*(.+?)(?=\n\n|$)",
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(
                    text,
                    pattern,
                    RegexOptions.IgnoreCase | RegexOptions.Singleline
                );
                if (match.Success)
                {
                    var reasoning = match.Groups[1].Value.Trim();
                    // Clean up any trailing content that might be captured
                    reasoning = Regex.Replace(reasoning, @"\n+$", "").Trim();
                    if (!string.IsNullOrEmpty(reasoning))
                    {
                        return reasoning;
                    }
                }
            }
            return "No detailed reasoning provided";
        }

        return new QualityScore
        {
            Completeness = GetScore(response, "completeness"),
            Structure = GetScore(response, "structure"),
            Accuracy = GetScore(response, "accuracy"),
            Engagement = GetScore(response, "engagement"),
            EvidenceQuality = GetScore(response, "evidence_quality"),
            Balance = GetScore(response, "balance"),
            Actionability = GetScore(response, "actionability"),
            Depth = GetScore(response, "depth"),
            Reasoning = GetReasoning(response),
        };
    }

    private static QualityScore CreateDefaultScore(string reason) =>
        new()
        {
            Completeness = 3,
            Structure = 3,
            Accuracy = 3,
            Engagement = 3,
            EvidenceQuality = 3,
            Balance = 3,
            Actionability = 3,
            Depth = 3,
            Reasoning = reason,
        };
}
