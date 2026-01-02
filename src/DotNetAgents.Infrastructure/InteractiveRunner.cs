using System.Text;
using Microsoft.Extensions.AI;

namespace DotNetAgents.Infrastructure;

/// <summary>
/// Reusable interactive conversation loop with streaming responses.
/// </summary>
public static class InteractiveRunner
{
    /// <summary>
    /// Run an interactive chat session with streaming responses.
    /// </summary>
    /// <param name="client">The chat client to use.</param>
    /// <param name="systemPrompt">System prompt for the assistant.</param>
    /// <param name="title">Title to display in the header.</param>
    /// <param name="provider">Provider name for display.</param>
    /// <param name="model">Model name for display.</param>
    /// <param name="options">Optional chat options (e.g., tools).</param>
    /// <param name="onClear">Optional callback when user types 'clear'.</param>
    public static async Task RunAsync(
        IChatClient client,
        string systemPrompt,
        string title,
        string provider,
        string model,
        ChatOptions? options = null,
        Func<Task>? onClear = null)
    {
        PrintHeader(title, provider, model);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, systemPrompt)
        };

        while (true)
        {
            Console.Write("You: ");
            var input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
                continue;

            if (input.Equals("quit", StringComparison.OrdinalIgnoreCase) ||
                input.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Goodbye!");
                break;
            }

            if (input.Equals("clear", StringComparison.OrdinalIgnoreCase))
            {
                messages.Clear();
                messages.Add(new(ChatRole.System, systemPrompt));
                if (onClear != null)
                    await onClear();
                Console.WriteLine("[Conversation cleared]\n");
                continue;
            }

            messages.Add(new(ChatRole.User, input));
            Console.Write("\nAssistant: ");

            var fullResponse = new StringBuilder();

            try
            {
                await foreach (var chunk in client.GetStreamingResponseAsync(messages, options))
                {
                    Console.Write(chunk.Text);
                    fullResponse.Append(chunk.Text);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[Error: {ex.Message}]");
                messages.RemoveAt(messages.Count - 1); // Remove failed user message
                Console.WriteLine();
                continue;
            }

            messages.Add(new(ChatRole.Assistant, fullResponse.ToString()));
            Console.WriteLine("\n");
        }
    }

    private static void PrintHeader(string title, string provider, string model)
    {
        var titleLine = $"  {title}";
        var modelLine = $"  Provider: {provider} | Model: {model}";
        var width = Math.Max(Math.Max(titleLine.Length, modelLine.Length) + 4, 60);

        var border = new string('=', width);
        var padding = new string(' ', width - 2);

        Console.WriteLine($"+{border}+");
        Console.WriteLine($"|{titleLine.PadRight(width)}|");
        Console.WriteLine($"|{new string('-', width)}|");
        Console.WriteLine($"|{modelLine.PadRight(width)}|");
        Console.WriteLine($"|  Commands: 'quit' to exit, 'clear' to reset{new string(' ', width - 46)}|");
        Console.WriteLine($"+{border}+");
        Console.WriteLine();
    }
}
