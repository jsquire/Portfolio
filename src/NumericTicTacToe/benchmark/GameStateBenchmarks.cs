using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Squire.NumTic.Benchmark;

/// <summary>
///   Benchmarks for GameState operations including copying, mutation, and winner detection.
/// </summary>
///
[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
[CategoriesColumn]
public class GameStateBenchmarks
{
    /// <summary>The fresh game state for baseline benchmarks.</summary>
    private GameState _gameState = null!;

    /// <summary>The mid-game state with several moves applied.</summary>
    private GameState _midGameState = null!;

    /// <summary>The near-end game state with many moves applied.</summary>
    private GameState _nearEndState = null!;

    /// <summary>A valid move for testing apply/undo operations.</summary>
    private Move _validMove;

    /// <summary>
    ///   Initializes the benchmark test data with various game states.
    /// </summary>
    ///
    [GlobalSetup]
    public void Setup()
    {
        // Fresh game state.

        _gameState = GameState.CreateDefault();

        // Mid-game state (some moves played).

        _midGameState = GameState.CreateDefault();
        _midGameState.ApplyMove(new Move(PlayerToken.Odd, 0, 1));
        _midGameState.ApplyMove(new Move(PlayerToken.Even, 1, 2));
        _midGameState.ApplyMove(new Move(PlayerToken.Odd, 2, 3));

        // Near-end state (many moves played).

        _nearEndState = GameState.CreateDefault();
        _nearEndState.ApplyMove(new Move(PlayerToken.Odd, 0, 1));
        _nearEndState.ApplyMove(new Move(PlayerToken.Even, 1, 2));
        _nearEndState.ApplyMove(new Move(PlayerToken.Odd, 2, 3));
        _nearEndState.ApplyMove(new Move(PlayerToken.Even, 3, 4));
        _nearEndState.ApplyMove(new Move(PlayerToken.Odd, 4, 5));
        _nearEndState.ApplyMove(new Move(PlayerToken.Even, 5, 6));

        _validMove = new Move(PlayerToken.Odd, 6, 7);
    }

    /// <summary>
    ///   Benchmarks CreateCopy operation on a fresh game state.
    /// </summary>
    ///
    /// <returns>A copy of the fresh game state.</returns>
    ///
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("CoreOperations")]
    public GameState CreateCopy_FreshGame() => _gameState.CreateCopy();

    /// <summary>
    ///   Benchmarks CreateCopy operation on a mid-game state.
    /// </summary>
    ///
    /// <returns>A copy of the mid-game state.</returns>
    ///
    [Benchmark]
    [BenchmarkCategory("CoreOperations")]
    public GameState CreateCopy_MidGame() => _midGameState.CreateCopy();

    /// <summary>
    ///   Benchmarks CreateCopy operation on a near-end game state.
    /// </summary>
    ///
    /// <returns>A copy of the near-end game state.</returns>
    ///
    [Benchmark]
    [BenchmarkCategory("CoreOperations")]
    public GameState CreateCopy_NearEnd() => _nearEndState.CreateCopy();

    /// <summary>
    ///   Benchmarks the ApplyMove operation on a game state copy.
    /// </summary>
    ///
    [Benchmark]
    [BenchmarkCategory("CoreOperations")]
    public void ApplyMove()
    {
        var copy = _nearEndState.CreateCopy();
        copy.ApplyMove(_validMove);
    }

    /// <summary>
    ///   Benchmarks the UndoMove operation after applying a move.
    /// </summary>
    ///
    [Benchmark]
    [BenchmarkCategory("CoreOperations")]
    public void UndoMove()
    {
        var copy = _nearEndState.CreateCopy();
        copy.ApplyMove(_validMove);
        copy.UndoMove(_validMove);
    }

    /// <summary>
    ///   Benchmarks the apply-undo mutation pattern used in recursive scenarios.
    /// </summary>
    ///
    [Benchmark]
    [BenchmarkCategory("CoreOperations")]
    public void ApplyUndoPattern()
    {
        // Simulate the mutation-based recursive pattern.

        _nearEndState.ApplyMove(_validMove);
        _nearEndState.UndoMove(_validMove);
    }

    /// <summary>
    ///   Benchmarks winner detection on a game state without a winner.
    /// </summary>
    ///
    /// <returns>The winner token or null if no winner exists.</returns>
    ///
    [Benchmark]
    [BenchmarkCategory("WinnerDetection")]
    public PlayerToken? ScanForWinner_NoWinner() => _midGameState.Winner;

    /// <summary>
    ///   Benchmarks winner detection on a game state with a winner.
    /// </summary>
    ///
    /// <returns>The winner token or null if no winner exists.</returns>
    ///
    [Benchmark]
    [BenchmarkCategory("WinnerDetection")]
    public PlayerToken? ScanForWinner_HasWinner()
    {
        // Create a winning state.

        var winningState = GameState.CreateDefault();

        winningState.ApplyMove(new Move(PlayerToken.Odd, 0, 1));  // (1,1)
        winningState.ApplyMove(new Move(PlayerToken.Even, 3, 2)); // (2,1)
        winningState.ApplyMove(new Move(PlayerToken.Odd, 1, 5));  // (1,2)
        winningState.ApplyMove(new Move(PlayerToken.Even, 4, 4)); // (2,2)
        winningState.ApplyMove(new Move(PlayerToken.Odd, 2, 9));  // (1,3) - Winning move: 1+5+9=15

        return winningState.Winner;
    }

    /// <summary>
    ///   Benchmarks checking if a board position is empty.
    /// </summary>
    ///
    /// <returns>True if the position is empty, false otherwise.</returns>
    ///
    [Benchmark]
    [BenchmarkCategory("BoardOperations")]
    public bool IsEmptyPosition() => _midGameState.IsEmptyPosition(2, 2);

    /// <summary>
    ///   Benchmarks getting a token value from a board position.
    /// </summary>
    ///
    /// <returns>The token value at the specified position.</returns>
    ///
    [Benchmark]
    [BenchmarkCategory("BoardOperations")]
    public int GetBoardPosition() => _midGameState.GetBoardPosition(2, 2);

    /// <summary>
    ///   Benchmarks converting a board index to row and column coordinates.
    /// </summary>
    ///
    /// <returns>The row and column coordinates for the board index.</returns>
    ///
    [Benchmark]
    [BenchmarkCategory("BoardOperations")]
    public (int Row, int Column) GetBoardCoordinates() => _midGameState.GetBoardCoordinates(4);
}
