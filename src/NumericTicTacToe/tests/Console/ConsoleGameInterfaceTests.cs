using NUnit.Framework;
using NSubstitute;
using Spectre.Console;
using Spectre.Console.Testing;
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
        var testConsole = new TestConsole();
        var gameState = CreateValidGameState();
        var gameInterface = new ConsoleGameInterface(gameState, testConsole);

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
        var testConsole = new TestConsole();
        var gameState = CreateValidGameState();
        var gameInterface = new ConsoleGameInterface(gameState, testConsole);

        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThatAsync(async () => await gameInterface.RenderAsync(gameState, cancellationTokenSource.Token),
            Throws.InstanceOf<OperationCanceledException>());
    }

    /// <summary>
    ///   Verifies that RenderAsync handles winning game state information correctly.
    /// </summary>
    ///
    [Test]
    public async Task RenderAsyncWithWinningGameShowsWinner()
    {
        // Use TestConsole for proper Spectre.Console testing.

        var testConsole = new TestConsole();
        var gameState = CreateValidGameState();
        UpdateGameStateToWinningScenario(gameState);

        // Create the ConsoleGameInterface AFTER we have the final game state

        var gameInterface = new ConsoleGameInterface(gameState, testConsole);

        // Test that rendering completes for winning game states.

        await gameInterface.RenderAsync(gameState);

        var output = testConsole.Output;

        // Verify that winning state is displayed.

        Assert.That(output, Contains.Substring("Odd"), "Should display the winning player");

        // Verify the game state is actually in a winning condition.

        Assert.That(gameState.Winner, Is.Not.Null, "Game state should have a winner");
        Assert.That(gameState.IsGameOver, Is.True, "Game should be marked as over");
    }

    /// <summary>
    ///   Verifies that RenderPlayerTextAsync handles different TextType values correctly.
    /// </summary>
    ///
    [Test]
    public async Task RenderPlayerTextAsyncHandlesTextType()
    {
        var testConsole = new TestConsole();
        var gameState = CreateValidGameState();
        var gameInterface = new ConsoleGameInterface(gameState, testConsole);

        foreach (var textType in Enum.GetValues<TextType>())
        {
            await gameInterface.RenderPlayerTextAsync(textType, "Test message");

            // Some rendering types related to prompting are only rendered when
            // reading input, so ensure that flow is triggered before capturing output.

            if (textType == TextType.Prompt)
            {
                testConsole.Input.PushTextWithEnter("test input");
                var readTask = gameInterface.ReadPlayerResponseAsnyc();

                _ = await readTask;
            }

            var output = testConsole.Output;
            Assert.That(output, Contains.Substring("Test message"), $"Should render message for {textType}");
        }
    }

    /// <summary>
    ///   Verifies that ReadPlayerResponseAsync returns the correct input from the console.
    /// </summary>
    ///
    [Test]
    public async Task ReadPlayerResponseAsyncReturnsCorrectInput()
    {
        var gameState = CreateValidGameState();
        var testConsole = new TestConsole();

        // Push specific input followed by Enter for TextPrompt

        testConsole.Input.PushTextWithEnter("player move input");

        var gameInterface = new ConsoleGameInterface(gameState, testConsole);

        var result = await gameInterface.ReadPlayerResponseAsnyc();

        // Verify the method returns the exact input provided

        Assert.That(result, Is.EqualTo("player move input"), "Should return the exact input provided by the player");
    }

    /// <summary>
    ///   Verifies that ConsoleGameInterface displays board state correctly after moves are played.
    /// </summary>
    ///
    [Test]
    public async Task RenderAsyncShowsCorrectBoardStateAfterMoves()
    {
        // Use a mock console to prevent UI widgets from appearing in test output.

        var testConsole = new TestConsole();
        var gameState = CreateValidGameState();

        // Apply some moves using proper game flow

        gameState.ApplyMove(new Move(PlayerToken.Odd, 0, 1));   // Top-left
        gameState.ApplyMove(new Move(PlayerToken.Even, 4, 2));  // Center
        gameState.ApplyMove(new Move(PlayerToken.Odd, 8, 3));   // Bottom-right

        var gameInterface = new ConsoleGameInterface(gameState, testConsole);

        // Test that rendering this state works correctly.

        await gameInterface.RenderAsync(gameState);

        var output = testConsole.Output;

        // Verify that board state information is displayed correctly.

        Assert.That(output, Contains.Substring("Even"), "Should display current player (Even's turn)");
        Assert.That(output, Contains.Substring("1"), "Should display placed token 1");
        Assert.That(output, Contains.Substring("2"), "Should display placed token 2");
        Assert.That(output, Contains.Substring("3"), "Should display placed token 3");
    }

    /// <summary>
    ///   Verifies that ReadPlayerResponseAsync handles multiple sequential input operations correctly.
    /// </summary>
    ///
    [Test]
    public async Task ReadPlayerResponseAsyncHandlesSequentialInputs()
    {
        var gameState = CreateValidGameState();
        var testConsole = new TestConsole();
        var expectedResponses = new[] { "move1", "move2", "move3" };

        // Queue multiple responses in the test console with Enter keys

        foreach (var response in expectedResponses)
        {
            testConsole.Input.PushTextWithEnter(response);
        }

        var gameInterface = new ConsoleGameInterface(gameState, testConsole);

        // Execute sequential read operations

        var results = new List<string>();

        for (var i = 0; i < expectedResponses.Length; i++)
        {
            var result = await gameInterface.ReadPlayerResponseAsnyc();
            results.Add(result ?? string.Empty);
        }

        // Verify all reads returned the expected responses in order

        Assert.That(results, Is.EqualTo(expectedResponses), "Should return responses in the correct order");
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
    ///   Updates an existing game state to create a realistic winning scenario
    ///   that follows proper turn alternation and token management using ApplyMove.
    /// </summary>
    ///
    private static GameState UpdateGameStateToWinningScenario(GameState gameState)
    {
        // Simulate alternating moves using ApplyMove: Odd(3), Even(2), Odd(5), Even(4), Odd(7) = Win
        // This creates a valid game progression that keeps the highest tokens available

        // Move 1: Odd player places 3 at position 0

        gameState.ApplyMove(new Move(PlayerToken.Odd, 0, 3));

        // Move 2: Even player places 2 at position 3

        gameState.ApplyMove(new Move(PlayerToken.Even, 3, 2));

        // Move 3: Odd player places 5 at position 1

        gameState.ApplyMove(new Move(PlayerToken.Odd, 1, 5));

        // Move 4: Even player places 4 at position 4

        gameState.ApplyMove(new Move(PlayerToken.Even, 4, 4));

        // Move 5: Odd player places 7 at position 2 (completes winning row: 3+5+7=15)

        gameState.ApplyMove(new Move(PlayerToken.Odd, 2, 7));

        return gameState;
    }
}