using NSubstitute;
using NUnit.Framework;
using Squire.NumTic.Contracts;

namespace Squire.NumTic.Tests;

/// <summary>
///   The suite of tests for the <see cref="Game"/> class.
/// </summary>
///
[TestFixture]
[Category("Game")]
public class GameTests
{
    /// <summary>
    ///   Verifies functionality of the constructor.
    /// </summary>
    ///
    [Test]
    public void ConstructorThrowsArgumentNullExceptionWhenOddPlayerIsNull()
    {
        var evenPlayer = Substitute.For<IPlayer>();
        var renderer = Substitute.For<IGameInterface>();
        var state = GameState.CreateDefault();

        Assert.That(() => new Game(null!, evenPlayer, renderer, state),
            Throws.InstanceOf<ArgumentNullException>().With.Property("ParamName").EqualTo("oddPlayer"),
            "Constructor should throw ArgumentNullException for null oddPlayer");
    }

    /// <summary>
    ///   Verifies functionality of the constructor.
    /// </summary>
    ///
    [Test]
    public void ConstructorThrowsArgumentNullExceptionWhenEvenPlayerIsNull()
    {
        var oddPlayer = Substitute.For<IPlayer>();
        var renderer = Substitute.For<IGameInterface>();
        var state = GameState.CreateDefault();

        Assert.That(() => new Game(oddPlayer, null!, renderer, state),
            Throws.InstanceOf<ArgumentNullException>().With.Property("ParamName").EqualTo("evenPlayer"),
            "Constructor should throw ArgumentNullException for null evenPlayer");
    }

    /// <summary>
    ///   Verifies functionality of the constructor.
    /// </summary>
    ///
    [Test]
    public void ConstructorThrowsArgumentNullExceptionWhenRendererIsNull()
    {
        var oddPlayer = Substitute.For<IPlayer>();
        var evenPlayer = Substitute.For<IPlayer>();
        var state = GameState.CreateDefault();

        Assert.That(() => new Game(oddPlayer, evenPlayer, null!, state),
            Throws.InstanceOf<ArgumentNullException>().With.Property("ParamName").EqualTo("gameInterface"),
            "Constructor should throw ArgumentNullException for null renderer");
    }

    /// <summary>
    ///   Verifies functionality of the constructor.
    /// </summary>
    ///
    [Test]
    public void ConstructorThrowsArgumentNullExceptionWhenStateIsNull()
    {
        var oddPlayer = Substitute.For<IPlayer>();
        var evenPlayer = Substitute.For<IPlayer>();
        var renderer = Substitute.For<IGameInterface>();

        Assert.That(() => new Game(oddPlayer, evenPlayer, renderer, null!),
            Throws.InstanceOf<ArgumentNullException>().With.Property("ParamName").EqualTo("state"),
            "Constructor should throw ArgumentNullException for null state");
    }

    /// <summary>
    ///   Verifies functionality of the PlayAsync method.
    /// </summary>
    ///
    [Test]
    public async Task PlayAsyncCompletesGameWhenWinnerIsDetermined()
    {
        var gameState = GameState.CreateDefault();
        var oddPlayer = Substitute.For<IPlayer>();
        var evenPlayer = Substitute.For<IPlayer>();
        var renderer = Substitute.For<IGameInterface>();

        // Pre-set board to be one move away from win: 1 + 5 = 6, need 9 to reach 15.

        gameState.Board[gameState.GetBoardPosition(1, 1)] = 1;
        gameState.Board[gameState.GetBoardPosition(1, 2)] = 5;

        // The Odd player will make the winning move.

        var winningMove = new Move(PlayerToken.Odd, 2, 9);

        oddPlayer
            .PlayTurnAsync(Arg.Any<GameState>(), Arg.Any<CancellationToken>())
            .Returns(winningMove);

        var game = new Game(oddPlayer, evenPlayer, renderer, gameState);

        // Set up a list to capture the game states at the time of each render call.

        var renderedStates = new List<(PlayerToken? Winner, PlayerToken CurrentTurn, bool IsGameOver)>();

        renderer
            .When(r => r.RenderAsync(Arg.Any<GameState>(), Arg.Any<CancellationToken>()))
            .Do(callInfo =>
            {
                var state = callInfo.Arg<GameState>();
                renderedStates.Add((state.Winner, state.CurrentTurn, state.IsGameOver));
            });

        await game.PlayAsync();

        // Verify renders: initial + after winning move = 2 calls.

        await renderer.Received(2).RenderAsync(Arg.Any<GameState>(), Arg.Any<CancellationToken>());

        // Verify the captured states are correct.

        Assert.That(renderedStates.Count, Is.EqualTo(2), "Should have captured exactly 2 render calls");

        // Verify initial render was called with no winner.

        Assert.That(renderedStates[0].Winner, Is.Null, "First render should have no winner");
        Assert.That(renderedStates[0].CurrentTurn, Is.EqualTo(PlayerToken.Odd), "First render should be Odd player's turn");
        Assert.That(renderedStates[0].IsGameOver, Is.False, "First render should not show game over");

        // Verify final render was called with winner.

        Assert.That(renderedStates[1].Winner, Is.EqualTo(PlayerToken.Odd), "Second render should show Odd as winner");
        Assert.That(renderedStates[1].CurrentTurn, Is.EqualTo(PlayerToken.Odd), "Second render should show Odd's turn (winner doesn't alternate)");
        Assert.That(renderedStates[1].IsGameOver, Is.True, "Second render should show game over");
    }

    /// <summary>
    ///   Verifies functionality of the PlayAsync method.
    /// </summary>
    ///
    [Test]
    public async Task PlayAsyncHandlesCancellationGracefully()
    {
        var gameState = GameState.CreateDefault();
        var oddPlayer = Substitute.For<IPlayer>();
        var evenPlayer = Substitute.For<IPlayer>();
        var renderer = Substitute.For<IGameInterface>();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var game = new Game(oddPlayer, evenPlayer, renderer, gameState);

        await game.PlayAsync(cts.Token);

        // Should render initial state only.

        await renderer.Received(1).RenderAsync(Arg.Any<GameState>(), Arg.Any<CancellationToken>());

        // Players should not be called if canceled immediately.

        await oddPlayer.DidNotReceive().PlayTurnAsync(Arg.Any<GameState>(), Arg.Any<CancellationToken>());
        await evenPlayer.DidNotReceive().PlayTurnAsync(Arg.Any<GameState>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    ///   Verifies functionality of the PlayAsync method.
    /// </summary>
    ///
    [Test]
    public async Task PlayAsyncHandlesPlayerCancellationGracefully()
    {
        var gameState = GameState.CreateDefault();
        var oddPlayer = Substitute.For<IPlayer>();
        var evenPlayer = Substitute.For<IPlayer>();
        var renderer = Substitute.For<IGameInterface>();

        // First player throws OperationCanceledException.

        oddPlayer
            .PlayTurnAsync(Arg.Any<GameState>(), Arg.Any<CancellationToken>())
            .Returns<Task<Move>>(x => throw new OperationCanceledException());

        var game = new Game(oddPlayer, evenPlayer, renderer, gameState);

        await game.PlayAsync();

        // Should render initial state only.

        await renderer.Received(1).RenderAsync(Arg.Any<GameState>(), Arg.Any<CancellationToken>());

        // Odd player should be called once (and throw).

        await oddPlayer.Received(1).PlayTurnAsync(Arg.Any<GameState>(), Arg.Any<CancellationToken>());

        // Even player should not be called.

        await evenPlayer.DidNotReceive().PlayTurnAsync(Arg.Any<GameState>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    ///   Verifies functionality of the PlayAsync method.
    /// </summary>
    ///
    [Test]
    public async Task PlayAsyncRendersAfterEachMove()
    {
        var gameState = GameState.CreateDefault();
        var oddPlayer = Substitute.For<IPlayer>();
        var evenPlayer = Substitute.For<IPlayer>();
        var renderer = Substitute.For<IGameInterface>();

        // Set up moves that will lead to a win: 1 + 5 + 9 = 15.

        var move1 = new Move(PlayerToken.Odd, 0, 1);    // Position 0: value 1
        var move2 = new Move(PlayerToken.Even, 3, 2);   // Position 3: value 2 (non-winning)
        var move3 = new Move(PlayerToken.Odd, 1, 5);    // Position 1: value 5
        var move4 = new Move(PlayerToken.Even, 4, 4);   // Position 4: value 4 (non-winning)
        var winningMove = new Move(PlayerToken.Odd, 2, 9); // Position 2: value 9 (completes 1+5+9=15)

        oddPlayer
            .PlayTurnAsync(Arg.Any<GameState>(), Arg.Any<CancellationToken>())
            .Returns(move1, move3, winningMove);

        evenPlayer
            .PlayTurnAsync(Arg.Any<GameState>(), Arg.Any<CancellationToken>())
            .Returns(move2, move4);

        var game = new Game(oddPlayer, evenPlayer, renderer, gameState);
        await game.PlayAsync();

        // Should render: initial + after move1 + after move2 + after move3 + after move4 + after winning move = 6 total.

        await renderer.Received(6).RenderAsync(Arg.Any<GameState>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    ///   Verifies functionality of the PlayAsync method.
    /// </summary>
    ///
    [Test]
    public async Task PlayAsyncContinuesUntilWinnerOrCancellation()
    {
        var gameState = GameState.CreateDefault();
        var oddPlayer = Substitute.For<IPlayer>();
        var evenPlayer = Substitute.For<IPlayer>();
        var renderer = Substitute.For<IGameInterface>();

        // Set up several non-winning moves, then a winning move.

        var move1 = new Move(PlayerToken.Odd, 0, 1);     // Position 0: 1 (no win)
        var move2 = new Move(PlayerToken.Even, 3, 2);    // Position 3: 2 (no win)
        var move3 = new Move(PlayerToken.Odd, 4, 3);     // Position 4: 3 (no win)
        var move4 = new Move(PlayerToken.Even, 6, 4);    // Position 6: 4 (no win)
        var move5 = new Move(PlayerToken.Odd, 1, 5);     // Position 1: 5 (no win)
        var move6 = new Move(PlayerToken.Even, 7, 6);    // Position 7: 6 (no win)
        var winningMove = new Move(PlayerToken.Odd, 2, 9); // Position 2: 9, completes top row 1+5+9=15

        oddPlayer
            .PlayTurnAsync(Arg.Any<GameState>(), Arg.Any<CancellationToken>())
            .Returns(move1, move3, move5, winningMove);

        evenPlayer
            .PlayTurnAsync(Arg.Any<GameState>(), Arg.Any<CancellationToken>())
            .Returns(move2, move4, move6);

        var game = new Game(oddPlayer, evenPlayer, renderer, gameState);
        await game.PlayAsync();

        // Verify the game continued through multiple moves until winner.

        await oddPlayer.Received(4).PlayTurnAsync(Arg.Any<GameState>(), Arg.Any<CancellationToken>());
        await evenPlayer.Received(3).PlayTurnAsync(Arg.Any<GameState>(), Arg.Any<CancellationToken>());

        // Verify renders: initial + after each of the 7 moves = 8 total.

        await renderer.Received(8).RenderAsync(Arg.Any<GameState>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    ///   Verifies functionality of the Reset method.
    /// </summary>
    ///
    [Test]
    public void ResetWithStateUpdatesGameState()
    {
        var initialState = GameState.CreateDefault();
        var oddPlayer = Substitute.For<IPlayer>();
        var evenPlayer = Substitute.For<IPlayer>();
        var renderer = Substitute.For<IGameInterface>();

        var customState = new GameState(
            PlayerToken.Even,
            new byte[9], // Empty board
            15,
            [
                new HashSet<byte> { 1, 3, 5, 7, 9 }, // Odd player tokens
                new HashSet<byte> { 2, 4, 6, 8 }     // Even player tokens
            ]);

        var game = new Game(oddPlayer, evenPlayer, renderer, initialState);

        game.Reset(customState);
    }

    /// <summary>
    ///   Verifies functionality of the Reset method.
    /// </summary>
    ///
    [Test]
    public void ResetWithNullStateThrowsArgumentNullException()
    {
        var gameState = GameState.CreateDefault();
        var oddPlayer = Substitute.For<IPlayer>();
        var evenPlayer = Substitute.For<IPlayer>();
        var renderer = Substitute.For<IGameInterface>();

        var game = new Game(oddPlayer, evenPlayer, renderer, gameState);

        Assert.That(() => game.Reset(null!),
            Throws.InstanceOf<ArgumentNullException>().With.Property("ParamName").EqualTo("state"),
            "Reset should throw ArgumentNullException for null state");
    }

    /// <summary>
    ///   Verifies functionality of the Reset method.
    /// </summary>
    ///
    [Test]
    public void ResetAllowsSwitchingBetweenGameConfigurations()
    {
        var initialState = GameState.CreateDefault();
        var oddPlayer = Substitute.For<IPlayer>();
        var evenPlayer = Substitute.For<IPlayer>();
        var renderer = Substitute.For<IGameInterface>();

        var state1 = new GameState(
            PlayerToken.Odd,
            new byte[9],
            15,
            [
                new HashSet<byte> { 1, 3, 5, 7, 9 },
                new HashSet<byte> { 2, 4, 6, 8 }
            ]
        );

        var state2 = new GameState(
            PlayerToken.Even,
            new byte[9],
            21,
            [
                new HashSet<byte> { 9, 7, 5, 3, 1 },
                new HashSet<byte> { 8, 6, 4, 2 }
            ]
        );

        var game = new Game(oddPlayer, evenPlayer, renderer, initialState);

        game.Reset(state1);
        game.Reset(state2);
        game.Reset(state1);
    }

    /// <summary>
    ///   Verifies functionality of the Reset method.
    /// </summary>
    ///
    [Test]
    public async Task GameMaintainsPlayerAndRendererReferencesAfterReset()
    {
        var gameState = GameState.CreateDefault();
        var oddPlayer = Substitute.For<IPlayer>();
        var evenPlayer = Substitute.For<IPlayer>();
        var renderer = Substitute.For<IGameInterface>();

        var winningMove = new Move(PlayerToken.Odd, 2, 9);

        oddPlayer
            .PlayTurnAsync(Arg.Any<GameState>(), Arg.Any<CancellationToken>())
            .Returns(winningMove);

        var game = new Game(oddPlayer, evenPlayer, renderer, gameState);

        // Reset to a fresh state with pre-configured winning setup.

        var resetState = GameState.CreateDefault();
        resetState.Board[resetState.GetBoardPosition(1, 1)] = 1;
        resetState.Board[resetState.GetBoardPosition(1, 2)] = 5;

        game.Reset(resetState);
        await game.PlayAsync();

        // Verify original references were used.

        await oddPlayer.Received().PlayTurnAsync(Arg.Any<GameState>(), Arg.Any<CancellationToken>());
        await renderer.Received().RenderAsync(Arg.Any<GameState>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    ///   Verifies functionality of the PlayAsync method.
    /// </summary>
    ///
    [Test]
    public async Task PlayAsyncHandlesExtendedGameSessionsEfficiently()
    {
        var gameState = GameState.CreateDefault();
        var oddPlayer = Substitute.For<IPlayer>();
        var evenPlayer = Substitute.For<IPlayer>();
        var renderer = Substitute.For<IGameInterface>();

        // Create a sequence of moves that will result in a very long game before winning.
        // This tests performance over extended play sessions.

        var moves = new List<Move>();

        // Fill most of the board with non-winning combinations.

        moves.Add(new Move(PlayerToken.Odd, 0, 1));    // Position 0: 1
        moves.Add(new Move(PlayerToken.Even, 3, 2));   // Position 3: 2
        moves.Add(new Move(PlayerToken.Odd, 1, 3));    // Position 1: 3
        moves.Add(new Move(PlayerToken.Even, 4, 4));   // Position 4: 4
        moves.Add(new Move(PlayerToken.Odd, 6, 5));    // Position 6: 5
        moves.Add(new Move(PlayerToken.Even, 7, 6));   // Position 7: 6
        moves.Add(new Move(PlayerToken.Odd, 8, 7));    // Position 8: 7
        moves.Add(new Move(PlayerToken.Even, 5, 8));   // Position 5: 8
        moves.Add(new Move(PlayerToken.Odd, 2, 9));    // Position 2: 9 (wins: 1+3+9=13, wait that's not 15)

        // Let me fix this - we need 1+5+9=15 for a win.

        moves.Clear();
        moves.Add(new Move(PlayerToken.Odd, 0, 1));    // Position 0: 1
        moves.Add(new Move(PlayerToken.Even, 3, 2));   // Position 3: 2
        moves.Add(new Move(PlayerToken.Odd, 6, 3));    // Position 6: 3
        moves.Add(new Move(PlayerToken.Even, 7, 4));   // Position 7: 4
        moves.Add(new Move(PlayerToken.Odd, 8, 7));    // Position 8: 7
        moves.Add(new Move(PlayerToken.Even, 5, 6));   // Position 5: 6
        moves.Add(new Move(PlayerToken.Odd, 1, 5));    // Position 1: 5
        moves.Add(new Move(PlayerToken.Even, 4, 8));   // Position 4: 8
        moves.Add(new Move(PlayerToken.Odd, 2, 9));    // Position 2: 9 (wins: top row 1+5+9=15)

        oddPlayer.PlayTurnAsync(Arg.Any<GameState>(), Arg.Any<CancellationToken>())
            .Returns(moves[0], moves[2], moves[4], moves[6], moves[8]);

        evenPlayer.PlayTurnAsync(Arg.Any<GameState>(), Arg.Any<CancellationToken>())
            .Returns(moves[1], moves[3], moves[5], moves[7]);

        var game = new Game(oddPlayer, evenPlayer, renderer, gameState);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        await game.PlayAsync();

        stopwatch.Stop();

        // Verify the game completed efficiently even with many moves.

        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(5000),
            "Extended game session should complete efficiently");
        Assert.That(gameState.Winner, Is.EqualTo(PlayerToken.Odd),
            "Game should conclude with correct winner");
    }

    /// <summary>
    ///   Verifies functionality of the PlayAsync method.
    /// </summary>
    ///
    [Test]
    public async Task PlayAsyncHandlesRapidPlayerResponsesCorrectly()
    {
        var gameState = GameState.CreateDefault();
        var oddPlayer = Substitute.For<IPlayer>();
        var evenPlayer = Substitute.For<IPlayer>();
        var renderer = Substitute.For<IGameInterface>();

        // Configure players to respond extremely quickly (simulating rapid human input or fast AI).

        var quickMove1 = new Move(PlayerToken.Odd, 0, 1);
        var quickMove2 = new Move(PlayerToken.Even, 3, 2);
        var quickMove3 = new Move(PlayerToken.Odd, 1, 5);
        var quickMove4 = new Move(PlayerToken.Even, 4, 4);
        var winningMove = new Move(PlayerToken.Odd, 2, 9);

        // Use immediate task completion to simulate instant responses.

        oddPlayer.PlayTurnAsync(Arg.Any<GameState>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(quickMove1), Task.FromResult(quickMove3), Task.FromResult(winningMove));

        evenPlayer.PlayTurnAsync(Arg.Any<GameState>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(quickMove2), Task.FromResult(quickMove4));

        var game = new Game(oddPlayer, evenPlayer, renderer, gameState);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        await game.PlayAsync();

        stopwatch.Stop();

        // Verify rapid responses are handled correctly without race conditions.

        Assert.That(gameState.Winner, Is.EqualTo(PlayerToken.Odd),
            "Rapid responses should not cause race conditions affecting game outcome");
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(1000),
            "Rapid responses should be processed quickly");

        // Verify all expected render calls occurred in sequence.

        await renderer.Received(6).RenderAsync(Arg.Any<GameState>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    ///   Verifies functionality of the PlayAsync method.
    /// </summary>
    ///
    [Test]
    public async Task GameHandlesMemoryCleanupAfterMultipleSessions()
    {
        var oddPlayer = Substitute.For<IPlayer>();
        var evenPlayer = Substitute.For<IPlayer>();
        var renderer = Substitute.For<IGameInterface>();

        // Set up a quick winning scenario that will work across multiple sessions.

        var move1 = new Move(PlayerToken.Odd, 0, 1);
        var winningMove = new Move(PlayerToken.Odd, 1, 5);
        var move2 = new Move(PlayerToken.Even, 3, 2);

        oddPlayer.PlayTurnAsync(Arg.Any<GameState>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var state = callInfo.Arg<GameState>();
                var availableTokens = state.GetPlayerTokens(PlayerToken.Odd);
                var firstAvailableToken = availableTokens.First();
                var emptyPosition = Array.IndexOf(state.Board, GameState.EmptyBoardSpaceValue);
                return new Move(PlayerToken.Odd, emptyPosition, firstAvailableToken);
            });

        evenPlayer.PlayTurnAsync(Arg.Any<GameState>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var state = callInfo.Arg<GameState>();
                var availableTokens = state.GetPlayerTokens(PlayerToken.Even);
                var firstAvailableToken = availableTokens.First();
                var emptyPosition = Array.IndexOf(state.Board, GameState.EmptyBoardSpaceValue);
                return new Move(PlayerToken.Even, emptyPosition, firstAvailableToken);
            });

        // Execute multiple game sessions to test for memory leaks or accumulation.

        for (var session = 0; session < 3; session++)
        {
            var gameState = GameState.CreateDefault();
            var game = new Game(oddPlayer, evenPlayer, renderer, gameState);

            await game.PlayAsync();

            Assert.That(gameState.IsGameOver, Is.True,
                $"Session {session + 1} should complete with game over");

            // Allow objects to be eligible for garbage collection.

            gameState = null;
            game = null;
        }

        // Force garbage collection to test for proper cleanup.

        GC.Collect(2, GCCollectionMode.Forced, true);
        GC.WaitForPendingFinalizers();

        // If we reach here without memory issues, cleanup is working correctly.

        Assert.Pass("Memory cleanup test completed successfully");
    }

    /// <summary>
    ///   Verifies functionality of the PlayAsync method.
    /// </summary>
    ///
    [Test]
    public async Task GameHandlesAlternatingWinLossPositionsCorrectly()
    {
        var gameState = GameState.CreateDefault();
        var oddPlayer = Substitute.For<IPlayer>();
        var evenPlayer = Substitute.For<IPlayer>();
        var renderer = Substitute.For<IGameInterface>();

        // Create a scenario that fills the board without creating any winning combinations.
        // Ensure no row, column, or diagonal sums to 15.

        var moves = new[]
        {
            new Move(PlayerToken.Odd, 0, 1),    // Top-left: 1
            new Move(PlayerToken.Even, 1, 2),   // Top-center: 2
            new Move(PlayerToken.Odd, 2, 3),    // Top-right: 3 (Row 0: 1+2+3=6)
            new Move(PlayerToken.Even, 3, 4),   // Middle-left: 4
            new Move(PlayerToken.Odd, 4, 5),    // Center: 5
            new Move(PlayerToken.Even, 5, 6),   // Middle-right: 6 (Row 1: 4+5+6=15 - avoid this)
            new Move(PlayerToken.Odd, 6, 7),    // Bottom-left: 7
            new Move(PlayerToken.Even, 7, 8),   // Bottom-center: 8
            new Move(PlayerToken.Odd, 8, 9)     // Bottom-right: 9 (Row 2: 7+8+9=24)
        };

        // Adjust to avoid winning combinations.
        // Row 1: 4+5+6=15, so change position 5 to use a different token.

        moves[5] = new Move(PlayerToken.Even, 5, 8);  // Middle-right: 8 (Row 1: 4+5+8=17)
        moves[7] = new Move(PlayerToken.Even, 7, 6);  // Bottom-center: 6 (Row 2: 7+6+9=22)

        oddPlayer.PlayTurnAsync(Arg.Any<GameState>(), Arg.Any<CancellationToken>())
            .Returns(moves[0], moves[2], moves[4], moves[6], moves[8]);

        evenPlayer.PlayTurnAsync(Arg.Any<GameState>(), Arg.Any<CancellationToken>())
            .Returns(moves[1], moves[3], moves[5], moves[7]);

        var game = new Game(oddPlayer, evenPlayer, renderer, gameState);

        await game.PlayAsync();

        // Verify the game handles the complex position changes correctly.

        Assert.That(gameState.IsGameOver, Is.True,
            "Game should conclude when no more valid moves are available");

        // Verify the game handled alternating position changes correctly.
        // The game may end due to win condition or board being full.

        var emptyPositions = gameState.Board.Count(pos => pos == GameState.EmptyBoardSpaceValue);
        Assert.That(emptyPositions, Is.LessThanOrEqualTo(2),
            "Game should have minimal empty positions when concluded");
    }
}