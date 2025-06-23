using NUnit.Framework;
using Squire.NumTic.Console;
using Squire.NumTic.Game;

namespace Squire.NumTic.Tests;

/// <summary>
///   Tests for the <see cref="ConsoleGameInterface"/> class focusing on custom behavior
///   and valid game scenarios rather than built-in .NET functionality.
/// </summary>
///
[TestFixture]
[NonParallelizable]
[Category("Console")]
public class ConsoleGameInterfaceTests
{
    /// <summary>
    ///   Verifies that RenderAsync throws when gameState is null.
    /// </summary>
    ///
    [Test]
    public async Task RenderAsyncWithNullGameStateThrows()
    {
        var gameInterface = new ConsoleGameInterface();

        await Assert.ThatAsync(async () => await gameInterface.RenderAsync(null!),
            Throws.ArgumentNullException.With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("gameState"));
    }

    /// <summary>
    ///   Verifies that RenderAsync handles cancellation appropriately when requested.
    /// </summary>
    ///
    [Test]
    public async Task RenderAsyncWithCancellationTokenThrows()
    {
        var gameInterface = new ConsoleGameInterface();
        var gameState = CreateValidGameState();

        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThatAsync(async () => await gameInterface.RenderAsync(gameState, cancellationTokenSource.Token),
            Throws.InstanceOf<OperationCanceledException>());
    }

    /// <summary>
    ///   Verifies that RenderAsync completes successfully with valid game states.
    /// </summary>
    ///
    [Test]
    public async Task RenderAsyncWithValidGameStateCompletesSuccessfully()
    {
        var gameInterface = new ConsoleGameInterface();
        var gameState = CreateValidGameState();

        // Redirect console output to avoid cluttering test output.

        using var originalOut = System.Console.Out;
        using var stringWriter = new StringWriter();

        System.Console.SetOut(stringWriter);

        try
        {
            await Assert.ThatAsync(async () => await gameInterface.RenderAsync(gameState), Throws.Nothing);
        }
        finally
        {
            System.Console.SetOut(originalOut);
        }
    }

    /// <summary>
    ///   Verifies that RenderAsync handles different board sizes appropriately.
    /// </summary>
    ///
    [Test]
    public async Task RenderAsyncWithDifferentBoardSizesCompletesSuccessfully()
    {
        var gameInterface = new ConsoleGameInterface();

        // Test with a 4x4 board
        var largerGameState = new GameState(
            PlayerToken.Even,
            new int[16],
            20,
            [
                new HashSet<byte> { 1, 3, 5, 7, 9, 11, 13, 15 },
                new HashSet<byte> { 2, 4, 6, 8, 10, 12, 14, 16 }
            ]);

        // Redirect console output to avoid cluttering test output.

        using var originalOut = System.Console.Out;
        using var stringWriter = new StringWriter();

        System.Console.SetOut(stringWriter);

        try
        {
            await Assert.ThatAsync(async () => await gameInterface.RenderAsync(largerGameState), Throws.Nothing);
        }
        finally
        {
            System.Console.SetOut(originalOut);
        }
    }

    /// <summary>
    ///   Verifies that RenderAsync handles game states with moves made.
    /// </summary>
    ///
    [Test]
    public async Task RenderAsyncWithGameInProgressCompletesSuccessfully()
    {
        var gameInterface = new ConsoleGameInterface();
        var gameState = CreateGameStateWithMoves();

        // Redirect console output to avoid cluttering test output.

        using var originalOut = System.Console.Out;
        using var stringWriter = new StringWriter();

        System.Console.SetOut(stringWriter);

        try
        {
            await Assert.ThatAsync(async () => await gameInterface.RenderAsync(gameState), Throws.Nothing);
        }
        finally
        {
            System.Console.SetOut(originalOut);
        }
    }

    /// <summary>
    ///   Verifies that RenderAsync handles winning game scenarios.
    /// </summary>
    ///
    [Test]
    public async Task RenderAsyncWithWinningGameCompletesSuccessfully()
    {
        var gameInterface = new ConsoleGameInterface();
        var gameState = CreateWinningGameState();

        // Redirect console output to avoid cluttering test output.

        using var originalOut = System.Console.Out;
        using var stringWriter = new StringWriter();
        System.Console.SetOut(stringWriter);

        try
        {
            await Assert.ThatAsync(async () => await gameInterface.RenderAsync(gameState), Throws.Nothing);
        }
        finally
        {
            System.Console.SetOut(originalOut);
        }
    }

    /// <summary>
    ///   Verifies that RenderPlayerTextAsync handles different TextType values correctly.
    /// </summary>
    ///
    [Test]
    public async Task RenderPlayerTextAsyncHandlesAllTextTypesCorrectly()
    {
        var gameInterface = new ConsoleGameInterface();

        // Redirect console output to avoid cluttering test output.

        using var originalOut = System.Console.Out;
        using var stringWriter = new StringWriter();

        System.Console.SetOut(stringWriter);

        try
        {
            // Test that all enum values are handled without throwing.

            foreach (TextType textType in Enum.GetValues<TextType>())
            {
                await Assert.ThatAsync(async () => await gameInterface.RenderPlayerTextAsync(textType, "Test"), Throws.Nothing);
            }
        }
        finally
        {
            System.Console.SetOut(originalOut);
        }
    }

    /// <summary>
    ///   Verifies that RenderPlayerTextAsync respects contract behavior for parameter validation.
    /// </summary>
    ///
    [Test]
    public async Task RenderPlayerTextAsyncWithEmptyStringCompletesSuccessfully()
    {
        var gameInterface = new ConsoleGameInterface();

        // Redirect console output to avoid cluttering test output.

        using var originalOut = System.Console.Out;
        using var stringWriter = new StringWriter();

        System.Console.SetOut(stringWriter);

        try
        {
            await Assert.ThatAsync(async () => await gameInterface.RenderPlayerTextAsync(TextType.Message, ""), Throws.Nothing);
        }
        finally
        {
            System.Console.SetOut(originalOut);
        }
    }

    /// <summary>
    ///   Verifies that ReadPlayerResponseAsync can be called without throwing at the contract level.
    /// </summary>
    ///
    [Test]
    public void ReadPlayerResponseAsyncAcceptsCallWithoutCancellation()
    {
        var gameInterface = new ConsoleGameInterface();

        // We're testing that the method signature accepts the call, not the behavior
        // since actual execution would require console input.

        Assert.That(() => gameInterface.ReadPlayerResponseAsnyc(), Throws.Nothing);
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
    ///   Creates a game state with some moves already made.
    /// </summary>
    ///
    private static GameState CreateGameStateWithMoves()
    {
        var board = new int[9];
        board[0] = 1; // Player Odd placed 1 at position (1,1)
        board[4] = 4; // Player Even placed 4 at position (2,2)
        board[8] = 9; // Player Odd placed 9 at position (3,3)

        return new GameState(
            PlayerToken.Even,
            board,
            15,
            [
                new HashSet<byte> { 3, 5, 7 }, // Odd player has used 1 and 9
                new HashSet<byte> { 2, 6, 8 }  // Even player has used 4
            ]);
    }

    /// <summary>
    ///   Creates a winning game state for testing.
    /// </summary>
    ///
    private static GameState CreateWinningGameState()
    {
        var board = new int[9];
        board[0] = 1; // (1,1)
        board[1] = 5; // (1,2)
        board[2] = 9; // (1,3) - Winning row: 1 + 5 + 9 = 15

        var gameState = new GameState(
            PlayerToken.Even,
            board,
            15,
            [
                new HashSet<byte> { 3, 7 },     // Odd player has used 1, 5, 9
                new HashSet<byte> { 2, 4, 6, 8 } // Even player hasn't used anything yet
            ]);

        // Manually set the winner by scanning, since this is a test scenario.

        _ = gameState.ScanForWinner();
        return gameState;
    }
}
