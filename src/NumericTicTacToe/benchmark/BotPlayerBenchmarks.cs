using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using NSubstitute;
using Squire.NumTic.Contracts;
using Squire.NumTic.Players;

namespace Squire.NumTic.Benchmark;

/// <summary>
///   Benchmarks for BotPlayer AI operations including move selection and concurrent evaluation patterns.
/// </summary>
///
[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
[CategoriesColumn]
public class BotPlayerBenchmarks
{
    /// <summary>The early game state with all tokens available.</summary>
    private GameState _earlyGameState = null!;

    /// <summary>The mid-game state with some tokens already used.</summary>
    private GameState _midGameState = null!;

    /// <summary>The easy difficulty bot player instance.</summary>
    private BotPlayer _easyBot = null!;

    /// <summary>The hard difficulty bot player instance.</summary>
    private BotPlayer _hardBot = null!;

    /// <summary>The mock game interface for bot player operations.</summary>
    private IGameInterface _mockInterface = null!;

    /// <summary>
    ///   Initializes the benchmark test data with bot players and game states.
    /// </summary>
    ///
    [GlobalSetup]
    public void Setup()
    {
        _mockInterface = Substitute.For<IGameInterface>();

        _easyBot = new BotPlayer(_mockInterface,
            new BotPlayerOptions { Difficulty = Difficulty.Easy });

        _hardBot = new BotPlayer(_mockInterface,
            new BotPlayerOptions { Difficulty = Difficulty.Hard });

        // Early game (all tokens available).

        _earlyGameState = GameState.CreateDefault();

        // Mid game (some tokens used).

        _midGameState = GameState.CreateDefault();
        _midGameState.ApplyMove(new Move(PlayerToken.Odd, 0, 1));
        _midGameState.ApplyMove(new Move(PlayerToken.Even, 1, 2));
    }

    /// <summary>
    ///   Benchmarks easy bot move selection on an early game state.
    /// </summary>
    ///
    /// <returns>The move selected by the easy bot.</returns>
    ///
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("BotDifficulty")]
    public async Task<Move> EasyBot_EarlyGame() => await _easyBot.PlayTurnAsync(_earlyGameState);

    /// <summary>
    ///   Benchmarks easy bot move selection on a mid-game state.
    /// </summary>
    ///
    /// <returns>The move selected by the easy bot.</returns>
    ///
    [Benchmark]
    [BenchmarkCategory("BotDifficulty")]
    public async Task<Move> EasyBot_MidGame() => await _easyBot.PlayTurnAsync(_midGameState);

    /// <summary>
    ///   Benchmarks hard bot move selection on an early game state.
    /// </summary>
    ///
    /// <returns>The move selected by the hard bot.</returns>
    ///
    [Benchmark]
    [BenchmarkCategory("BotDifficulty")]
    public async Task<Move> HardBot_EarlyGame() => await _hardBot.PlayTurnAsync(_earlyGameState);

    /// <summary>
    ///   Benchmarks hard bot move selection on a mid-game state.
    /// </summary>
    ///
    /// <returns>The move selected by the hard bot.</returns>
    ///
    [Benchmark]
    [BenchmarkCategory("BotDifficulty")]
    public async Task<Move> HardBot_MidGame() => await _hardBot.PlayTurnAsync(_midGameState);

    /// <summary>
    ///   Benchmarks the concurrent copying pattern used by BotPlayer for move evaluation.
    /// </summary>
    ///
    [Benchmark]
    [BenchmarkCategory("AllocationPatterns")]
    public void SimulateConcurrentCopying()
    {
        // Simulate the current BotPlayer pattern of creating copies for concurrent evaluation.

        var copies = new GameState[4]; // Max tokens at start

        for (int index = 0; index < copies.Length; ++index)
        {
            copies[index] = _earlyGameState.CreateCopy();
        }

        // Simulate some operations on the copies.

        for (int index = 0; index < copies.Length; ++index)
        {
            var isGameOver = copies[index].IsGameOver;
            var tokenCount = copies[index].CurrentPlayerTokens.Count;
        }
    }
}
