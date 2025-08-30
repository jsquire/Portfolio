using NSubstitute;
using NUnit.Framework;
using Squire.NumTic.Console;
using Squire.NumTic.Contracts;

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

        // Verify that the player communicated with the game interface to prompt for token selection.

        await mockGameInterface
            .Received()
            .RenderPlayerTextAsync(
                Arg.Any<TextType>(),
                Arg.Is<string>(s => s.Contains("Select a token to place")),
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

        // Setup mock for 4x4 board - select token 1, position 16 (last position in 4x4 board).

        mockGameInterface.ReadPlayerResponseAsnyc(Arg.Any<CancellationToken>())
            .Returns("1", "16");

        var move = await player.PlayTurnAsync(largeGameState);
        Assert.That(move.PositionIndex, Is.EqualTo(15)); // Position 16 = 0-based index 15 in 4x4 board
    }

    /// <summary>
    ///   Verifies that PlayTurnAsync handles non-numeric token input gracefully.
    /// </summary>
    ///
    [Test]
    public async Task PlayTurnAsyncHandlesNonNumericTokenInput()
    {
        var mockGameInterface = Substitute.For<IGameInterface>();
        var player = new ConsolePlayer(mockGameInterface);
        var gameState = CreateValidGameState();

        // Setup mock to return non-numeric token input, then valid.

        mockGameInterface.ReadPlayerResponseAsnyc(Arg.Any<CancellationToken>())
            .Returns("abc", "1", "1", "1");

        var move = await player.PlayTurnAsync(gameState);

        Assert.That(move.Token, Is.EqualTo(1), "Should eventually accept valid token after non-numeric input");

        // Verify that error message was displayed for non-numeric input.

        await mockGameInterface
            .Received()
            .RenderPlayerTextAsync(
                TextType.Error,
                Arg.Is<string>(s => s.Contains("valid")),
                Arg.Any<CancellationToken>());
    }

    /// <summary>
    ///   Verifies that PlayTurnAsync handles negative token input gracefully.
    /// </summary>
    ///
    [Test]
    public async Task PlayTurnAsyncHandlesNegativeTokenInput()
    {
        var mockGameInterface = Substitute.For<IGameInterface>();
        var player = new ConsolePlayer(mockGameInterface);
        var gameState = CreateValidGameState();

        // Setup mock to return negative token input, then valid.

        mockGameInterface.ReadPlayerResponseAsnyc(Arg.Any<CancellationToken>())
            .Returns("-5", "1", "1", "1");

        var move = await player.PlayTurnAsync(gameState);

        Assert.That(move.Token, Is.EqualTo(1), "Should eventually accept valid token after negative input");
    }

    /// <summary>
    ///   Verifies that PlayTurnAsync handles empty string input gracefully.
    /// </summary>
    ///
    [Test]
    public async Task PlayTurnAsyncHandlesEmptyStringInput()
    {
        var mockGameInterface = Substitute.For<IGameInterface>();
        var player = new ConsolePlayer(mockGameInterface);
        var gameState = CreateValidGameState();

        // Setup mock to return empty string, then valid input.

        mockGameInterface.ReadPlayerResponseAsnyc(Arg.Any<CancellationToken>())
            .Returns("", "1", "1", "1");

        var move = await player.PlayTurnAsync(gameState);

        Assert.That(move.Token, Is.EqualTo(1), "Should eventually accept valid token after empty input");
    }

    /// <summary>
    ///   Verifies that PlayTurnAsync handles extremely large token values gracefully.
    /// </summary>
    ///
    [Test]
    public async Task PlayTurnAsyncHandlesExtremelyLargeTokenInput()
    {
        var mockGameInterface = Substitute.For<IGameInterface>();
        var player = new ConsolePlayer(mockGameInterface);
        var gameState = CreateValidGameState();

        // Setup mock to return extremely large token, then valid.

        mockGameInterface.ReadPlayerResponseAsnyc(Arg.Any<CancellationToken>())
            .Returns("999", "1", "1", "1");

        var move = await player.PlayTurnAsync(gameState);

        Assert.That(move.Token, Is.EqualTo(1), "Should eventually accept valid token after extremely large input");
    }

    /// <summary>
    ///   Verifies that PlayTurnAsync handles decimal number input gracefully.
    /// </summary>
    ///
    [Test]
    public async Task PlayTurnAsyncHandlesDecimalNumberInput()
    {
        var mockGameInterface = Substitute.For<IGameInterface>();
        var player = new ConsolePlayer(mockGameInterface);
        var gameState = CreateValidGameState();

        // Setup mock to return decimal number, then valid input.

        mockGameInterface.ReadPlayerResponseAsnyc(Arg.Any<CancellationToken>())
            .Returns("1.5", "1", "1", "1");

        var move = await player.PlayTurnAsync(gameState);

        Assert.That(move.Token, Is.EqualTo(1), "Should eventually accept valid token after decimal input");
    }

    /// <summary>
    ///   Verifies that PlayTurnAsync handles Unicode digit input gracefully.
    /// </summary>
    ///
    [Test]
    public async Task PlayTurnAsyncHandlesUnicodeDigitInput()
    {
        var mockGameInterface = Substitute.For<IGameInterface>();
        var player = new ConsolePlayer(mockGameInterface);
        var gameState = CreateValidGameState();

        // Setup mock to return Unicode digit, then valid input.

        mockGameInterface.ReadPlayerResponseAsnyc(Arg.Any<CancellationToken>())
            .Returns("①", "1", "1", "1");

        var move = await player.PlayTurnAsync(gameState);

        Assert.That(move.Token, Is.EqualTo(1), "Should handle Unicode gracefully and accept valid input");
    }

    /// <summary>
    ///   Verifies that PlayTurnAsync handles emoji input gracefully.
    /// </summary>
    ///
    [Test]
    public async Task PlayTurnAsyncHandlesEmojiInput()
    {
        var mockGameInterface = Substitute.For<IGameInterface>();
        var player = new ConsolePlayer(mockGameInterface);
        var gameState = CreateValidGameState();

        // Setup mock to return emoji, then valid input.

        mockGameInterface.ReadPlayerResponseAsnyc(Arg.Any<CancellationToken>())
            .Returns("🎮", "1", "1", "1");

        var move = await player.PlayTurnAsync(gameState);

        Assert.That(move.Token, Is.EqualTo(1), "Should handle emoji gracefully and accept valid input");
    }

    /// <summary>
    ///   Verifies that PlayTurnAsync handles extremely long input strings without crashing.
    /// </summary>
    ///
    [Test]
    public async Task PlayTurnAsyncHandlesExtremelyLongInput()
    {
        var mockGameInterface = Substitute.For<IGameInterface>();
        var player = new ConsolePlayer(mockGameInterface);
        var gameState = CreateValidGameState();

        // Create an extremely long string that could potentially cause issues.

        var longString = new string('1', 10000);
        var veryLongString = new string('a', 100000);

        mockGameInterface.ReadPlayerResponseAsnyc(Arg.Any<CancellationToken>())
            .Returns(
                longString,      // 10k characters of '1'
                veryLongString,  // 100k characters of 'a'
                "1",             // Valid token
                "1",             // Valid row
                "1"              // Valid column
            );

        var move = await player.PlayTurnAsync(gameState);

        Assert.That(move.Token, Is.EqualTo(1), "Should handle extremely long input without crashing");
        Assert.That(move.PositionIndex, Is.EqualTo(0), "Should eventually process valid input after long strings");
    }

    /// <summary>
    ///   Verifies that PlayTurnAsync handles input with leading and trailing whitespace correctly.
    /// </summary>
    ///
    [Test]
    public async Task PlayTurnAsyncHandlesSpaceWhitespace()
    {
        var mockGameInterface = Substitute.For<IGameInterface>();
        var player = new ConsolePlayer(mockGameInterface);
        var gameState = CreateValidGameState();

        // Setup mock to return inputs with space whitespace.

        mockGameInterface.ReadPlayerResponseAsnyc(Arg.Any<CancellationToken>())
            .Returns(
                "  1  ",     // Token with spaces
                "   1   ",   // Row with spaces
                " 1 "        // Column with spaces
            );

        var move = await player.PlayTurnAsync(gameState);

        Assert.That(move.Token, Is.EqualTo(1), "Should trim space whitespace and accept valid token");
        Assert.That(move.PositionIndex, Is.EqualTo(0), "Should trim space whitespace and accept valid position");
    }

    /// <summary>
    ///   Verifies that PlayTurnAsync handles tab character input correctly.
    /// </summary>
    ///
    [Test]
    public async Task PlayTurnAsyncHandlesTabCharacterInput()
    {
        var mockGameInterface = Substitute.For<IGameInterface>();
        var player = new ConsolePlayer(mockGameInterface);
        var gameState = CreateValidGameState();

        // Setup mock to return input with tab characters.

        mockGameInterface.ReadPlayerResponseAsnyc(Arg.Any<CancellationToken>())
            .Returns(
                "\t1\t",     // Token with tabs
                "\t\t1\t\t", // Row with tabs
                "\t1\t"      // Column with tabs
            );

        var move = await player.PlayTurnAsync(gameState);

        Assert.That(move.Token, Is.EqualTo(1), "Should trim tab characters and accept valid token");
        Assert.That(move.PositionIndex, Is.EqualTo(0), "Should trim tab characters and accept valid position");
    }

    /// <summary>
    ///   Creates a valid initial game state for testing.
    /// </summary>
    ///
    private static GameState CreateValidGameState() =>
        new GameState(
            PlayerToken.Odd,
            new byte[9],
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
            new byte[16],
            20,
            [
                new HashSet<byte> { 1, 3, 5, 7, 9, 11, 13, 15 },
                new HashSet<byte> { 2, 4, 6, 8, 10, 12, 14, 16 }
            ]);
}