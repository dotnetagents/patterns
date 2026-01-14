using System.ComponentModel;
using ModelContextProtocol.Server;

namespace McpCalculatorServer.Tools;

/// <summary>
/// Calculator tools exposed via Model Context Protocol (MCP).
/// These tools demonstrate how to create MCP-compliant tool definitions
/// that can be discovered and invoked by MCP clients.
/// </summary>
[McpServerToolType]
public static class CalculatorTools
{
    [McpServerTool, Description("Add two numbers together")]
    public static double Add(
        [Description("First number")] double a,
        [Description("Second number")] double b)
        => a + b;

    [McpServerTool, Description("Subtract second number from first")]
    public static double Subtract(
        [Description("First number")] double a,
        [Description("Second number")] double b)
        => a - b;

    [McpServerTool, Description("Multiply two numbers")]
    public static double Multiply(
        [Description("First number")] double a,
        [Description("Second number")] double b)
        => a * b;

    [McpServerTool, Description("Divide first number by second")]
    public static double Divide(
        [Description("Dividend")] double a,
        [Description("Divisor (must not be zero)")] double b)
    {
        if (b == 0)
            throw new ArgumentException("Cannot divide by zero", nameof(b));
        return a / b;
    }

    [McpServerTool, Description("Raise a number to a power")]
    public static double Power(
        [Description("Base number")] double baseNum,
        [Description("Exponent")] double exponent)
        => Math.Pow(baseNum, exponent);

    [McpServerTool, Description("Calculate the square root of a number")]
    public static double SquareRoot([Description("Number (must be non-negative)")] double n)
    {
        if (n < 0)
            throw new ArgumentException("Cannot take square root of negative number", nameof(n));
        return Math.Sqrt(n);
    }

    [McpServerTool, Description("Calculate the factorial of a non-negative integer")]
    public static long Factorial([Description("Non-negative integer (max 20)")] int n)
    {
        if (n < 0)
            throw new ArgumentException("Factorial undefined for negative numbers", nameof(n));
        if (n > 20)
            throw new ArgumentException("Factorial too large (max n=20)", nameof(n));

        long result = 1;
        for (int i = 2; i <= n; i++)
            result *= i;
        return result;
    }

    [McpServerTool, Description("Calculate the absolute value of a number")]
    public static double AbsoluteValue([Description("Number to get absolute value of")] double n)
        => Math.Abs(n);

    [McpServerTool, Description("Calculate the natural logarithm (ln) of a number")]
    public static double NaturalLog([Description("Number (must be positive)")] double n)
    {
        if (n <= 0)
            throw new ArgumentException("Natural log undefined for non-positive numbers", nameof(n));
        return Math.Log(n);
    }

    [McpServerTool, Description("Calculate the sine of an angle in radians")]
    public static double Sine([Description("Angle in radians")] double radians)
        => Math.Sin(radians);

    [McpServerTool, Description("Calculate the cosine of an angle in radians")]
    public static double Cosine([Description("Angle in radians")] double radians)
        => Math.Cos(radians);
}
