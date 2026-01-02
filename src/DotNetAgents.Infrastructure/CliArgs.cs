namespace DotNetAgents.Infrastructure;

/// <summary>
/// Simple CLI argument parser for pattern projects.
/// </summary>
public class CliArgs
{
    private readonly string[] _args;

    public CliArgs(string[] args) => _args = args;

    /// <summary>
    /// Check if a flag is present (e.g., --benchmark or -b).
    /// </summary>
    public bool HasFlag(string flag, string? shortFlag = null)
    {
        return _args.Contains(flag) || (shortFlag != null && _args.Contains(shortFlag));
    }

    /// <summary>
    /// Get the value following a flag (e.g., --model gpt-4.1 returns "gpt-4.1").
    /// </summary>
    public string? GetValue(string flag, string? shortFlag = null)
    {
        var index = Array.IndexOf(_args, flag);
        if (index == -1 && shortFlag != null)
            index = Array.IndexOf(_args, shortFlag);

        if (index >= 0 && index < _args.Length - 1)
            return _args[index + 1];

        return null;
    }

    // Common arguments
    public string? Provider => GetValue("--provider", "-p");
    public string? Model => GetValue("--model", "-m");
    public string? Filter => GetValue("--filter", "-f");
    public string? EvaluatePath => GetValue("--evaluate", "-e");
    public bool IsBenchmark => HasFlag("--benchmark", "-b");
    public bool IsList => HasFlag("--list-benchmarks", "--list");

    /// <summary>
    /// Validate required arguments for interactive mode.
    /// </summary>
    public bool ValidateInteractiveArgs(out string? error)
    {
        if (string.IsNullOrEmpty(Provider))
        {
            error = "Error: --provider is required for interactive mode.\n"
                  + "Valid providers: ollama, openai, azure, openrouter, github";
            return false;
        }

        if (string.IsNullOrEmpty(Model))
        {
            error = "Error: --model is required for interactive mode.\n"
                  + "Examples: 'gpt-4o', 'gpt-4o-mini', 'llama3.2'";
            return false;
        }

        error = null;
        return true;
    }

    /// <summary>
    /// Print usage information.
    /// </summary>
    public static void PrintUsage(string patternName)
    {
        Console.WriteLine($"Usage: dotnet run -- [options]");
        Console.WriteLine();
        Console.WriteLine("Modes:");
        Console.WriteLine("  (default)              Interactive mode");
        Console.WriteLine("  --benchmark, -b        Run benchmarks");
        Console.WriteLine("  --evaluate, -e <path>  Evaluate existing run results");
        Console.WriteLine("  --list-benchmarks      List available benchmarks");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --provider, -p <name>  LLM provider (azure, openai, ollama, openrouter, github)");
        Console.WriteLine("  --model, -m <name>     Model name (e.g., gpt-4.1, llama3.2)");
        Console.WriteLine("  --filter, -f <pattern> Filter benchmarks by pattern");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine($"  dotnet run -- --provider azure --model gpt-4.1");
        Console.WriteLine($"  dotnet run -- --benchmark");
        Console.WriteLine($"  dotnet run -- --benchmark --filter \"multi-agent\"");
    }
}
