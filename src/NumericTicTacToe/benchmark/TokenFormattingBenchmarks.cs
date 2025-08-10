using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Squire.NumTic.Benchmark;

/// <summary>
///   Benchmark comparing the current FormatToken implementation against the previous switch statement approach.
/// </summary>
///
[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
[CategoriesColumn]
public class TokenFormattingBenchmarks
{
    /// <summary>Test tokens covering the different formatting cases.</summary>
    private readonly byte[] _testTokens = [1, 5, 9, 10, 15, 25, 50, 99, 100, 255];

    /// <summary>
    ///   Benchmarks the current FormatToken implementation using pattern matching with stackalloc.
    /// </summary>
    ///
    /// <returns>The total length of all formatted tokens (to prevent optimization).</returns>
    ///
    [Benchmark(Baseline = true)]
    public int CurrentFormatTokenApproach()
    {
        var totalLength = 0;

        foreach (var token in _testTokens)
        {
            var formatted = FormatTokenCurrent(token);
            totalLength += formatted.Length;
        }

        return totalLength;
    }

    /// <summary>
    ///   Benchmarks the previous FormatToken implementation using a switch statement with string interpolation.
    /// </summary>
    ///
    /// <returns>The total length of all formatted tokens (to prevent optimization).</returns>
    ///
    [Benchmark]
    public int PreviousFormatTokenApproach()
    {
        var totalLength = 0;

        foreach (var token in _testTokens)
        {
            var formatted = FormatTokenPrevious(token);
            totalLength += formatted.Length;
        }

        return totalLength;
    }

    /// <summary>
    ///   The current FormatToken implementation from ConsoleGameInterface.
    /// </summary>
    ///
    /// <param name="token">The token value to format.</param>
    ///
    /// <returns>The formatted token string.</returns>
    ///
    private static string FormatTokenCurrent(byte token) =>
        token switch
        {
            < 10 => new string((Span<char>)stackalloc char[3] { ' ', (char)('0' + token), ' ' }),
            < 100 => new string((Span<char>)stackalloc char[3] { ' ', (char)('0' + token / 10), (char)('0' + token % 10) }),
            _ => token.ToString()
        };

    /// <summary>
    ///   The previous FormatToken implementation using a switch statement with string interpolation.
    /// </summary>
    ///
    /// <param name="token">The token value to format.</param>
    ///
    /// <returns>The formatted token string.</returns>
    ///
    private static string FormatTokenPrevious(byte token)
    {
        return token switch
        {
            < 10 => $" {token} ",
            < 100 => $" {token}",
            _ => token.ToString()
        };
    }
}
