using System.Text;
using System.Text.RegularExpressions;
using DotNetAgents.BenchmarkLlm.Core;
using Microsoft.Extensions.AI;

namespace DotNetAgents.BenchmarkLlm.Evaluation;

/// <summary>
/// Evaluates multiple benchmark outputs comparatively using LLM-as-Judge.
/// </summary>
public sealed class ComparativeEvaluator
{
    private readonly IChatClient _judgeClient;

    private const string SystemPrompt = """
        You are an expert content analyst comparing AI-generated outputs from different approaches.
        Your role is to provide objective, detailed comparative analysis highlighting the strengths
        and weaknesses of each approach. Be specific and cite examples from the content.
        """;

    private const string ComparisonPrompt = """
        Compare the following benchmark outputs written about: "<<PROMPT>>"

        ## Benchmark Outputs

        <<CONTENT>>

        ## Metrics Summary

        <<METRICS>>

        ---

        ## Your Analysis

        Provide a detailed comparative analysis. Format your response exactly as:

        ### ANALYSIS
        [3-5 paragraphs comparing the outputs across dimensions like:
        - Content depth and comprehensiveness
        - Evidence quality (statistics, examples, citations)
        - Balance (pros vs cons coverage)
        - Practical actionability
        - Writing quality and engagement
        Cite specific examples from each output to support your analysis.]

        ### STRENGTHS AND WEAKNESSES
        [For each benchmark, list 2-3 key strengths and 2-3 key weaknesses in this format:]

        **[benchmark_name]**
        Strengths:
        - [strength 1]
        - [strength 2]
        Weaknesses:
        - [weakness 1]
        - [weakness 2]

        ### VERDICT
        [2-3 sentences summarizing which approach performed better and why, considering both quality and efficiency (tokens/latency).]
        """;

    public ComparativeEvaluator(IChatClient judgeClient)
    {
        _judgeClient = judgeClient;
    }

    public async Task<ComparativeAnalysis> CompareAsync(
        IReadOnlyList<BenchmarkLlmResult> results,
        CancellationToken cancellationToken = default
    )
    {
        var prompt = results.FirstOrDefault()?.Prompt ?? "Unknown";

        // Build benchmark comparisons with metrics
        var comparisons = results
            .Select(r => new BenchmarkComparison
            {
                FullName = r.FullName,
                IsBaseline = r.IsBaseline,
                WordCount = CountWords(r.Content ?? ""),
                TotalTokens = r.Metrics.TotalTokens,
                TotalCalls = r.Metrics.TotalCalls,
                TotalLatencyMs = r.Metrics.TotalLatencyMs,
                QualityScore = r.QualityScore,
            })
            .ToList();

        // Build content section for LLM
        var contentBuilder = new StringBuilder();
        foreach (var result in results)
        {
            var baseline = result.IsBaseline ? " (BASELINE)" : "";
            contentBuilder.AppendLine($"### {result.FullName}{baseline}");
            contentBuilder.AppendLine();

            var content = TruncateContent(result.Content ?? "(No content)", 3000);
            contentBuilder.AppendLine(content);
            contentBuilder.AppendLine();
            contentBuilder.AppendLine("---");
            contentBuilder.AppendLine();
        }

        // Build metrics section
        var metricsBuilder = new StringBuilder();
        metricsBuilder.AppendLine("| Benchmark | Words | Tokens | API Calls | Latency |");
        metricsBuilder.AppendLine("|-----------|-------|--------|-----------|---------|");
        foreach (var c in comparisons)
        {
            var baseline = c.IsBaseline ? " *" : "";
            metricsBuilder.AppendLine(
                $"| {c.FullName}{baseline} | {c.WordCount} | {c.TotalTokens} | {c.TotalCalls} | {c.TotalLatencyMs}ms |"
            );
        }
        metricsBuilder.AppendLine();
        metricsBuilder.AppendLine("*baseline");

        // Call LLM for comparative analysis
        var userPrompt = ComparisonPrompt
            .Replace("<<PROMPT>>", prompt)
            .Replace("<<CONTENT>>", contentBuilder.ToString())
            .Replace("<<METRICS>>", metricsBuilder.ToString());

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, SystemPrompt),
            new(ChatRole.User, userPrompt),
        };

        string responseText;
        try
        {
            var responseBuilder = new StringBuilder();
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
            return new ComparativeAnalysis
            {
                Prompt = prompt,
                Timestamp = DateTime.UtcNow,
                Benchmarks = comparisons,
                Analysis = $"Comparative analysis failed: {ex.Message}",
                Verdict = "Unable to generate verdict due to API error.",
            };
        }

        // Parse the response
        var (analysis, verdict, strengthsWeaknesses) = ParseComparisonResponse(responseText);

        // Update comparisons with strengths/weaknesses
        var updatedComparisons = comparisons
            .Select(c =>
            {
                if (strengthsWeaknesses.TryGetValue(c.FullName, out var sw))
                {
                    return c with { Strengths = sw.Strengths, Weaknesses = sw.Weaknesses };
                }
                return c;
            })
            .ToList();

        return new ComparativeAnalysis
        {
            Prompt = prompt,
            Timestamp = DateTime.UtcNow,
            Benchmarks = updatedComparisons,
            Analysis = analysis,
            Verdict = verdict,
        };
    }

    private static int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        return Regex.Matches(text, @"\b\w+\b").Count;
    }

    private static string TruncateContent(string content, int maxLength)
    {
        if (content.Length <= maxLength)
            return content;

        return content[..maxLength] + "\n\n[Content truncated for comparison...]";
    }

    private static (
        string Analysis,
        string Verdict,
        Dictionary<string, (List<string> Strengths, List<string> Weaknesses)> StrengthsWeaknesses
    ) ParseComparisonResponse(string response)
    {
        var analysis = "";
        var verdict = "";
        var strengthsWeaknesses =
            new Dictionary<string, (List<string> Strengths, List<string> Weaknesses)>();

        // Extract analysis section
        var analysisMatch = Regex.Match(
            response,
            @"###\s*ANALYSIS\s*\n(.*?)(?=###\s*STRENGTHS|###\s*VERDICT|$)",
            RegexOptions.Singleline | RegexOptions.IgnoreCase
        );
        if (analysisMatch.Success)
        {
            analysis = analysisMatch.Groups[1].Value.Trim();
        }

        // Extract verdict section
        var verdictMatch = Regex.Match(
            response,
            @"###\s*VERDICT\s*\n(.*?)(?=###|$)",
            RegexOptions.Singleline | RegexOptions.IgnoreCase
        );
        if (verdictMatch.Success)
        {
            verdict = verdictMatch.Groups[1].Value.Trim();
        }

        // Extract strengths and weaknesses for each benchmark
        var swSection = Regex.Match(
            response,
            @"###\s*STRENGTHS\s+AND\s+WEAKNESSES\s*\n(.*?)(?=###\s*VERDICT|$)",
            RegexOptions.Singleline | RegexOptions.IgnoreCase
        );

        if (swSection.Success)
        {
            var section = swSection.Groups[1].Value;

            // Find each benchmark block
            var benchmarkMatches = Regex.Matches(
                section,
                @"\*\*([^*]+)\*\*\s*\n\s*Strengths:\s*\n(.*?)Weaknesses:\s*\n(.*?)(?=\*\*|$)",
                RegexOptions.Singleline | RegexOptions.IgnoreCase
            );

            foreach (Match match in benchmarkMatches)
            {
                var name = match.Groups[1].Value.Trim();
                var strengthsText = match.Groups[2].Value;
                var weaknessesText = match.Groups[3].Value;

                var strengths = ExtractListItems(strengthsText);
                var weaknesses = ExtractListItems(weaknessesText);

                strengthsWeaknesses[name] = (strengths, weaknesses);
            }
        }

        // Fallback if parsing failed
        if (string.IsNullOrEmpty(analysis))
        {
            analysis = response;
        }
        if (string.IsNullOrEmpty(verdict))
        {
            verdict = "See analysis above.";
        }

        return (analysis, verdict, strengthsWeaknesses);
    }

    private static List<string> ExtractListItems(string text)
    {
        var items = new List<string>();
        var matches = Regex.Matches(
            text,
            @"[-*]\s*(.+?)(?=\n[-*]|\n\n|$)",
            RegexOptions.Singleline
        );
        foreach (Match match in matches)
        {
            var item = match.Groups[1].Value.Trim();
            if (!string.IsNullOrEmpty(item))
            {
                items.Add(item);
            }
        }
        return items;
    }
}
