using DotNetAgents.BenchmarkLlm.Metrics;
using Microsoft.Extensions.AI;

namespace DotNetAgents.BenchmarkLlm.Core;

/// <summary>
/// Context passed to benchmark methods providing access to metrics-tracked client creation.
/// All clients created through this context will have their metrics collected.
/// </summary>
public sealed class BenchmarkContext
{
    private readonly MetricsCollectingChatClient _metricsClient;
    private readonly Func<string, IChatClient> _clientFactory;
    private readonly List<MetricsCollectingChatClient> _additionalClients = new();
    private readonly object _lock = new();

    /// <summary>
    /// Creates a new benchmark context.
    /// </summary>
    /// <param name="metricsClient">The primary metrics-collecting client.</param>
    /// <param name="clientFactory">Factory function to create clients for specific models.</param>
    public BenchmarkContext(
        MetricsCollectingChatClient metricsClient,
        Func<string, IChatClient> clientFactory
    )
    {
        _metricsClient = metricsClient;
        _clientFactory = clientFactory;
    }

    /// <summary>
    /// Gets the default chat client (wrapped with metrics collection).
    /// Use this when you want all agents to use the same model.
    /// </summary>
    public IChatClient Client => _metricsClient;

    /// <summary>
    /// Creates a chat client for a specific model, wrapped with metrics collection.
    /// Use this when different agents need different models.
    /// </summary>
    /// <param name="model">The model name (e.g., "gpt-4o", "gpt-4o-mini").</param>
    /// <returns>A metrics-collecting chat client for the specified model.</returns>
    public IChatClient CreateClient(string model)
    {
        var innerClient = _clientFactory(model);
        var wrappedClient = new MetricsCollectingChatClient(innerClient, $"model:{model}");

        lock (_lock)
        {
            _additionalClients.Add(wrappedClient);
        }

        return wrappedClient;
    }

    /// <summary>
    /// Gets aggregated metrics from all clients created through this context.
    /// </summary>
    public AggregatedMetrics GetAggregatedMetrics()
    {
        var allMetrics = new List<CallMetrics>();

        // Add metrics from primary client
        allMetrics.AddRange(_metricsClient.CallMetrics);

        // Add metrics from additional clients
        lock (_lock)
        {
            foreach (var client in _additionalClients)
            {
                allMetrics.AddRange(client.CallMetrics);
            }
        }

        return new AggregatedMetrics(allMetrics);
    }
}
