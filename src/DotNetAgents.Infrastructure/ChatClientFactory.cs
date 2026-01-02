using System.ClientModel;
using Azure.AI.OpenAI;
using DotNetEnv;
using Microsoft.Extensions.AI;
using OllamaSharp;
using OpenAI;
using OpenAI.Chat;

namespace DotNetAgents.Infrastructure;

/// <summary>
/// Factory for creating IChatClient instances based on environment configuration.
/// Supports: Azure OpenAI, OpenAI, OpenRouter, GitHub Models, and Ollama (local).
/// </summary>
/// <remarks>
/// Each provider uses its own environment variables, allowing multiple providers
/// to be configured simultaneously for benchmarking:
///
/// - Ollama:      OLLAMA_ENDPOINT (optional, defaults to http://localhost:11434)
/// - OpenAI:      OPENAI_API_KEY, OPENAI_ENDPOINT (optional)
/// - Azure:       AZURE_OPENAI_API_KEY, AZURE_OPENAI_ENDPOINT
/// - OpenRouter:  OPENROUTER_API_KEY, OPENROUTER_ENDPOINT (optional, defaults to https://openrouter.ai/api/v1)
/// - GitHub:      GITHUB_TOKEN, GITHUB_MODELS_ENDPOINT (optional, defaults to https://models.github.ai/inference)
///
/// Provider must always be specified explicitly in code via Create(provider, model) or AgentConfig.Provider.
/// </remarks>
public static class ChatClientFactory
{
    private static bool _envLoaded;

    /// <summary>
    /// Source name for OpenTelemetry instrumentation.
    /// </summary>
    public const string TelemetrySourceName = "DotNetAgents.Benchmark";

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

    /// <summary>
    /// Creates a chat client for a specific provider.
    /// </summary>
    /// <param name="provider">Provider name: "ollama", "openai", "azure", "openrouter", "github"</param>
    /// <param name="model">Model name (e.g., "gpt-4o", "llama3.2")</param>
    public static IChatClient Create(string provider, string model)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            throw new ArgumentException(
                "Provider must be explicitly specified. "
                    + "Valid providers: ollama, openai, azure, openrouter, github",
                nameof(provider)
            );
        }

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

        provider = provider.ToLowerInvariant();
        Console.WriteLine($"[ChatClientFactory] Provider: {provider}, Model: {model}");

        var client = provider switch
        {
            "azure" => CreateAzureOpenAIClient(model),
            "openai" => CreateOpenAIClient(model),
            "openrouter" => CreateOpenRouterClient(model),
            "github" => CreateGitHubModelsClient(model),
            "ollama" => CreateOllamaClient(model),
            _ => throw new ArgumentException(
                $"Unknown provider: {provider}. Valid providers: ollama, openai, azure, openrouter, github",
                nameof(provider)
            ),
        };

        // Always apply OpenTelemetry instrumentation for observability
        return client.AsBuilder()
            .UseOpenTelemetry(
                sourceName: TelemetrySourceName,
                configure: c => c.EnableSensitiveData = false)
            .Build();
    }

    private static string GetEnvVar(string name) =>
        Environment.GetEnvironmentVariable(name)
        ?? throw new InvalidOperationException(
            $"{name} environment variable not set. "
                + "See: https://dotnetagents.net/blog/setting-up-agent-environment"
        );

    private static string? GetEnvVarOptional(string name) =>
        Environment.GetEnvironmentVariable(name);

    private static IChatClient CreateAzureOpenAIClient(string model)
    {
        string apiKey = GetEnvVar("AZURE_OPENAI_API_KEY");
        string endpoint = GetEnvVar("AZURE_OPENAI_ENDPOINT");

        return new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(apiKey))
            .GetChatClient(model)
            .AsIChatClient();
    }

    private static IChatClient CreateOpenAIClient(string model)
    {
        string apiKey = GetEnvVar("OPENAI_API_KEY");
        string? endpoint = GetEnvVarOptional("OPENAI_ENDPOINT");

        if (endpoint != null)
        {
            var options = new OpenAIClientOptions { Endpoint = new Uri(endpoint) };
            return new OpenAIClient(new ApiKeyCredential(apiKey), options)
                .GetChatClient(model)
                .AsIChatClient();
        }

        return new ChatClient(model, apiKey).AsIChatClient();
    }

    private static IChatClient CreateOpenRouterClient(string model)
    {
        string apiKey = GetEnvVar("OPENROUTER_API_KEY");
        string endpoint =
            GetEnvVarOptional("OPENROUTER_ENDPOINT") ?? "https://openrouter.ai/api/v1";

        var options = new OpenAIClientOptions { Endpoint = new Uri(endpoint) };

        return new OpenAIClient(new ApiKeyCredential(apiKey), options)
            .GetChatClient(model)
            .AsIChatClient();
    }

    private static IChatClient CreateGitHubModelsClient(string model)
    {
        string apiKey = GetEnvVar("GITHUB_TOKEN");
        string endpoint =
            GetEnvVarOptional("GITHUB_MODELS_ENDPOINT") ?? "https://models.github.ai/inference";

        var options = new OpenAIClientOptions { Endpoint = new Uri(endpoint) };

        return new OpenAIClient(new ApiKeyCredential(apiKey), options)
            .GetChatClient(model)
            .AsIChatClient();
    }

    private static IChatClient CreateOllamaClient(string model)
    {
        string endpoint = GetEnvVarOptional("OLLAMA_ENDPOINT") ?? "http://localhost:11434";
        return new OllamaApiClient(new Uri(endpoint), model);
    }
}
