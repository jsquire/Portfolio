using NUnit.Framework;

namespace Squire.NumTic.Tests;

/// <summary>
///   The suite of tests for the <see cref="McpRenderer"/> class.
/// </summary>
///
[TestFixture]
[Category(TestCategory.MCP)]
public class McpRendererTests
{
    /// <summary>
    ///   Verifies functionality of the RenderGameState method.
    /// </summary>
    ///
    [Test]
    public void RenderGameStateWithNullGameStateThrows()
    {
        var gameState = GameState.CreateDefault();
        var renderer = new McpRenderer(gameState);

        Assert.That(() => renderer.RenderGameState(null!),
            Throws.ArgumentNullException.With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("gameState"));
    }

    /// <summary>
    ///   Verifies functionality of the RenderBoard method.
    /// </summary>
    ///
    [Test]
    public void RenderBoardWithNullGameStateThrows()
    {
        var gameState = GameState.CreateDefault();
        var renderer = new McpRenderer(gameState);

        Assert.That(() => renderer.RenderBoard(null!),
            Throws.ArgumentNullException.With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("gameState"));
    }

    /// <summary>
    ///   Verifies functionality of the RenderGameState method.
    /// </summary>
    ///
    [Test]
    public void RenderGameStateProducesNonEmptyOutput()
    {
        var gameState = GameState.CreateDefault();
        var renderer = new McpRenderer(gameState);
        var result = renderer.RenderGameState(gameState);

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Not.Empty);
        Assert.That(result.Trim(), Is.Not.Empty);
    }

    /// <summary>
    ///   Verifies functionality of the RenderBoard method.
    /// </summary>
    ///
    [Test]
    public void RenderBoardProducesNonEmptyOutput()
    {
        var gameState = GameState.CreateDefault();
        var renderer = new McpRenderer(gameState);
        var result = renderer.RenderBoard(gameState);

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Not.Empty);
        Assert.That(result.Trim(), Is.Not.Empty);
    }

    /// <summary>
    ///   Verifies functionality of the RenderGameState method.
    /// </summary>
    ///
    [Test]
    public void RenderGameStateContainsRequiredComponents()
    {
        var gameState = CreateGameStateWithPartialBoard();
        var renderer = new McpRenderer(gameState);
        var result = renderer.RenderGameState(gameState);

        // Check for structural components, not exact format.

        Assert.That(result, Contains.Substring("```"), "Should contain code block markers");
        Assert.That(result, Contains.Substring("Game Board"), "Should contain game board section");
        Assert.That(result, Contains.Substring("Player Information"), "Should contain player information section");
        Assert.That(result, Contains.Substring("Odd"), "Should show odd player information");
        Assert.That(result, Contains.Substring("Even"), "Should show even player information");
    }

    /// <summary>
    ///   Verifies functionality of the RenderGameState method.
    /// </summary>
    ///
    [Test]
    public void RenderGameStateWithEmptyBoardShowsEmptySpaces()
    {
        var gameState = GameState.CreateDefault();
        var renderer = new McpRenderer(gameState);
        var result = renderer.RenderGameState(gameState);

        // Empty positions should show as empty spaces, not the empty marker value.

        Assert.That(result, Contains.Substring("|"), "Should contain grid structure");
        Assert.That(result, Does.Not.Contain(GameState.EmptyBoardSpaceValue), "Empty positions should not show zeros");
    }

    /// <summary>
    ///   Verifies functionality of the RenderGameState method.
    /// </summary>
    ///
    [Test]
    public void RenderGameStateWithTokensShowsTokenValues()
    {
        var gameState = CreateGameStateWithTokens();
        var renderer = new McpRenderer(gameState);
        var result = renderer.RenderGameState(gameState);

        Assert.That(result, Contains.Substring("5"), "Should display placed token value 5");
        Assert.That(result, Contains.Substring("2"), "Should display placed token value 2");
    }

    /// <summary>
    ///   Verifies functionality of the RenderGameState method.
    /// </summary>
    ///
    [Test]
    public void RenderGameStateWithWinnerShowsGameState()
    {
        var gameState = CreateWinningGameState();
        var renderer = new McpRenderer(gameState);
        var result = renderer.RenderGameState(gameState);

        // Should render the winning state (showing the winning line tokens).

        Assert.That(result, Contains.Substring("3"), "Should show winning token 3");
        Assert.That(result, Contains.Substring("5"), "Should show winning token 5");
        Assert.That(result, Contains.Substring("7"), "Should show winning token 7");
    }

    /// <summary>
    ///   Verifies functionality of the RenderGameState method.
    /// </summary>
    ///
    [Test]
    public void RenderGameStateShowsTokenAvailabilityInfo()
    {
        var gameState = CreateGameStateWithUsedTokens();
        var renderer = new McpRenderer(gameState);
        var result = renderer.RenderGameState(gameState);

        // Should show player token information.

        Assert.That(result, Contains.Substring("Tokens"), "Should show player token information");
        Assert.That(result, Contains.Substring("Odd Player"), "Should show odd player info");
        Assert.That(result, Contains.Substring("Even Player"), "Should show even player info");
    }

    /// <summary>
    ///   Verifies functionality of the RenderGameState method.
    /// </summary>
    ///
    [Test]
    public void RenderGameStateWithNoAvailableTokensHandlesGracefully()
    {
        var gameState = CreateGameStateWithAllTokensUsed();
        var renderer = new McpRenderer(gameState);
        var result = renderer.RenderGameState(gameState);

        // Should handle game states with fewer available tokens.

        Assert.That(result, Is.Not.Empty, "Should produce output even with fewer available tokens");
        Assert.That(result, Contains.Substring("Tokens"), "Should still show player token information");
    }

    /// <summary>
    ///   Verifies functionality of the RenderBoard method.
    /// </summary>
    ///
    [Test]
    public void RenderBoardContainsGridStructure()
    {
        var gameState = CreateGameStateWithMixedTokens();
        var renderer = new McpRenderer(gameState);
        var result = renderer.RenderBoard(gameState);

        // Should contain grid structure without markdown wrapper.

        Assert.That(result, Contains.Substring("|"), "Should contain column separators");
        Assert.That(result, Contains.Substring("---"), "Should contain row separators");
    }

    /// <summary>
    ///   Verifies functionality of the RenderBoard method.
    /// </summary>
    ///
    [Test]
    public void RenderBoardShowsEmptySpacesAsEmpty()
    {
        var gameState = GameState.CreateDefault();
        var renderer = new McpRenderer(gameState);
        var result = renderer.RenderBoard(gameState);

        // Empty board should not show position numbers, just empty spaces.
        // Should contain grid structure but no token numbers.

        Assert.That(result, Contains.Substring("|"), "Should contain column separators");
        Assert.That(result, Does.Not.Contain(GameState.EmptyBoardSpaceValue), "Empty spaces should not show position numbers");
    }

    /// <summary>
    ///   Verifies functionality of the RenderGameState method.
    /// </summary>
    ///
    [Test]
    public void RenderGameStateWithOddPlayerWinShowsEnhancedMessage()
    {
        var gameState = GameState.CreateDefault();

        // Create a winning scenario for Odd player: 1 + 5 + 9 = 15 in top row.

        gameState.ApplyMove(new Move(PlayerToken.Odd, gameState.GetBoardPosition(1, 1), 1));
        gameState.ApplyMove(new Move(PlayerToken.Even, gameState.GetBoardPosition(2, 1), 2));
        gameState.ApplyMove(new Move(PlayerToken.Odd, gameState.GetBoardPosition(1, 2), 5));
        gameState.ApplyMove(new Move(PlayerToken.Even, gameState.GetBoardPosition(2, 2), 4));
        gameState.ApplyMove(new Move(PlayerToken.Odd, gameState.GetBoardPosition(1, 3), 9));

        var renderer = new McpRenderer(gameState);
        var result = renderer.RenderGameState(gameState);

        // Verify game completion message components.

        Assert.That(result, Contains.Substring("Game Over"), "Should explicitly indicate game has ended");
        Assert.That(result, Contains.Substring("Odd"), "Should indicate odd player");
        Assert.That(result, Contains.Substring("wins"), "Should indicate victory");
        Assert.That(result, Contains.Substring("Ready for another round"), "Should show new game instruction");
        Assert.That(result, Contains.Substring("---"), "Should include visual separators");
        Assert.That(gameState.Winner, Is.EqualTo(PlayerToken.Odd), "Test setup should create odd winner");
    }

    /// <summary>
    ///   Verifies functionality of the RenderGameState method.
    /// </summary>
    ///
    [Test]
    public void RenderGameStateWithEvenPlayerWinShowsEnhancedMessage()
    {
        // Since even numbers can't sum to 15, create a custom scenario with different winning total.

        var evenWinGameState = new GameState(
            PlayerToken.Even,
            [2, 4, 6, 0, 0, 0, 0, 0, 0], // Even wins with first row: 2+4+6=12
            12,                          // Custom winning total for even numbers
            [
                new HashSet<byte> { 1, 3, 5, 7, 9 },
                new HashSet<byte> { 2, 4, 6, 8 }
            ]);

        // Trigger winner detection.

        evenWinGameState.ScanForWinner();

        var renderer = new McpRenderer(evenWinGameState);
        var result = renderer.RenderGameState(evenWinGameState);

        // Verify game completion message components.

        Assert.That(result, Contains.Substring("Game Over"), "Should explicitly indicate game has ended");
        Assert.That(result, Contains.Substring("Even"), "Should indicate even player");
        Assert.That(result, Contains.Substring("wins"), "Should indicate victory");
        Assert.That(result, Contains.Substring("Ready for another round"), "Should show new game instruction");
        Assert.That(result, Contains.Substring("---"), "Should include visual separators");
        Assert.That(evenWinGameState.Winner, Is.EqualTo(PlayerToken.Even), "Test setup should create even winner");
    }

    /// <summary>
    ///   Verifies functionality of the RenderGameState method.
    /// </summary>
    ///
    [Test]
    public void RenderGameStateWithDrawShowsEnhancedMessage()
    {
        var gameState = GameState.CreateDefault();

        // Fill the board without creating any winning combinations.

        gameState.ApplyMove(new Move(PlayerToken.Odd, 0, 1));
        gameState.ApplyMove(new Move(PlayerToken.Even, 1, 2));
        gameState.ApplyMove(new Move(PlayerToken.Odd, 2, 3));
        gameState.ApplyMove(new Move(PlayerToken.Even, 3, 4));
        gameState.ApplyMove(new Move(PlayerToken.Odd, 4, 7));
        gameState.ApplyMove(new Move(PlayerToken.Even, 5, 6));
        gameState.ApplyMove(new Move(PlayerToken.Odd, 6, 9));
        gameState.ApplyMove(new Move(PlayerToken.Even, 7, 8));

        // Place final token to fill board without win.

        gameState.ApplyMove(new Move(PlayerToken.Odd, 8, 5));

        var renderer = new McpRenderer(gameState);
        var result = renderer.RenderGameState(gameState);

        // Verify game completion message components.

        Assert.That(result, Contains.Substring("Game Over"), "Should explicitly indicate game has ended");
        Assert.That(result, Contains.Substring("draw"), "Should indicate draw result");
        Assert.That(result, Contains.Substring("Ready for another round"), "Should show new game instruction");
        Assert.That(result, Contains.Substring("---"), "Should include visual separators");
        Assert.That(gameState.IsGameOver, Is.True, "Game should be marked as over");
        Assert.That(gameState.Winner, Is.Null, "Game should have no winner (draw)");
    }

    /// <summary>
    ///   Creates a game state with some tokens placed on the board.
    /// </summary>
    ///
    private static GameState CreateGameStateWithPartialBoard()
    {
        var gameState = GameState.CreateDefault();

        // Place a few tokens to create a partial game scenario.

        gameState.ApplyMove(new Move(PlayerToken.Odd, 0, 1));   // Odd plays 1 at position 0
        gameState.ApplyMove(new Move(PlayerToken.Even, 4, 2));  // Even plays 2 at position 4

        return gameState;
    }

    /// <summary>
    ///   Creates a game state with specific tokens placed for testing token display.
    /// </summary>
    ///
    private static GameState CreateGameStateWithTokens()
    {
        var gameState = GameState.CreateDefault();

        // Place specific tokens for verification.

        gameState.ApplyMove(new Move(PlayerToken.Odd, 0, 5));   // Odd plays 5 at position 0
        gameState.ApplyMove(new Move(PlayerToken.Even, 3, 2));  // Even plays 2 at position 3

        return gameState;
    }

    /// <summary>
    ///   Creates a winning game state for testing winner display.
    /// </summary>
    ///
    private static GameState CreateWinningGameState()
    {
        var gameState = GameState.CreateDefault();

        // Create a valid winning scenario using ApplyMove.
        // Simulate alternating moves: Odd(3), Even(2), Odd(5), Even(4), Odd(7) = Win

        gameState.ApplyMove(new Move(PlayerToken.Odd, 0, 3));   // Odd plays 3 at position 0
        gameState.ApplyMove(new Move(PlayerToken.Even, 3, 2));  // Even plays 2 at position 3
        gameState.ApplyMove(new Move(PlayerToken.Odd, 1, 5));   // Odd plays 5 at position 1
        gameState.ApplyMove(new Move(PlayerToken.Even, 4, 4));  // Even plays 4 at position 4
        gameState.ApplyMove(new Move(PlayerToken.Odd, 2, 7));   // Odd plays 7 at position 2 (3+5+7=15)

        return gameState;
    }

    /// <summary>
    ///   Creates a game state with some tokens used to test token availability display.
    /// </summary>
    ///
    private static GameState CreateGameStateWithUsedTokens()
    {
        var gameState = GameState.CreateDefault();

        // Use some tokens to create available/used token scenarios.

        gameState.ApplyMove(new Move(PlayerToken.Odd, 0, 1));   // Odd uses 1
        gameState.ApplyMove(new Move(PlayerToken.Even, 3, 2));  // Even uses 2
        gameState.ApplyMove(new Move(PlayerToken.Odd, 1, 3));   // Odd uses 3

        return gameState;
    }

    /// <summary>
    ///   Creates a game state where many tokens have been used (but not invalid).
    /// </summary>
    ///
    private static GameState CreateGameStateWithAllTokensUsed()
    {
        var gameState = GameState.CreateDefault();

        // Apply moves alternating turns correctly.

        gameState.ApplyMove(new Move(PlayerToken.Odd, 0, 1));   // Odd uses 1
        gameState.ApplyMove(new Move(PlayerToken.Even, 1, 2));  // Even uses 2
        gameState.ApplyMove(new Move(PlayerToken.Odd, 2, 3));   // Odd uses 3
        gameState.ApplyMove(new Move(PlayerToken.Even, 3, 4));  // Even uses 4
        gameState.ApplyMove(new Move(PlayerToken.Odd, 4, 5));   // Odd uses 5
        gameState.ApplyMove(new Move(PlayerToken.Even, 5, 6));  // Even uses 6
        // Skip token 7 and 8 to avoid turn issues.

        return gameState;
    }

    /// <summary>
    ///   Creates a game state with a mix of tokens for testing grid display.
    /// </summary>
    ///
    private static GameState CreateGameStateWithMixedTokens()
    {
        var gameState = GameState.CreateDefault();

        // Place tokens in different positions to test grid rendering.

        gameState.ApplyMove(new Move(PlayerToken.Odd, 0, 1));   // Position 0: 1
        gameState.ApplyMove(new Move(PlayerToken.Even, 2, 2));  // Position 2: 2
        gameState.ApplyMove(new Move(PlayerToken.Odd, 4, 5));   // Position 4: 5
        gameState.ApplyMove(new Move(PlayerToken.Even, 6, 6));  // Position 6: 6

        return gameState;
    }

    /// <summary>
    ///   Verifies functionality of the RenderGameState method.
    /// </summary>
    ///
    [Test]
    public void RenderGameStateIncludesPositionGuideHeader()
    {
        var gameState = GameState.CreateDefault();
        var renderer = new McpRenderer(gameState);
        var result = renderer.RenderGameState(gameState);

        Assert.That(result, Does.Contain("Position Guide"));
    }

    /// <summary>
    ///   Verifies functionality of the RenderGameState method.
    /// </summary>
    ///
    [Test]
    public void RenderGameStateIncludesPositionNumbers()
    {
        var gameState = GameState.CreateDefault();
        var renderer = new McpRenderer(gameState);
        var result = renderer.RenderGameState(gameState);

        // Verify numbers 1-9 appear somewhere.

        for (var index = 1; index <= 9; ++index)
        {
            Assert.That(result, Does.Contain(index.ToString()));
        }
    }

    /// <summary>
    ///   Verifies functionality of the RenderGameState method.
    /// </summary>
    ///
    [Test]
    public void RenderGameStateIncludesMarkdownCodeBlock()
    {
        var gameState = GameState.CreateDefault();
        var renderer = new McpRenderer(gameState);
        var result = renderer.RenderGameState(gameState);

        Assert.That(result, Does.Contain("```"));
    }

    /// <summary>
    ///   Verifies functionality of the RenderBoard method.
    /// </summary>
    ///
    [Test]
    public void RenderBoardExcludesPlayerInformation()
    {
        var gameState = GameState.CreateDefault();
        var renderer = new McpRenderer(gameState);
        var result = renderer.RenderBoard(gameState);

        // RenderBoard includes code blocks but not the game state headers.

        Assert.That(result, Does.Contain("```"));
        Assert.That(result, Does.Not.Contain("Game Board"));
        Assert.That(result, Does.Not.Contain("Player Information"));
    }

    /// <summary>
    ///   Verifies functionality of the RenderBoard method.
    /// </summary>
    ///
    /// <param name="token">The token value to test.</param>
    ///
    [Test]
    [TestCase((byte)1)]
    [TestCase((byte)5)]
    [TestCase((byte)9)]
    public void RenderBoardIncludesPlacedToken(byte token)
    {
        var gameState = GameState.CreateDefault();

        gameState.ApplyMove(new Move(PlayerToken.Odd, 4, token)); // Center position

        var renderer = new McpRenderer(gameState);
        var result = renderer.RenderBoard(gameState);

        Assert.That(result, Does.Contain(token.ToString()));
    }
}