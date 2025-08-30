using BenchmarkDotNet.Attributes;

namespace Squire.NumTic.Benchmark;

/// <summary>
///   Benchmarks different approaches for finding the maximum token value across two HashSet collections.
///   Used to optimize the formatted token lookup table size in ConsoleGameInterface.BuildFormattedTokenLookup().
/// </summary>
///
[MemoryDiagnoser]
[SimpleJob]
public class TokenMaxValueBenchmarks
{
    private readonly HashSet<byte> _oddTokens3x3 = new() { 1, 3, 5, 7, 9 };
    private readonly HashSet<byte> _evenTokens3x3 = new() { 2, 4, 6, 8 };

    private readonly HashSet<byte> _oddTokens4x4 = new() { 1, 3, 5, 7, 9, 11, 13, 15 };
    private readonly HashSet<byte> _evenTokens4x4 = new() { 2, 4, 6, 8, 10, 12, 14, 16 };

    private readonly HashSet<byte> _oddTokens5x5 = new() { 1, 3, 5, 7, 9, 11, 13, 15, 17, 19, 21, 23, 25 };
    private readonly HashSet<byte> _evenTokens5x5 = new() { 2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22, 24 };

    private readonly HashSet<byte> _oddTokens10x10 = new() { 1, 3, 5, 7, 9, 11, 13, 15, 17, 19, 21, 23, 25, 27, 29, 31, 33, 35, 37, 39, 41, 43, 45, 47, 49, 51, 53, 55, 57, 59, 61, 63, 65, 67, 69, 71, 73, 75, 77, 79, 81, 83, 85, 87, 89, 91, 93, 95, 97, 99 };
    private readonly HashSet<byte> _evenTokens10x10 = new() { 2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22, 24, 26, 28, 30, 32, 34, 36, 38, 40, 42, 44, 46, 48, 50, 52, 54, 56, 58, 60, 62, 64, 66, 68, 70, 72, 74, 76, 78, 80, 82, 84, 86, 88, 90, 92, 94, 96, 98, 100 };

    private readonly HashSet<byte> _oddTokensExtreme = Enumerable.Range(1, 128).Where(x => x % 2 == 1).Select(x => (byte)x).ToHashSet();
    private readonly HashSet<byte> _evenTokensExtreme = Enumerable.Range(2, 127).Where(x => x % 2 == 0).Select(x => (byte)x).ToHashSet();

    /// <summary>
    ///   Finds the maximum token value using stackalloc array and sorting approach for 3x3 board.
    /// </summary>
    ///
    [Benchmark]
    public byte StackallocSorted3x3()
    {
        // Allocate space for both token sets.

        var totalCount = _oddTokens3x3.Count + _evenTokens3x3.Count;
        Span<byte> tokens = stackalloc byte[totalCount];

        // Copy tokens from both hashsets into the array.

        var index = 0;

        foreach (var token in _oddTokens3x3)
        {
            tokens[index++] = token;
        }

        foreach (var token in _evenTokens3x3)
        {
            tokens[index++] = token;
        }

        // Sort the array and return the highest value.

        tokens.Sort();
        return tokens[^1];
    }

    /// <summary>
    ///   Finds the maximum token value using variable tracking approach for 3x3 board.
    /// </summary>
    ///
    [Benchmark]
    public byte VariableTracking3x3()
    {
        // Track maximum value while enumerating both hashsets.

        byte max = 0;

        foreach (var token in _oddTokens3x3)
        {
            if (token > max)
            {
                max = token;
            }
        }

        foreach (var token in _evenTokens3x3)
        {
            if (token > max)
            {
                max = token;
            }
        }

        return max;
    }

    /// <summary>
    ///   Finds the maximum token value using stackalloc array and sorting approach for 4x4 board.
    /// </summary>
    ///
    [Benchmark]
    public byte StackallocSorted4x4()
    {
        var totalCount = _oddTokens4x4.Count + _evenTokens4x4.Count;
        Span<byte> tokens = stackalloc byte[totalCount];

        var index = 0;

        foreach (var token in _oddTokens4x4)
        {
            tokens[index++] = token;
        }

        foreach (var token in _evenTokens4x4)
        {
            tokens[index++] = token;
        }

        tokens.Sort();
        return tokens[^1];
    }

    /// <summary>
    ///   Finds the maximum token value using variable tracking approach for 4x4 board.
    /// </summary>
    ///
    [Benchmark]
    public byte VariableTracking4x4()
    {
        byte max = 0;

        foreach (var token in _oddTokens4x4)
        {
            if (token > max)
            {
                max = token;
            }
        }

        foreach (var token in _evenTokens4x4)
        {
            if (token > max)
            {
                max = token;
            }
        }

        return max;
    }

    /// <summary>
    ///   Finds the maximum token value using stackalloc array and sorting approach for 5x5 board.
    /// </summary>
    ///
    [Benchmark]
    public byte StackallocSorted5x5()
    {
        var totalCount = _oddTokens5x5.Count + _evenTokens5x5.Count;
        Span<byte> tokens = stackalloc byte[totalCount];

        var index = 0;

        foreach (var token in _oddTokens5x5)
        {
            tokens[index++] = token;
        }

        foreach (var token in _evenTokens5x5)
        {
            tokens[index++] = token;
        }

        tokens.Sort();
        return tokens[^1];
    }

    /// <summary>
    ///   Finds the maximum token value using variable tracking approach for 5x5 board.
    /// </summary>
    ///
    [Benchmark]
    public byte VariableTracking5x5()
    {
        byte max = 0;

        foreach (var token in _oddTokens5x5)
        {
            if (token > max)
            {
                max = token;
            }
        }

        foreach (var token in _evenTokens5x5)
        {
            if (token > max)
            {
                max = token;
            }
        }

        return max;
    }

    /// <summary>
    ///   Finds the maximum token value using stackalloc array and sorting approach for 10x10 board.
    /// </summary>
    ///
    [Benchmark]
    public byte StackallocSorted10x10()
    {
        var totalCount = _oddTokens10x10.Count + _evenTokens10x10.Count;
        Span<byte> tokens = stackalloc byte[totalCount];

        var index = 0;

        foreach (var token in _oddTokens10x10)
        {
            tokens[index++] = token;
        }

        foreach (var token in _evenTokens10x10)
        {
            tokens[index++] = token;
        }

        tokens.Sort();
        return tokens[^1];
    }

    /// <summary>
    ///   Finds the maximum token value using variable tracking approach for 10x10 board.
    /// </summary>
    ///
    [Benchmark]
    public byte VariableTracking10x10()
    {
        byte max = 0;

        foreach (var token in _oddTokens10x10)
        {
            if (token > max)
            {
                max = token;
            }
        }

        foreach (var token in _evenTokens10x10)
        {
            if (token > max)
            {
                max = token;
            }
        }

        return max;
    }

    /// <summary>
    ///   Finds the maximum token value using stackalloc array and sorting approach for extreme case.
    /// </summary>
    ///
    [Benchmark]
    public byte StackallocSortedExtreme()
    {
        var totalCount = _oddTokensExtreme.Count + _evenTokensExtreme.Count;
        Span<byte> tokens = stackalloc byte[totalCount];

        var index = 0;

        foreach (var token in _oddTokensExtreme)
        {
            tokens[index++] = token;
        }

        foreach (var token in _evenTokensExtreme)
        {
            tokens[index++] = token;
        }

        tokens.Sort();
        return tokens[^1];
    }

    /// <summary>
    ///   Finds the maximum token value using variable tracking approach for extreme case.
    /// </summary>
    ///
    [Benchmark]
    public byte VariableTrackingExtreme()
    {
        byte max = 0;

        foreach (var token in _oddTokensExtreme)
        {
            if (token > max)
            {
                max = token;
            }
        }

        foreach (var token in _evenTokensExtreme)
        {
            if (token > max)
            {
                max = token;
            }
        }

        return max;
    }

    /// <summary>
    ///   Finds the maximum token value using LINQ Max() for comparison baseline (3x3).
    /// </summary>
    ///
    [Benchmark]
    public byte LinqMax3x3()
    {
        return Math.Max(_oddTokens3x3.Max(), _evenTokens3x3.Max());
    }

    /// <summary>
    ///   Finds the maximum token value using LINQ Max() for comparison baseline (extreme).
    /// </summary>
    ///
    [Benchmark]
    public byte LinqMaxExtreme()
    {
        return Math.Max(_oddTokensExtreme.Max(), _evenTokensExtreme.Max());
    }
}