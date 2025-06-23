using NUnit.Framework;
using NSubstitute;
using Squire.NumTic.Console;
using Squire.NumTic.Game;
using Squire.NumTic.Game.Contracts;

namespace Squire.NumTic.Tests;

/// <summary>
///   Tests for the <see cref="ConsolePlayer"/> class focusing on contract compliance
///   and valid game scenarios rather than UI specifics or built-in .NET functionality.
/// </summary>
///
[TestFixture]
[NonParallelizable]
[Category("Console")]
public class ConsolePlayerTests
{

    /// <summary>
    ///   Verifies that PlayTurnAsync validates the gameState parameter properly.
    /// </summary>
    ///
    [Test]
    public async Task PlayTurnAsyncWithNullGameStateThrows()
    {
        var mockGameInterface = Substitute.For<IGameInterface>();
        var player = new ConsolePlayer(mockGameInterface);

        await Assert.ThatAsync(async () => await player.PlayTurnAsync(null!),
            Throws.ArgumentNullException.With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("gameState"));
    }

    /// <summary>
    ///   Verifies that PlayTurnAsync handles cancellation appropriately.
    /// </summary>
    ///
    [Test]
    public async Task PlayTurnAsyncWithCancellationTokenThrows()
    {
        var mockGameInterface = Substitute.For<IGameInterface>();
        var player = new ConsolePlayer(mockGameInterface);
        var gameState = CreateValidGameState();

        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThatAsync(async () => await player.PlayTurnAsync(gameState, cancellationTokenSource.Token),
            Throws.TypeOf<OperationCanceledException>().With.Property(nameof(OperationCanceledException.CancellationToken)).EqualTo(cancellationTokenSource.Token));
    }

    /// <summary>
    ///   Verifies that PlayTurnAsync uses the game interface to render player information.
    /// </summary>
    ///
    [Test]
    public async Task PlayTurnAsyncRendersPlayerInformation()
    {
        var mockGameInterface = Substitute.For<IGameInterface>();
        var player = new ConsolePlayer(mockGameInterface);
        var gameState = CreateValidGameState();

        // Setup the mock to simulate user selecting token 1, row 1, column 1.

        mockGameInterface.ReadPlayerResponseAsnyc(Arg.Any<CancellationToken>())
            .Returns("1", "1", "1");

        var move = await player.PlayTurnAsync(gameState);

        // Verify that the player communicated with the game interface to show available tokens.

        await mockGameInterface
            .Received()
            .RenderPlayerTextAsync(
                Arg.Any<TextType>(),
                Arg.Is<string>(s => s.Contains("Available tokens")),
                Arg.Any<CancellationToken>());
    }

    /// <summary>
    ///   Verifies that PlayTurnAsync returns a valid Move with proper game state interactions.
    /// </summary>
    ///
    [Test]
    public async Task PlayTurnAsyncReturnsValidMoveWithMockInput()
    {
        var mockGameInterface = Substitute.For<IGameInterface>();
        var player = new ConsolePlayer(mockGameInterface);
        var gameState = CreateValidGameState();

        // Setup the mock to simulate user selecting token 1, row 1, column 1.

        mockGameInterface.ReadPlayerResponseAsnyc(Arg.Any<CancellationToken>())
            .Returns("1", "1", "1");

        var move = await player.PlayTurnAsync(gameState);

        Assert.That(move, Is.Not.Null);
        Assert.That(move.Player, Is.EqualTo(gameState.CurrentTurn));
        Assert.That(move.Token, Is.EqualTo(1));
        Assert.That(move.PositionIndex, Is.EqualTo(0)); // Row 1, Column 1 = position 0
    }

    /// <summary>
    ///   Verifies that PlayTurnAsync handles invalid input by retrying.
    /// </summary>
    ///
    [Test]
    public async Task PlayTurnAsyncHandlesInvalidInputGracefully()
    {
        var mockGameInterface = Substitute.For<IGameInterface>();
        var player = new ConsolePlayer(mockGameInterface);
        var gameState = CreateValidGameState();

        // Setup mock to first return invalid input, then valid.

        mockGameInterface.ReadPlayerResponseAsnyc(Arg.Any<CancellationToken>())
            .Returns("99", "1", "1", "1"); // invalid token 99, then valid token 1, row 1, column 1

        var move = await player.PlayTurnAsync(gameState);

        Assert.That(move, Is.Not.Null);
        Assert.That(move.Token, Is.EqualTo(1));

        // Verify error message was displayed.

        await mockGameInterface
            .Received()
            .RenderPlayerTextAsync(
                Arg.Any<TextType>(),
                Arg.Is<string>(s => s.Contains("not available")),
                Arg.Any<CancellationToken>());
    }

    /// <summary>
    ///   Verifies that PlayTurnAsync respects different board configurations.
    /// </summary>
    ///
    [Test]
    public async Task PlayTurnAsyncHandlesDifferentBoardSizes()
    {
        var mockGameInterface = Substitute.For<IGameInterface>();
        var player = new ConsolePlayer(mockGameInterface);
        var largeGameState = CreateLargerGameState(); // 4x4 board

        // Setup mock for 4x4 board - select token 1, row 4, column 4.

        mockGameInterface.ReadPlayerResponseAsnyc(Arg.Any<CancellationToken>())
            .Returns("1", "4", "4");

        var move = await player.PlayTurnAsync(largeGameState);

        Assert.That(move, Is.Not.Null);
        Assert.That(move.PositionIndex, Is.EqualTo(15)); // Row 4, Column 4 = position 15 in 4x4 board
    }

    /// <summary>
    ///   Creates a valid initial game state for testing.
    /// </summary>
    ///
    private static GameState CreateValidGameState() =>
        new GameState(
            PlayerToken.Odd,
            new int[9],
            15,
            [
                new HashSet<byte> { 1, 3, 5, 7, 9 },
                new HashSet<byte> { 2, 4, 6, 8 }
            ]);

    /// <summary>
    ///   Creates a 4x4 game state for testing larger boards.
    /// </summary>
    ///
    private static GameState CreateLargerGameState() =>
        new GameState(
            PlayerToken.Odd,
            new int[16],
            20,
            [
                new HashSet<byte> { 1, 3, 5, 7, 9, 11, 13, 15 },
                new HashSet<byte> { 2, 4, 6, 8, 10, 12, 14, 16 }
            ]);
}
