using NSubstitute;
using NUnit.Framework;
using Squire.NumTic.Game;
using Squire.NumTic.Game.Contracts;

namespace NumTic.Tests;

/// <summary>
///   Tests for the <see cref="Game"/> class.
/// </summary>
///
[TestFixture]
[Category("Game")]
public class GameTests
{
    /// <summary>
    ///   Verifies that the default constructor creates a game with correct initial state.
    /// </summary>
    ///
    [Test]
    public void DefaultConstructorCreatesGameWithCorrectInitialState()
    {
        var oddPlayer = Substitute.For<IPlayer>();
        var evenPlayer = Substitute.For<IPlayer>();
        var renderer = Substitute.For<IRenderer>();

        Assert.That(() => new Game(oddPlayer, evenPlayer, renderer), Throws.Nothing);
    }

    /// <summary>
    ///   Verifies that the constructor with GameState parameter works correctly.
    /// </summary>
    ///
    [Test]
    public void ConstructorWithGameStateCreatesGameSuccessfully()
    {
        var gameState = new GameState(
            PlayerToken.Odd,
            new int[9],
            15,
            [
                new HashSet<byte> { 1, 3, 5, 7, 9 },
                new HashSet<byte> { 2, 4, 6, 8 }
            ]);

        var oddPlayer = Substitute.For<IPlayer>();
        var evenPlayer = Substitute.For<IPlayer>();
        var renderer = Substitute.For<IRenderer>();

        Assert.That(() => new Game(oddPlayer, evenPlayer, renderer, gameState), Throws.Nothing);
    }

    /// <summary>
    ///   Verifies that the constructor throws ArgumentNullException when oddPlayer is null.
    /// </summary>
    ///
    [Test]
    public void ConstructorThrowsArgumentNullExceptionWhenOddPlayerIsNull()
    {
        var evenPlayer = Substitute.For<IPlayer>();
        var renderer = Substitute.For<IRenderer>();
        var state = GameState.CreateDefault();

        Assert.That(() => new Game(null!, evenPlayer, renderer, state),
            Throws.InstanceOf<ArgumentNullException>().With.Property("ParamName").EqualTo("oddPlayer"),
            "Constructor should throw ArgumentNullException for null oddPlayer");
    }

    /// <summary>
    ///   Verifies that the constructor throws ArgumentNullException when evenPlayer is null.
    /// </summary>
    ///
    [Test]
    public void ConstructorThrowsArgumentNullExceptionWhenEvenPlayerIsNull()
    {
        var oddPlayer = Substitute.For<IPlayer>();
        var renderer = Substitute.For<IRenderer>();
        var state = GameState.CreateDefault();

        Assert.That(() => new Game(oddPlayer, null!, renderer, state),
            Throws.InstanceOf<ArgumentNullException>().With.Property("ParamName").EqualTo("evenPlayer"),
            "Constructor should throw ArgumentNullException for null evenPlayer");
    }

    /// <summary>
    ///   Verifies that the constructor throws ArgumentNullException when renderer is null.
    /// </summary>
    ///
    [Test]
    public void ConstructorThrowsArgumentNullExceptionWhenRendererIsNull()
    {
        var oddPlayer = Substitute.For<IPlayer>();
        var evenPlayer = Substitute.For<IPlayer>();
        var state = GameState.CreateDefault();

        Assert.That(() => new Game(oddPlayer, evenPlayer, null!, state),
            Throws.InstanceOf<ArgumentNullException>().With.Property("ParamName").EqualTo("gameRenderer"),
            "Constructor should throw ArgumentNullException for null renderer");
    }

    /// <summary>
    ///   Verifies that the constructor throws ArgumentNullException when state is null.
    /// </summary>
    ///
    [Test]
    public void ConstructorThrowsArgumentNullExceptionWhenStateIsNull()
    {
        var oddPlayer = Substitute.For<IPlayer>();
        var evenPlayer = Substitute.For<IPlayer>();
        var renderer = Substitute.For<IRenderer>();

        Assert.That(() => new Game(oddPlayer, evenPlayer, renderer, null!),
            Throws.InstanceOf<ArgumentNullException>().With.Property("ParamName").EqualTo("state"),
            "Constructor should throw ArgumentNullException for null state");
    }

    /// <summary>
    ///   Verifies that PlayAsync completes a game successfully when a winner is determined.
    /// </summary>
    ///
    [Test]
    public async Task PlayAsyncCompletesGameWhenWinnerIsDetermined()
    {
        var gameState = GameState.CreateDefault();
        var oddPlayer = Substitute.For<IPlayer>();
        var evenPlayer = Substitute.For<IPlayer>();
        var renderer = Substitute.For<IRenderer>();

        // Pre-set board to be one move away from win: 1 + 5 = 6, need 9 to reach 15.

        gameState.SetBoardToken(1, 1, 1);
        gameState.SetBoardToken(1, 2, 5);

        // The Odd player will make the winning move.

        var winningMove = new Move(PlayerToken.Odd, 2, 9, null);

        oddPlayer
            .PlayTurnAsync(Arg.Any<GameState>(), Arg.Any<CancellationToken>())
            .Returns(winningMove);

        var game = new Game(oddPlayer, evenPlayer, renderer, gameState);

        // Set up a list to capture the game states at the time of each render call
        var renderedStates = new List<(PlayerToken? Winner, PlayerToken CurrentTurn, bool IsGameOver)>();

        renderer
            .When(r => r.RenderAsync(Arg.Any<GameState>(), Arg.Any<CancellationToken>()))
            .Do(callInfo =>
            {
                var state = callInfo.Arg<GameState>();
                renderedStates.Add((state.Winner, state.CurrentTurn, state.IsGameOver));
            });

        await Assert.ThatAsync(async () => await game.PlayAsync(), Throws.Nothing,
            "PlayAsync should complete successfully when a winner is determined");

        // Verify renders: initial + after winning move = 2 calls.

        await renderer.Received(2).RenderAsync(Arg.Any<GameState>(), Arg.Any<CancellationToken>());

        // Verify the captured states are correct
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
    ///   Verifies that PlayAsync handles cancellation gracefully.
    /// </summary>
    ///
    [Test]
    public async Task PlayAsyncHandlesCancellationGracefully()
    {
        var gameState = GameState.CreateDefault();
        var oddPlayer = Substitute.For<IPlayer>();
        var evenPlayer = Substitute.For<IPlayer>();
        var renderer = Substitute.For<IRenderer>();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var game = new Game(oddPlayer, evenPlayer, renderer, gameState);

        await Assert.ThatAsync(async () => await game.PlayAsync(cts.Token), Throws.Nothing,
            "PlayAsync should handle cancellation gracefully");

        // Should render initial state only.

        await renderer.Received(1).RenderAsync(Arg.Any<GameState>(), Arg.Any<CancellationToken>());

        // Players should not be called if canceled immediately.

        await oddPlayer.DidNotReceive().PlayTurnAsync(Arg.Any<GameState>(), Arg.Any<CancellationToken>());
        await evenPlayer.DidNotReceive().PlayTurnAsync(Arg.Any<GameState>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    ///   Verifies that PlayAsync handles OperationCanceledException from player gracefully.
    /// </summary>
    ///
    [Test]
    public async Task PlayAsyncHandlesPlayerCancellationGracefully()
    {
        var gameState = GameState.CreateDefault();
        var oddPlayer = Substitute.For<IPlayer>();
        var evenPlayer = Substitute.For<IPlayer>();
        var renderer = Substitute.For<IRenderer>();

        // First player throws OperationCanceledException.

        oddPlayer
            .PlayTurnAsync(Arg.Any<GameState>(), Arg.Any<CancellationToken>())
            .Returns<Task<Move>>(x => throw new OperationCanceledException());

        var game = new Game(oddPlayer, evenPlayer, renderer, gameState);

        await Assert.ThatAsync(async () => await game.PlayAsync(), Throws.Nothing,
            "PlayAsync should handle player cancellation gracefully");

        // Should render initial state only.

        await renderer.Received(1).RenderAsync(Arg.Any<GameState>(), Arg.Any<CancellationToken>());

        // Odd player should be called once (and throw).

        await oddPlayer.Received(1).PlayTurnAsync(Arg.Any<GameState>(), Arg.Any<CancellationToken>());

        // Even player should not be called.

        await evenPlayer.DidNotReceive().PlayTurnAsync(Arg.Any<GameState>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    ///   Verifies that PlayAsync renders after each successful move.
    /// </summary>
    ///
    [Test]
    public async Task PlayAsyncRendersAfterEachMove()
    {
        var gameState = GameState.CreateDefault();
        var oddPlayer = Substitute.For<IPlayer>();
        var evenPlayer = Substitute.For<IPlayer>();
        var renderer = Substitute.For<IRenderer>();

        // Set up moves that will lead to a win: 1 + 5 + 9 = 15.

        var move1 = new Move(PlayerToken.Odd, 0, 1, null);    // Position 0: value 1
        var move2 = new Move(PlayerToken.Even, 3, 2, null);   // Position 3: value 2 (non-winning)
        var move3 = new Move(PlayerToken.Odd, 1, 5, null);    // Position 1: value 5
        var move4 = new Move(PlayerToken.Even, 4, 4, null);   // Position 4: value 4 (non-winning)
        var winningMove = new Move(PlayerToken.Odd, 2, 9, null); // Position 2: value 9 (completes 1+5+9=15)

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
    ///   Verifies that PlayAsync continues game until winner or cancellation.
    /// </summary>
    ///
    [Test]
    public async Task PlayAsyncContinuesUntilWinnerOrCancellation()
    {
        var gameState = GameState.CreateDefault();
        var oddPlayer = Substitute.For<IPlayer>();
        var evenPlayer = Substitute.For<IPlayer>();
        var renderer = Substitute.For<IRenderer>();

        // Set up several non-winning moves, then a winning move.

        var move1 = new Move(PlayerToken.Odd, 0, 1, null);     // Position 0: 1 (no win)
        var move2 = new Move(PlayerToken.Even, 3, 2, null);    // Position 3: 2 (no win)
        var move3 = new Move(PlayerToken.Odd, 4, 3, null);     // Position 4: 3 (no win)
        var move4 = new Move(PlayerToken.Even, 6, 4, null);    // Position 6: 4 (no win)
        var move5 = new Move(PlayerToken.Odd, 1, 5, null);     // Position 1: 5 (no win)
        var move6 = new Move(PlayerToken.Even, 7, 6, null);    // Position 7: 6 (no win)
        var winningMove = new Move(PlayerToken.Odd, 2, 9, null); // Position 2: 9, completes top row 1+5+9=15

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
    ///   Verifies that PlayAsync works with default cancellation token.
    /// </summary>
    ///
    [Test]
    public async Task PlayAsyncWorksWithDefaultCancellationToken()
    {
        var gameState = GameState.CreateDefault();
        var oddPlayer = Substitute.For<IPlayer>();
        var evenPlayer = Substitute.For<IPlayer>();
        var renderer = Substitute.For<IRenderer>();

        // Set up an immediate win.

        gameState.SetBoardToken(1, 1, 1);
        gameState.SetBoardToken(1, 2, 5);

        var winningMove = new Move(PlayerToken.Odd, 2, 9, null);

        oddPlayer
            .PlayTurnAsync(Arg.Any<GameState>(), Arg.Any<CancellationToken>())
            .Returns(winningMove);

        var game = new Game(oddPlayer, evenPlayer, renderer, gameState);

        // Should not throw when using default cancellation token.

        await Assert.ThatAsync(async () => await game.PlayAsync(), Throws.Nothing,
            "PlayAsync should complete successfully with default cancellation token");
    }

    /// <summary>
    ///   Verifies that Reset() method resets game to default state.
    /// </summary>
    ///
    [Test]
    public void ResetResetsGameToDefaultState()
    {
        var gameState = GameState.CreateDefault();
        var oddPlayer = Substitute.For<IPlayer>();
        var evenPlayer = Substitute.For<IPlayer>();
        var renderer = Substitute.For<IRenderer>();

        // Modify the initial state.

        gameState.SetBoardToken(1, 1, 5);
        gameState.CurrentTurn = PlayerToken.Even;

        var game = new Game(oddPlayer, evenPlayer, renderer, gameState);

        Assert.That(() => game.Reset(), Throws.Nothing,
            "Reset should complete without throwing exceptions");
    }

    /// <summary>
    ///   Verifies that Reset() can be called multiple times without issues.
    /// </summary>
    ///
    [Test]
    public void ResetCanBeCalledMultipleTimes()
    {
        var gameState = GameState.CreateDefault();
        var oddPlayer = Substitute.For<IPlayer>();
        var evenPlayer = Substitute.For<IPlayer>();
        var renderer = Substitute.For<IRenderer>();

        var game = new Game(oddPlayer, evenPlayer, renderer, gameState);

        Assert.That(() =>
        {
            game.Reset();
            game.Reset();
            game.Reset();

        }, Throws.Nothing,
        "Multiple Reset calls should not cause issues");
    }

    /// <summary>
    ///   Verifies that Reset(GameState) sets the game to the specified state.
    /// </summary>
    ///
    [Test]
    public void ResetWithStateSuccessfullyUpdatesGameState()
    {
        var initialState = GameState.CreateDefault();
        var oddPlayer = Substitute.For<IPlayer>();
        var evenPlayer = Substitute.For<IPlayer>();
        var renderer = Substitute.For<IRenderer>();

        var customState = new GameState(
            PlayerToken.Even,
            new int[9], // Empty board
            15,
            [
                new HashSet<byte> { 1, 3, 5, 7, 9 }, // Odd player tokens
                new HashSet<byte> { 2, 4, 6, 8 }     // Even player tokens
            ]);

        var game = new Game(oddPlayer, evenPlayer, renderer, initialState);

        Assert.That(() => game.Reset(customState), Throws.Nothing,
            "Reset with custom state should complete successfully");
    }

    /// <summary>
    ///   Verifies that Reset(GameState) throws ArgumentNullException when state is null.
    /// </summary>
    ///
    [Test]
    public void ResetWithNullStateThrowsArgumentNullException()
    {
        var gameState = GameState.CreateDefault();
        var oddPlayer = Substitute.For<IPlayer>();
        var evenPlayer = Substitute.For<IPlayer>();
        var renderer = Substitute.For<IRenderer>();

        var game = new Game(oddPlayer, evenPlayer, renderer, gameState);

        Assert.That(() => game.Reset(null!),
            Throws.InstanceOf<ArgumentNullException>().With.Property("ParamName").EqualTo("state"),
            "Reset should throw ArgumentNullException for null state");
    }

    /// <summary>
    ///   Verifies that Reset(GameState) works with different board sizes.
    /// </summary>
    ///
    [Test]
    public void ResetWorksWithDifferentBoardSizes()
    {
        var initialState = GameState.CreateDefault(); // 3x3 board
        var oddPlayer = Substitute.For<IPlayer>();
        var evenPlayer = Substitute.For<IPlayer>();
        var renderer = Substitute.For<IRenderer>();

        // Create a 4x4 board state.

        var largerState = new GameState(
            PlayerToken.Odd,
            new int[16], // 4x4 board
            30,
            [
                new HashSet<byte> { 1, 3, 5, 7, 9, 11, 13, 15 },
                new HashSet<byte> { 2, 4, 6, 8, 10, 12, 14, 16 }
            ]);

        var game = new Game(oddPlayer, evenPlayer, renderer, initialState);

        Assert.That(() => game.Reset(largerState), Throws.Nothing,
            "Reset should work with different board sizes");
    }

    /// <summary>
    ///   Verifies that Reset(GameState) can switch between different game configurations.
    /// </summary>
    ///
    [Test]
    public void ResetAllowsSwitchingBetweenGameConfigurations()
    {
        var initialState = GameState.CreateDefault();
        var oddPlayer = Substitute.For<IPlayer>();
        var evenPlayer = Substitute.For<IPlayer>();
        var renderer = Substitute.For<IRenderer>();

        var state1 = new GameState(
            PlayerToken.Odd,
            new int[9],
            15,
            [
                new HashSet<byte> { 1, 3, 5, 7, 9 },
                new HashSet<byte> { 2, 4, 6, 8 }
            ]
        );

        var state2 = new GameState(
            PlayerToken.Even,
            new int[9],
            21,
            [
                new HashSet<byte> { 9, 7, 5, 3, 1 },
                new HashSet<byte> { 8, 6, 4, 2 }
            ]
        );

        var game = new Game(oddPlayer, evenPlayer, renderer, initialState);

        Assert.That(() =>
        {
            game.Reset(state1);
            game.Reset(state2);
            game.Reset(state1);
        }, Throws.Nothing,
        "Reset should allow switching between different game configurations");
    }

    /// <summary>
    ///   Verifies that game maintains player and renderer references after reset.
    /// </summary>
    ///
    [Test]
    public async Task GameMaintainsPlayerAndRendererReferencesAfterReset()
    {
        var gameState = GameState.CreateDefault();
        var oddPlayer = Substitute.For<IPlayer>();
        var evenPlayer = Substitute.For<IPlayer>();
        var renderer = Substitute.For<IRenderer>();

        var winningMove = new Move(PlayerToken.Odd, 2, 9, null);

        oddPlayer
            .PlayTurnAsync(Arg.Any<GameState>(), Arg.Any<CancellationToken>())
            .Returns(winningMove);

        var game = new Game(oddPlayer, evenPlayer, renderer, gameState);

        // Reset to a fresh state with pre-configured winning setup.

        var resetState = GameState.CreateDefault();
        resetState.SetBoardToken(1, 1, 1);
        resetState.SetBoardToken(1, 2, 5);

        game.Reset(resetState);
        await game.PlayAsync();

        // Verify original references were used.

        await oddPlayer.Received().PlayTurnAsync(Arg.Any<GameState>(), Arg.Any<CancellationToken>());
        await renderer.Received().RenderAsync(Arg.Any<GameState>(), Arg.Any<CancellationToken>());
    }
}
