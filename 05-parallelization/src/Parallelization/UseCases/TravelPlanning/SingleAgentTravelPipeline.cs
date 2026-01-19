using DotNetAgents.Patterns.Parallelization.Services;

namespace DotNetAgents.Patterns.Parallelization.UseCases.TravelPlanning;

/// <summary>
/// Pipeline that uses a single agent to handle all travel planning.
/// Used as a baseline to compare quality and approach against specialized agents.
/// </summary>
public static class SingleAgentTravelPipeline
{
    private const string SystemPrompt = """
        You are a comprehensive travel planning assistant. You help users plan their trips by researching and recommending:
        - Hotels and accommodations
        - Flights and transportation
        - Activities, attractions, and restaurants

        When given a travel planning request:
        1. Use the search tool to research hotels in the destination
        2. Use the search tool to find flight and transportation options
        3. Use the search tool to discover activities and things to do
        4. Compile everything into a comprehensive travel plan

        Your response should include:
        - Hotel recommendations with prices and features
        - Transportation options (flights, trains, local transport)
        - A day-by-day itinerary with activities
        - Restaurant recommendations
        - Budget estimates
        - Practical travel tips

        Be thorough, specific, and actionable. Include real names of hotels, airlines, and attractions when possible.
        """;

    /// <summary>
    /// Executes the single-agent travel planning pipeline.
    /// </summary>
    public static async Task<(string Result, Dictionary<string, string> AgentModels)> RunAsync(
        SingleAgentTravelConfig config,
        IGoogleSearchService searchService,
        string query,
        CancellationToken cancellationToken = default)
    {
        // Create agent using shared factory
        var agent = TravelAgentFactory.Create(
            config.Provider, config.Model, SystemPrompt, "TravelPlanner", searchService);

        var response = await agent.RunAsync(query, cancellationToken: cancellationToken);

        var agentModels = new Dictionary<string, string>
        {
            ["TravelPlanner"] = config.Model
        };

        return (response.Text, agentModels);
    }
}
