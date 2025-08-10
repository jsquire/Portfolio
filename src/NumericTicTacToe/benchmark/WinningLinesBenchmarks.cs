using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Squire.NumTic.Benchmark;

/// <summary>
///   Benchmarks for winning lines operations focusing on the impact of 1D vs 2D array structures.
///   These benchmarks measure the performance of FindWinningMove across various game scenarios.
/// </summary>
///
[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
[CategoriesColumn]
public class WinningLinesBenchmarks
{
    /// <summary>Empty board state for baseline measurements.</summary>
    private GameState _emptyBoard = null!;

    /// <summary>Mid-game state with several moves played.</summary>
    private GameState _midGameState = null!;

    /// <summary>Near-win state where a winning move is available.</summary>
    private GameState _nearWinState = null!;

    /// <summary>Full board state for maximum computational complexity.</summary>
    private GameState _fullBoardState = null!;

    /// <summary>Competitive state where both players have opportunities.</summary>
    private GameState _competitiveState = null!;

    /// <summary>
    ///   Initializes the benchmark test data with various game states.
    /// </summary>
    ///
    [GlobalSetup]
    public void Setup()
    {
        // Empty board - baseline performance.

        _emptyBoard = GameState.CreateDefault();

        // Mid-game state - realistic scenario with some moves played.

        _midGameState = new GameState(
            PlayerToken.Even,
            new byte[9] { 1, 2, 0, 0, 3, 0, 0, 0, 0 },
            15,
            [
                new HashSet<byte> { 5, 7, 9 },      // Odd tokens available (1,3 used)
                new HashSet<byte> { 4, 6, 8 }       // Even tokens available (2 used)
            ]);

        // Near-win state - odd player can win on next move.

        _nearWinState = new GameState(
            PlayerToken.Odd,
            new byte[9] { 1, 5, 0, 2, 4, 0, 0, 0, 0 },
            15,
            [
                new HashSet<byte> { 3, 7, 9 },      // Odd tokens available (1,5 used)
                new HashSet<byte> { 6, 8 }          // Even tokens available (2,4 used)
            ]);
        // Odd can play 9 at position 2 for 1+5+9=15 win (top row).

        // Competitive state - both players have winning opportunities.

        _competitiveState = new GameState(
            PlayerToken.Odd,
            new byte[9] { 1, 2, 0, 0, 5, 0, 0, 8, 0 },
            15,
            [
                new HashSet<byte> { 3, 7, 9 },      // Odd tokens available (1,5 used)
                new HashSet<byte> { 4, 6 }          // Even tokens available (2,8 used)
            ]);
        // Odd can win with 9 at position 8: 1+5+9=15 (diagonal).

        // Full board state - only leave a few spots empty.

        _fullBoardState = new GameState(
            PlayerToken.Even,
            new byte[9] { 1, 2, 3, 4, 5, 6, 7, 0, 0 },
            15,
            [
                new HashSet<byte> { 9 },            // Odd tokens available (1,3,5,7 used)
                new HashSet<byte> { 8 }             // Even tokens available (2,4,6 used)
            ]);
        // Leave positions 7 and 8 empty for testing.
    }

    /// <summary>
    ///   Benchmarks FindWinningMove on an empty board - baseline performance.
    /// </summary>
    ///
    /// <returns>The winning move for the odd player, or null if no immediate win.</returns>
    ///
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("FindWinningMove")]
    public Move? FindWinningMove_EmptyBoard() => _emptyBoard.FindWinningMove(PlayerToken.Odd);

    /// <summary>
    ///   Benchmarks FindWinningMove in a mid-game scenario.
    /// </summary>
    ///
    /// <returns>The winning move for the odd player, or null if no immediate win.</returns>
    ///
    [Benchmark]
    [BenchmarkCategory("FindWinningMove")]
    public Move? FindWinningMove_MidGame() => _midGameState.FindWinningMove(PlayerToken.Odd);

    /// <summary>
    ///   Benchmarks FindWinningMove when a winning move is available.
    /// </summary>
    ///
    /// <returns>The winning move for the odd player, or null if no immediate win.</returns>
    ///
    [Benchmark]
    [BenchmarkCategory("FindWinningMove")]
    public Move? FindWinningMove_NearWin() => _nearWinState.FindWinningMove(PlayerToken.Odd);

    /// <summary>
    ///   Benchmarks FindWinningMove on a nearly full board.
    /// </summary>
    ///
    /// <returns>The winning move for the odd player, or null if no immediate win.</returns>
    ///
    [Benchmark]
    [BenchmarkCategory("FindWinningMove")]
    public Move? FindWinningMove_FullBoard() => _fullBoardState.FindWinningMove(PlayerToken.Odd);

    /// <summary>
    ///   Benchmarks FindWinningMove in a competitive scenario.
    /// </summary>
    ///
    /// <returns>The winning move for the odd player, or null if no immediate win.</returns>
    ///
    [Benchmark]
    [BenchmarkCategory("FindWinningMove")]
    public Move? FindWinningMove_Competitive() => _competitiveState.FindWinningMove(PlayerToken.Odd);

    /// <summary>
    ///   Benchmarks FindWinningMove for the even player.
    /// </summary>
    ///
    /// <returns>The winning move for the even player, or null if no immediate win.</returns>
    ///
    [Benchmark]
    [BenchmarkCategory("FindWinningMove")]
    public Move? FindWinningMove_EvenPlayer() => _competitiveState.FindWinningMove(PlayerToken.Even);

    /// <summary>
    ///   Benchmarks repeated calls to FindWinningMove to test cache locality.
    /// </summary>
    ///
    /// <returns>The result of the last FindWinningMove call.</returns>
    ///
    [Benchmark]
    [BenchmarkCategory("CacheLocality")]
    public Move? FindWinningMove_RepeatedCalls()
    {
        Move? result = null;
        for (int i = 0; i < 100; i++)
        {
            result = _midGameState.FindWinningMove(PlayerToken.Odd);
        }
        return result;
    }

    /// <summary>
    ///   Benchmarks repeated calls to ScanForWinner for comparison.
    /// </summary>
    ///
    /// <returns>The result of the last ScanForWinner call.</returns>
    ///
    [Benchmark]
    [BenchmarkCategory("CacheLocality")]
    public PlayerToken? ScanForWinner_RepeatedCalls()
    {
        PlayerToken? result = null;
        for (int i = 0; i < 100; i++)
        {
            result = _midGameState.ScanForWinner();
        }
        return result;
    }

    /// <summary>
    ///   Comparison benchmark between ScanForWinner and FindWinningMove.
    /// </summary>
    ///
    /// <returns>The result of ScanForWinner.</returns>
    ///
    [Benchmark]
    [BenchmarkCategory("Comparison")]
    public PlayerToken? ScanForWinner_Comparison() => _nearWinState.ScanForWinner();
}
