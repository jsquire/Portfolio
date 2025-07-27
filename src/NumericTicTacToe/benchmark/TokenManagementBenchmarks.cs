using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Squire.NumTic.Benchmark;

/// <summary>
///   Benchmarks for token management operations including enumeration and collection operations.
/// </summary>
///
[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
[CategoriesColumn]
public class TokenManagementBenchmarks
{
    /// <summary>The game state for token operation benchmarks.</summary>
    private GameState _gameState = null!;

    /// <summary>The token collection for comparison benchmarks.</summary>
    private HashSet<byte> _tokens = null!;

    /// <summary>
    ///   Initializes the benchmark test data with game state and token collections.
    /// </summary>
    ///
    [GlobalSetup]
    public void Setup()
    {
        _gameState = GameState.CreateDefault();
        _tokens = new HashSet<byte> { 1, 3, 5, 7, 9 };
    }

    /// <summary>
    ///   Benchmarks standard HashSet enumeration for token operations.
    /// </summary>
    ///
    /// <returns>The sum of all tokens in the collection.</returns>
    ///
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("TokenEnumeration")]
    public int HashSetEnumeration()
    {
        var sum = 0;

        foreach (var token in _gameState.CurrentPlayerTokens)
        {
            sum += token;
        }

        return sum;
    }

    /// <summary>
    ///   Benchmarks Span-based enumeration for token operations.
    /// </summary>
    ///
    /// <returns>The sum of all tokens in the span.</returns>
    ///
    [Benchmark]
    [BenchmarkCategory("TokenEnumeration")]
    public int SpanEnumeration()
    {
        // Simulate the optimization we implemented.

        Span<byte> playerTokens = stackalloc byte[_gameState.CurrentPlayerTokens.Count];
        var index = 0;

        foreach (var token in _gameState.CurrentPlayerTokens)
        {
            playerTokens[index] = token;
            ++index;
        }

        var sum = 0;

        foreach (var token in playerTokens)
        {
            sum += token;
        }

        return sum;
    }

    /// <summary>
    ///   Benchmarks checking if a token exists in the current player's collection.
    /// </summary>
    ///
    /// <returns>True if the token exists in the collection, false otherwise.</returns>
    ///
    [Benchmark]
    [BenchmarkCategory("TokenOperations")]
    public bool ContainsToken() => _gameState.CurrentPlayerTokens.Contains(5);

    /// <summary>
    ///   Benchmarks add and remove operations on a token collection copy.
    /// </summary>
    ///
    [Benchmark]
    [BenchmarkCategory("TokenOperations")]
    public void AddRemoveToken()
    {
        var tokens = new HashSet<byte>(_gameState.CurrentPlayerTokens);
        tokens.Remove(5);
        tokens.Add(5);
    }

    /// <summary>
    ///   Benchmarks creating a copy of the current player's token collection.
    /// </summary>
    ///
    /// <returns>A new HashSet containing copies of the current player tokens.</returns>
    ///
    [Benchmark]
    [BenchmarkCategory("TokenCopying")]
    public HashSet<byte> CreateTokenCopy()
    {
        return [.. _gameState.CurrentPlayerTokens];
    }
}
