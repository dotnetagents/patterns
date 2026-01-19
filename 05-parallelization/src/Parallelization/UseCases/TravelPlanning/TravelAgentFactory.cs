using DotNetAgents.Infrastructure;
using DotNetAgents.Patterns.Parallelization.Services;
using DotNetAgents.Patterns.Parallelization.Tools;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace DotNetAgents.Patterns.Parallelization.UseCases.TravelPlanning;

/// <summary>
/// Factory for creating ChatClientAgent instances with Google Search tool.
/// Used by both parallel and sequential travel planning pipelines.
/// </summary>
public static class TravelAgentFactory
{
    public const string HotelsAgentName = "HotelsAgent";
    public const string TransportAgentName = "TransportAgent";
    public const string ActivitiesAgentName = "ActivitiesAgent";

    public static readonly string[] ExpectedAgentNames = [HotelsAgentName, TransportAgentName, ActivitiesAgentName];

    private const string HotelsSystemPrompt = """
        You are a specialized travel agent focused on finding the best hotels and accommodations.

        IMPORTANT: You MUST use the Search tool to find current hotel information. Do NOT rely on your training data.
        Always search before providing recommendations.

        When given a travel query:
        1. FIRST, use the Search tool to find hotels in the destination (e.g., "best hotels in [city] [dates]")
        2. Search for specific hotel types based on the traveler's needs
        3. Use multiple searches if needed for different hotel categories

        After searching, provide recommendations including:
        - 3-5 specific hotel recommendations with names and prices from search results
        - Key features and amenities
        - Location benefits
        - Booking tips
        - ALWAYS include the booking/info links from search results for each hotel

        You MUST call the Search tool at least once before responding.
        Format links as: [Hotel Name](url)
        """;

    private const string TransportSystemPrompt = """
        You are a specialized travel agent focused on transportation and getting around.

        IMPORTANT: You MUST use the Search tool to find current flight and transport information. Do NOT rely on your training data.
        Always search before providing recommendations.

        When given a travel query:
        1. FIRST, use the Search tool to find flights (e.g., "flights from [origin] to [destination] [dates]")
        2. Search for local transportation options at the destination
        3. Use multiple searches for different transport types

        After searching, provide recommendations including:
        - Flight options with prices from search results
        - Airport transfer options
        - Local transportation tips
        - Travel passes that save money
        - ALWAYS include booking/info links from search results for flights and transport options

        You MUST call the Search tool at least once before responding.
        Format links as: [Airline/Service Name](url)
        """;

    private const string ActivitiesSystemPrompt = """
        You are a specialized travel agent focused on activities, attractions, and experiences.

        IMPORTANT: You MUST use the Search tool to find current information about attractions and activities. Do NOT rely on your training data.
        Always search before providing recommendations.

        When given a travel query:
        1. FIRST, use the Search tool to find attractions (e.g., "top things to do in [city]")
        2. Search for restaurants and dining options
        3. Use multiple searches for different activity types

        After searching, provide recommendations including:
        - Top attractions with descriptions from search results
        - Restaurant recommendations
        - Suggested day-by-day itinerary
        - Tickets to book in advance
        - ALWAYS include links from search results for attractions, restaurants, and ticket booking

        You MUST call the Search tool at least once before responding.
        Format links as: [Attraction/Restaurant Name](url)
        """;

    public static ChatClientAgent CreateHotelsAgent(string provider, string model, IGoogleSearchService searchService)
        => Create(provider, model, HotelsSystemPrompt, HotelsAgentName, searchService);

    public static ChatClientAgent CreateTransportAgent(string provider, string model, IGoogleSearchService searchService)
        => Create(provider, model, TransportSystemPrompt, TransportAgentName, searchService);

    public static ChatClientAgent CreateActivitiesAgent(string provider, string model, IGoogleSearchService searchService)
        => Create(provider, model, ActivitiesSystemPrompt, ActivitiesAgentName, searchService);

    internal static ChatClientAgent Create(
        string provider,
        string model,
        string instructions,
        string name,
        IGoogleSearchService searchService)
    {
        var searchTool = new GoogleSearchTool(searchService);
        var tools = new List<AITool> { AIFunctionFactory.Create(searchTool.Search) };

        var chatClient = new ChatClientBuilder(ChatClientFactory.Create(provider, model))
            .UseFunctionInvocation()
            .Build();

        return new ChatClientAgent(chatClient, instructions, name, tools: tools);
    }
}