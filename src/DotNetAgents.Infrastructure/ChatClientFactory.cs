using System.ClientModel;
using Azure.AI.OpenAI;
using DotNetEnv;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OllamaSharp;
using OpenAI;
using OpenAI.Chat;

namespace DotNetAgents.Infrastructure;

/// <summary>
/// Factory for creating IChatClient instances based on environment configuration.
/// Supports: Azure OpenAI, OpenAI, OpenRouter, and Ollama (local).
/// </summary>
/// <remarks>
/// Environment variables:
/// - LLM_PROVIDER: "ollama" (default), "openai", "openrouter", "azure"
/// - API_KEY: API key for the provider (not needed for Ollama)
/// - ENDPOINT: Endpoint URL (required for Azure, optional for Ollama)
/// </remarks>
public static class ChatClientFactory
{
    private static bool _envLoaded;
    private static Func<IChatClient, string, IChatClient>? _wrapper;

    public static IDisposable UseWrapper(Func<IChatClient, string, IChatClient> wrapper)
    {
        _wrapper = wrapper;
        return new WrapperScope(() => _wrapper = null);
    }

    private sealed class WrapperScope(Action onDispose) : IDisposable
    {
        public void Dispose() => onDispose();
    }

    private static void EnsureEnvLoaded()
    {
        if (_envLoaded)
            return;
        _envLoaded = true;

        // Search for .env file in current and parent directories
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory != null)
        {
            var envPath = Path.Combine(directory.FullName, ".env");
            if (File.Exists(envPath))
            {
                Env.Load(envPath);
                Console.WriteLine($"[ChatClientFactory] Loaded .env from: {envPath}");
                return;
            }
            directory = directory.Parent;
        }
    }

    public static ConfiguredAgent CreateAgent(AgentConfig config)
    {
        return new ConfiguredAgent
        {
            Name = config.Name,
            Model = config.Model,
            Instructions = config.Instructions,
            ChatClientAgent = new(
                Create(config.Model),
                instructions: config.Instructions,
                name: config.Name
            ),
        };
    }

    public static IChatClient Create(string model)
    {
        if (string.IsNullOrWhiteSpace(model))
        {
            throw new ArgumentException(
                "Model must be explicitly specified. "
                    + "Examples: 'gpt-4o', 'gpt-4o-mini', 'llama3.2'",
                nameof(model)
            );
        }

        // Load .env file if present
        EnsureEnvLoaded();

        string provider =
            Environment.GetEnvironmentVariable("LLM_PROVIDER")?.ToLowerInvariant() ?? "ollama";

        Console.WriteLine($"[ChatClientFactory] Provider: {provider}, Model: {model}");

        var client = provider switch
        {
            "azure" => CreateAzureOpenAIClient(model),
            "openai" => CreateOpenAIClient(model),
            "openrouter" => CreateOpenRouterClient(model),
            "ollama" or _ => CreateOllamaClient(model),
        };

        return _wrapper != null ? _wrapper(client, model) : client;
    }

    private static string GetApiKey() =>
        Environment.GetEnvironmentVariable("API_KEY")
        ?? throw new InvalidOperationException(
            "API_KEY environment variable not set. "
                + "See: https://dotnetagents.net/getting-started"
        );

    private static string? GetEndpoint() => Environment.GetEnvironmentVariable("ENDPOINT");

    private static IChatClient CreateAzureOpenAIClient(string model)
    {
        string apiKey = GetApiKey();
        string endpoint =
            GetEndpoint()
            ?? throw new InvalidOperationException(
                "ENDPOINT environment variable not set for Azure. "
                    + "See: https://dotnetagents.net/getting-started"
            );

        return new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(apiKey))
            .GetChatClient(model)
            .AsIChatClient();
    }

    private static IChatClient CreateOpenAIClient(string model)
    {
        string apiKey = GetApiKey();
        return new ChatClient(model, apiKey).AsIChatClient();
    }

    private static IChatClient CreateOpenRouterClient(string model)
    {
        string apiKey = GetApiKey();

        var options = new OpenAIClientOptions
        {
            Endpoint = new Uri("https://openrouter.ai/api/v1"),
        };

        return new OpenAIClient(new ApiKeyCredential(apiKey), options)
            .GetChatClient(model)
            .AsIChatClient();
    }

    private static IChatClient CreateOllamaClient(string model)
    {
        string endpoint = GetEndpoint() ?? "http://localhost:11434";
        return new OllamaApiClient(new Uri(endpoint), model);
    }
}
