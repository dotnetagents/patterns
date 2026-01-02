using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;

namespace DotNetAgents.BenchmarkLlm.Evaluation;

/// <summary>
/// Evaluates agentic task completion using LLM-as-Judge methodology.
/// Designed for tool-use and multi-step agent workflows.
/// </summary>
public sealed class AgentTaskEvaluator : IContentEvaluator
{
    private readonly IChatClient _judgeClient;

    private const string SystemPrompt = """
        You are an expert evaluator assessing AI agent task completion.
        Your role is to determine if the agent successfully completed the requested task.
        Focus on outcomes and correct tool usage, not writing quality.
        Be strict - the task either succeeded or it didn't.
        """;

    private const string EvaluationPrompt = """
        Evaluate whether the AI agent successfully completed this task: "{0}"

        ## Scoring Rubrics (1-5 scale)

        **COMPLETENESS** - Did the agent complete ALL steps of the task?
        - 5: All required steps completed successfully
        - 4: Most steps completed, minor omissions
        - 3: Core task done but some steps skipped
        - 2: Task partially completed, significant steps missing
        - 1: Task not completed or wrong task performed

        **STRUCTURE** - Did the agent follow a logical sequence of actions?
        - 5: Optimal sequence, efficient tool usage
        - 4: Good sequence with minor inefficiencies
        - 3: Completed task but with unnecessary steps
        - 2: Confusing sequence, redundant actions
        - 1: Chaotic, illogical action sequence

        **ACCURACY** - Did the agent make correct decisions and tool calls?
        - 5: All decisions and parameters correct
        - 4: Mostly correct with minor issues
        - 3: Some incorrect choices but recovered
        - 2: Multiple errors affecting outcome
        - 1: Fundamentally wrong approach

        **ENGAGEMENT** - Did the agent communicate clearly about its actions?
        - 5: Clear, helpful communication throughout
        - 4: Good communication with minor gaps
        - 3: Adequate but could be clearer
        - 2: Confusing or incomplete communication
        - 1: Poor or misleading communication

        **EVIDENCE_QUALITY** - Did the agent use data from tools correctly?
        - 5: Correctly interpreted all tool responses
        - 4: Good interpretation with minor issues
        - 3: Some misinterpretation but functional
        - 2: Significant misuse of tool data
        - 1: Ignored or misunderstood tool responses

        **BALANCE** - Did the agent handle the task appropriately (not over/under-doing)?
        - 5: Perfect scope - did exactly what was needed
        - 4: Appropriate scope with minor extras/omissions
        - 3: Acceptable but scope could be better
        - 2: Over-engineered or under-delivered
        - 1: Completely wrong scope

        **ACTIONABILITY** - Did the agent take concrete actions vs just talking?
        - 5: All necessary actions taken, task completed
        - 4: Most actions taken, task mostly complete
        - 3: Some actions taken but incomplete execution
        - 2: More talk than action, limited progress
        - 1: No meaningful actions taken

        **DEPTH** - Did the agent handle edge cases and details?
        - 5: Handled all details and potential issues
        - 4: Good detail handling with minor oversights
        - 3: Basic handling, some details missed
        - 2: Shallow execution, many details missed
        - 1: No attention to details or edge cases

        ---

        ## Agent Output to Evaluate

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
        reasoning: [2-3 sentences explaining whether the task was completed successfully and any issues]
        """;

    public AgentTaskEvaluator(IChatClient judgeClient)
    {
        _judgeClient = judgeClient;
    }

    public async Task<QualityScore> EvaluateAsync(
        string task,
        string agentOutput,
        CancellationToken cancellationToken = default
    )
    {
        var cleanOutput = StripVerificationFooter(agentOutput);

        if (cleanOutput.Length > 8000)
        {
            cleanOutput = cleanOutput[..8000] + "\n\n[Output truncated for evaluation...]";
        }

        var userPrompt = string.Format(EvaluationPrompt, task, cleanOutput);

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

    private static string StripVerificationFooter(string content)
    {
        // Remove our verification footer from the output before evaluation
        return Regex.Replace(content, @"\n\n[✓✗].*$", "", RegexOptions.Singleline).Trim();
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
