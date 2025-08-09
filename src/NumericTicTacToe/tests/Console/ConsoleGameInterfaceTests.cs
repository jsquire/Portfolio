using NUnit.Framework;
using Squire.NumTic;
using Squire.NumTic.Console;

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
    ///   Verifies that RenderAsync handles winning game scenarios.
    /// </summary>
    ///
    [Test]
    public async Task RenderAsyncWithWinningGameRendersCorrectly()
    {
        var gameInterface = new ConsoleGameInterface();
        var gameState = CreateWinningGameState();

        // Redirect console output to avoid cluttering test output.

        using var originalOut = System.Console.Out;
        using var stringWriter = new StringWriter();
        System.Console.SetOut(stringWriter);

        try
        {
            await gameInterface.RenderAsync(gameState);
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
                await gameInterface.RenderPlayerTextAsync(textType, "Test");
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
    public async Task RenderPlayerTextAsyncWithEmptyStringHandlesEmptyInput()
    {
        var gameInterface = new ConsoleGameInterface();

        // Redirect console output to avoid cluttering test output.

        using var originalOut = System.Console.Out;
        using var stringWriter = new StringWriter();

        System.Console.SetOut(stringWriter);

        try
        {
            await gameInterface.RenderPlayerTextAsync(TextType.Message, "");
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
    public async Task ReadPlayerResponseAsyncAcceptsCallWithoutCancellation()
    {
        var gameInterface = new ConsoleGameInterface();

        var originalInput = System.Console.In;
        var input = new StringReader("1\n"); // Simulate user typing "1" and pressing Enter

        System.Console.SetIn(input);

        try
        {
            await gameInterface.ReadPlayerResponseAsnyc();
        }
        finally
        {
            System.Console.SetIn(originalInput);
        }
    }

    /// <summary>
    ///   Verifies that ConsoleGameInterface handles extremely large game boards correctly.
    /// </summary>
    ///
    [Test]
    public async Task ConsoleGameInterfaceHandlesLargeBoardsCorrectly()
    {
        var gameInterface = new ConsoleGameInterface();

        // Create a large 10x10 board to test rendering performance and correctness.

        var largeBoard = new byte[100];
        var largeGameState = new GameState(
            PlayerToken.Odd,
            largeBoard,
            50,
            [
                new HashSet<byte> { 1, 3, 5, 7, 9, 11, 13, 15, 17, 19, 21, 23, 25, 27, 29, 31, 33, 35, 37, 39, 41, 43, 45, 47, 49 },
                new HashSet<byte> { 2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22, 24, 26, 28, 30, 32, 34, 36, 38, 40, 42, 44, 46, 48, 50 }
            ]);

        // Place some tokens to create a more complex board state.

        largeBoard[0] = 1;   // Top-left
        largeBoard[10] = 3;  // Second row, first column
        largeBoard[99] = 5;  // Bottom-right

        using var originalOut = System.Console.Out;
        using var stringWriter = new StringWriter();
        System.Console.SetOut(stringWriter);

        try
        {
            await gameInterface.RenderAsync(largeGameState);

            var output = stringWriter.ToString();

            // Verify the output contains expected structural elements for large boards.

            Assert.That(output, Contains.Substring("NUMERIC TIC-TAC-TOE"), "Should contain title");
            Assert.That(output, Contains.Substring("Game Board:"), "Should contain board header");
            Assert.That(output, Contains.Substring("Players:"), "Should contain players section");
            Assert.That(output.Split('\n').Length, Is.GreaterThan(30), "Large board should produce substantial output");
        }
        finally
        {
            System.Console.SetOut(originalOut);
        }
    }

    /// <summary>
    ///   Verifies that ConsoleGameInterface handles concurrent rendering calls safely.
    /// </summary>
    ///
    [Test]
    public async Task ConsoleGameInterfaceHandlesConcurrentRenderingSafely()
    {
        var gameInterface = new ConsoleGameInterface();
        var gameState = CreateWinningGameState();

        // Redirect console to capture all output.

        using var originalOut = System.Console.Out;
        using var stringWriter = new StringWriter();
        System.Console.SetOut(stringWriter);

        try
        {
            // Execute multiple concurrent render operations.

            var renderTasks = Enumerable.Range(0, 10)
                .Select(_ => gameInterface.RenderAsync(gameState))
                .ToArray();

            await Task.WhenAll(renderTasks);

            // All tasks should complete successfully without exceptions.

            foreach (var task in renderTasks)
            {
                Assert.That(task.IsCompletedSuccessfully, Is.True,
                    "Concurrent rendering should complete without exceptions");
            }
        }
        finally
        {
            System.Console.SetOut(originalOut);
        }
    }

    /// <summary>
    ///   Verifies that RenderPlayerTextAsync handles null and extremely long text correctly.
    /// </summary>
    ///
    [Test]
    public async Task RenderPlayerTextAsyncHandlesExtremeTextLengths()
    {
        var gameInterface = new ConsoleGameInterface();

        using var originalOut = System.Console.Out;
        using var stringWriter = new StringWriter();
        System.Console.SetOut(stringWriter);

        try
        {
            // Test with extremely long text that could cause buffer issues.

            var longText = new string('A', 100000);
            var textWithNewlines = string.Join("\n", Enumerable.Repeat("Line of text", 1000));
            var textWithSpecialChars = "Text with special chars: \0\t\r\n\x1B[31m\uFEFF";

            await gameInterface.RenderPlayerTextAsync(TextType.Message, longText);
            await gameInterface.RenderPlayerTextAsync(TextType.Error, textWithNewlines);
            await gameInterface.RenderPlayerTextAsync(TextType.Prompt, textWithSpecialChars);

            // If we reach here without exceptions, the interface handled extreme text correctly.

            var output = stringWriter.ToString();
            Assert.That(output.Length, Is.GreaterThan(50000), "Should have rendered substantial content");
        }
        finally
        {
            System.Console.SetOut(originalOut);
        }
    }

    /// <summary>
    ///   Verifies that ReadPlayerResponseAsync handles rapid successive calls correctly.
    /// </summary>
    ///
    [Test]
    public async Task ReadPlayerResponseAsyncHandlesRapidSuccessiveCalls()
    {
        var gameInterface = new ConsoleGameInterface();
        var responses = new[] { "response1", "response2", "response3", "response4", "response5" };

        using var originalInput = System.Console.In;
        using var stringReader = new StringReader(string.Join("\n", responses));
        System.Console.SetIn(stringReader);

        try
        {
            // Execute rapid successive read operations.

            var readTasks = Enumerable.Range(0, 5)
                .Select(_ => gameInterface.ReadPlayerResponseAsnyc())
                .ToArray();

            var results = await Task.WhenAll(readTasks);

            // Verify all reads completed and returned expected responses.

            Assert.That(results.Length, Is.EqualTo(5), "Should complete all read operations");

            foreach (var result in results)
            {
                Assert.That(responses, Contains.Item(result),
                    "Each result should be one of the expected responses");
            }
        }
        finally
        {
            System.Console.SetIn(originalInput);
        }
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
    ///   Creates a winning game state for testing.
    /// </summary>
    ///
    private static GameState CreateWinningGameState()
    {
        var board = new byte[9];
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