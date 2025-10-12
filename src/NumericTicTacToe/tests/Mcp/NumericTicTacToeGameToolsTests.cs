using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using NUnit.Framework;
using Squire.NumTic.Contracts;
using Squire.NumTic.Mcp;

namespace Squire.NumTic.Tests;

/// <summary>
///   The suite of tests for the <see cref="NumericTicTacToeGameTools"/> class.
/// </summary>
///
[TestFixture]
[Category(TestCategory.MCP)]
public class NumericTicTacToeGameToolsTests
{
    /// <summary>
    ///   Verifies functionality of the constructor.
    /// </summary>
    ///
    [Test]
    public void PublicConstructorRequiresAllDependencies()
    {
        var mockCache = Substitute.For<IMemoryCache>();
        var mockBotInterface = Substitute.For<IGameInterface>();
        var mockRenderer = Substitute.For<McpRenderer>(GameState.CreateDefault());

        Assert.That(() => new NumericTicTacToeGameTools(null!, mockBotInterface, mockRenderer),
            Throws.ArgumentNullException.With.Property("ParamName").EqualTo("cache"));

        Assert.That(() => new NumericTicTacToeGameTools(mockCache, null!, mockRenderer),
            Throws.ArgumentNullException.With.Property("ParamName").EqualTo("botGameInterface"));
    }

    /// <summary>
    ///   Verifies functionality of the StartNewGame method.
    /// </summary>
    ///
    [Test]
    public async Task StartNewGameUsesGameStateFactory()
    {
        var mockCache = Substitute.For<IMemoryCache>();
        var mockBotInterface = Substitute.For<IGameInterface>();
        var gameState = GameState.CreateDefault();
        var mockRenderer = Substitute.For<McpRenderer>(gameState);

        // Set up renderer to return expected content.

        mockRenderer
            .RenderGameState(gameState)
            .Returns("Mock Game Render");

        // Custom game state to verify factory usage.

        var tools = new NumericTicTacToeGameTools(mockCache, mockBotInterface, mockRenderer, () => gameState);
        var result = await tools.StartNewGame();

        // Verify result is from renderer.

        Assert.That(result, Is.EqualTo("Mock Game Render"));
    }

    /// <summary>
    ///   Verifies functionality of the StartNewGame method.
    /// </summary>
    ///
    [Test]
    public async Task StartNewGameRemovesPreviousGame()
    {
        var mockCache = Substitute.For<IMemoryCache>();

        // Set up cache to return existing game.

        mockCache.TryGetValue(Arg.Any<string>(), out Arg.Any<object?>())
            .Returns(x =>
            {
                x[1] = (GameState.CreateDefault(), PlayerToken.Odd, Difficulty.Easy);
                return true;
            });

        var mockBotInterface = Substitute.For<IGameInterface>();
        var mockRenderer = Substitute.For<McpRenderer>(GameState.CreateDefault());

        mockRenderer
            .RenderGameState(Arg.Any<GameState>())
            .Returns("Rendered");

        var tools = new NumericTicTacToeGameTools(mockCache, mockBotInterface, mockRenderer);

        // Start a new game with the same gameId to trigger cleanup.

        await tools.StartNewGame(PlayerToken.Odd, Difficulty.Easy, "game1");

        // Verify the previous game was removed from cache.

        mockCache.Received(1).Remove(Arg.Is<string>(k => k.Contains("game1")));
    }

    /// <summary>
    ///   Verifies functionality of the StartNewGame method.
    /// </summary>
    ///
    [Test]
    public async Task StartNewGameCallsRenderer()
    {
        var mockCache = Substitute.For<IMemoryCache>();
        var mockBotInterface = Substitute.For<IGameInterface>();
        var mockRenderer = Substitute.For<McpRenderer>(GameState.CreateDefault());

        mockRenderer.RenderGameState(Arg.Any<GameState>()).Returns("Expected Output");

        var tools = new NumericTicTacToeGameTools(mockCache, mockBotInterface, mockRenderer);

        // Start a new game and get the rendered output.

        var result = await tools.StartNewGame();

        // Verify the renderer was called and its output returned.

        Assert.That(result, Is.EqualTo("Expected Output"));
        mockRenderer.Received(1).RenderGameState(Arg.Any<GameState>());
    }

    /// <summary>
    ///   Verifies functionality of the StartNewGame method.
    /// </summary>
    ///
    [Test]
    public async Task StartNewGameSkipsBotMoveForOddHumanPlayer()
    {
        var mockBotInterface = Substitute.For<IGameInterface>();
        var mockCache = Substitute.For<IMemoryCache>();
        var mockRenderer = Substitute.For<McpRenderer>(GameState.CreateDefault());

        var tools = new NumericTicTacToeGameTools(mockCache, mockBotInterface, mockRenderer);
        await tools.StartNewGame(PlayerToken.Odd, Difficulty.Easy);

        // Bot should not have been used.

        await mockBotInterface
            .DidNotReceive()
            .RenderPlayerTextAsync(Arg.Any<TextType>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    ///   Verifies functionality of the MakeMove method.
    /// </summary>
    ///
    [Test]
    public async Task MakeMoveThrowsForMissingGameState()
    {
        var mockCache = Substitute.For<IMemoryCache>();
        var mockBotInterface = Substitute.For<IGameInterface>();
        var mockRenderer = Substitute.For<McpRenderer>(GameState.CreateDefault());

        // Set up cache to return false for TryGetValue (game not found).

        mockCache
            .TryGetValue(Arg.Any<string>(), out Arg.Any<object?>())
            .Returns(false);

        var tools = new NumericTicTacToeGameTools(mockCache, mockBotInterface, mockRenderer);

        await Assert.ThatAsync(async () => await tools.MakeMove(1, 1, "missingGame"),
            Throws.InvalidOperationException.With.Message.Contain("No game found"));
    }

    /// <summary>
    ///   Verifies functionality of the MakeMove method.
    /// </summary>
    ///
    /// <param name="position">The position value to test.</param>
    ///
    [Test]
    [TestCase(-1)]
    [TestCase(10)]
    public async Task MakeMoveThrowsForInvalidPositionAfterAdjustment(int position)
    {
        var mockCache = Substitute.For<IMemoryCache>();

        // Set up cache to return cached game.

        mockCache.TryGetValue(Arg.Any<string>(), out Arg.Any<object?>())
            .Returns(x =>
            {
                x[1] = (GameState.CreateDefault(), PlayerToken.Odd, Difficulty.Easy);
                return true;
            });

        var mockBotInterface = Substitute.For<IGameInterface>();
        var mockRenderer = Substitute.For<McpRenderer>(GameState.CreateDefault());
        var tools = new NumericTicTacToeGameTools(mockCache, mockBotInterface, mockRenderer);

        // Verify that invalid positions throw ArgumentOutOfRangeException.

        await Assert.ThatAsync(async () => await tools.MakeMove(position, 1),
            Throws.TypeOf<ArgumentOutOfRangeException>()
                .With.Property("ParamName").EqualTo("position"));
    }

    /// <summary>
    ///   Verifies functionality of the MakeMove method.
    /// </summary>
    ///
    /// <param name="humanType">The human player type.</param>
    /// <param name="invalidToken">The invalid token for that player type.</param>
    ///
    [Test]
    [TestCase(PlayerToken.Odd, (byte)2)]
    [TestCase(PlayerToken.Odd, (byte)4)]
    [TestCase(PlayerToken.Even, (byte)1)]
    [TestCase(PlayerToken.Even, (byte)3)]
    public async Task MakeMoveThrowsForInvalidTokenForPlayer(PlayerToken humanType, byte invalidToken)
    {
        var mockCache = Substitute.For<IMemoryCache>();

        // Set up cache to return cached game.

        mockCache.TryGetValue(Arg.Any<string>(), out Arg.Any<object?>())
            .Returns(x =>
            {
                x[1] = (GameState.CreateDefault(), humanType, Difficulty.Easy);
                return true;
            });

        var mockBotInterface = Substitute.For<IGameInterface>();
        var mockRenderer = Substitute.For<McpRenderer>(GameState.CreateDefault());
        var tools = new NumericTicTacToeGameTools(mockCache, mockBotInterface, mockRenderer);

        // Verify that invalid tokens for the player type throw ArgumentOutOfRangeException.

        await Assert.ThatAsync(async () => await tools.MakeMove(1, invalidToken),
            Throws.TypeOf<ArgumentOutOfRangeException>()
                .With.Property("ParamName").EqualTo("token"));
    }

    /// <summary>
    ///   Verifies functionality of the MakeMove method.
    /// </summary>
    ///
    [Test]
    public async Task MakeMoveThrowsWhenGameIsAlreadyOver()
    {
        var completedState = GameState.CreateDefault();

        // Create a winning scenario: 3+5+7=15 in top row.

        completedState.ApplyMove(new Move(PlayerToken.Odd, 0, 3));
        completedState.ApplyMove(new Move(PlayerToken.Even, 3, 2));
        completedState.ApplyMove(new Move(PlayerToken.Odd, 1, 5));
        completedState.ApplyMove(new Move(PlayerToken.Even, 4, 4));
        completedState.ApplyMove(new Move(PlayerToken.Odd, 2, 7)); // Wins

        var mockCache = Substitute.For<IMemoryCache>();

        // Set up cache to return completed game.

        mockCache.TryGetValue(Arg.Any<string>(), out Arg.Any<object?>())
            .Returns(x =>
            {
                x[1] = (completedState, PlayerToken.Odd, Difficulty.Easy);
                return true;
            });

        var mockBotInterface = Substitute.For<IGameInterface>();
        var mockRenderer = Substitute.For<McpRenderer>(completedState);
        var tools = new NumericTicTacToeGameTools(mockCache, mockBotInterface, mockRenderer);

        // Verify that attempting a move on a completed game throws InvalidOperationException.

        await Assert.ThatAsync(async () => await tools.MakeMove(4, 9),
            Throws.InvalidOperationException
                .With.Message.Contain("No game in progress"));
    }

    /// <summary>
    ///   Verifies functionality of the MakeMove method.
    /// </summary>
    ///
    [Test]
    public async Task MakeMoveRemovesGameFromCacheWhenItEnds()
    {
        var nearWinState = GameState.CreateDefault();

        // Create near-win scenario: 3+5 in top row, need 7 to win.

        nearWinState.ApplyMove(new Move(PlayerToken.Odd, 0, 3));
        nearWinState.ApplyMove(new Move(PlayerToken.Even, 3, 2));
        nearWinState.ApplyMove(new Move(PlayerToken.Odd, 1, 5));
        nearWinState.ApplyMove(new Move(PlayerToken.Even, 4, 4));

        var mockCache = Substitute.For<IMemoryCache>();

        // Set up cache to return near-win game.

        mockCache.TryGetValue(Arg.Any<string>(), out Arg.Any<object?>())
            .Returns(x =>
            {
                x[1] = (nearWinState, PlayerToken.Odd, Difficulty.Easy);
                return true;
            });

        var mockBotInterface = Substitute.For<IGameInterface>();
        var mockRenderer = Substitute.For<McpRenderer>(nearWinState);

        mockRenderer.RenderGameState(Arg.Any<GameState>()).Returns("Rendered");

        var tools = new NumericTicTacToeGameTools(mockCache, mockBotInterface, mockRenderer);

        // Make the winning move (position 3 = index 2, token 7 completes 3+5+7=15).

        await tools.MakeMove(3, 7);

        // Verify the completed game was removed from cache.

        mockCache.Received(1).Remove(Arg.Is<string>(k => k.Contains("default")));
    }

    /// <summary>
    ///   Verifies functionality of the DisplayBoard method.
    /// </summary>
    ///
    [Test]
    public async Task DisplayBoardThrowsForMissingGameState()
    {
        var mockCache = Substitute.For<IMemoryCache>();
        var mockBotInterface = Substitute.For<IGameInterface>();
        var mockRenderer = Substitute.For<McpRenderer>(GameState.CreateDefault());

        // Set up cache to return false for TryGetValue.

        mockCache
            .TryGetValue(Arg.Any<string>(), out Arg.Any<object?>())
            .Returns(false);

        var tools = new NumericTicTacToeGameTools(mockCache, mockBotInterface, mockRenderer);

        await Assert.ThatAsync(async () => await tools.DisplayBoard("missingGame"),
            Throws.InvalidOperationException.With.Message.Contain("No game found"));
    }

    /// <summary>
    ///   Verifies functionality of the DisplayBoard method.
    /// </summary>
    ///
    [Test]
    public async Task DisplayBoardCallsRendererWithCachedGameState()
    {
        var gameState = GameState.CreateDefault();
        var mockCache = Substitute.For<IMemoryCache>();

        // Set up cache to return game state.

        mockCache.TryGetValue(Arg.Any<string>(), out Arg.Any<object?>())
            .Returns(x =>
            {
                x[1] = (gameState, PlayerToken.Odd, Difficulty.Easy);
                return true;
            });

        var mockBotInterface = Substitute.For<IGameInterface>();
        var mockRenderer = Substitute.For<McpRenderer>(gameState);

        mockRenderer
            .RenderBoard(Arg.Any<GameState>())
            .Returns("Board Output");

        var tools = new NumericTicTacToeGameTools(mockCache, mockBotInterface, mockRenderer);

        // Display the board and get the rendered output.

        var result = await tools.DisplayBoard();

        // Verify the renderer was called with the cached game state.

        Assert.That(result, Is.EqualTo("Board Output"));
        mockRenderer.Received(1).RenderBoard(Arg.Any<GameState>());
    }

    /// <summary>
    ///   Verifies functionality of the GetAvailableTokens method.
    /// </summary>
    ///
    /// <param name="playerType">The player type to get tokens for.</param>
    ///
    [Test]
    [TestCase(PlayerToken.Odd)]
    [TestCase(PlayerToken.Even)]
    public async Task GetAvailableTokensReturnsFormattedStringForPlayer(PlayerToken playerType)
    {
        var mockCache = Substitute.For<IMemoryCache>();

        // Set up cache to return game state.

        mockCache.TryGetValue(Arg.Any<string>(), out Arg.Any<object?>())
            .Returns(x =>
            {
                x[1] = (GameState.CreateDefault(), PlayerToken.Odd, Difficulty.Easy);
                return true;
            });

        var mockBotInterface = Substitute.For<IGameInterface>();
        var mockRenderer = Substitute.For<McpRenderer>(GameState.CreateDefault());
        var tools = new NumericTicTacToeGameTools(mockCache, mockBotInterface, mockRenderer);

        // Get the available tokens for the specified player type.

        var result = await tools.GetAvailableTokens(playerType);

        // Verify the result contains expected formatting and player type.

        Assert.That(result, Does.Contain($"Available tokens for {playerType} player:"));
        Assert.That(result, Does.Contain("{"));
        Assert.That(result, Does.Contain("}"));
    }

    /// <summary>
    ///   Verifies functionality of the ExplainGameRulesAsync method.
    /// </summary>
    ///
    [Test]
    public async Task ExplainGameRulesAsyncReturnsRulesWithKeyElements()
    {
        var mockCache = Substitute.For<IMemoryCache>();
        var mockBotInterface = Substitute.For<IGameInterface>();
        var mockRenderer = Substitute.For<McpRenderer>(GameState.CreateDefault());
        var tools = new NumericTicTacToeGameTools(mockCache, mockBotInterface, mockRenderer);

        // Get the game rules explanation.

        var result = await tools.ExplainGameRulesAsync();

        // Verify key sections are present in the rules content.

        Assert.That(result, Does.Contain("Objective"));
        Assert.That(result, Does.Contain("How to Win"));
        Assert.That(result, Does.Contain("15")); // Winning sum
    }

    /// <summary>
    ///   Verifies functionality of the ListCommands method.
    /// </summary>
    ///
    [Test]
    public async Task ListCommandsReturnsAllToolNames()
    {
        var mockCache = Substitute.For<IMemoryCache>();
        var mockBotInterface = Substitute.For<IGameInterface>();
        var mockRenderer = Substitute.For<McpRenderer>(GameState.CreateDefault());
        var tools = new NumericTicTacToeGameTools(mockCache, mockBotInterface, mockRenderer);

        // Get the list of all available commands.

        var result = await tools.ListCommands();

        // Verify all tool names are present in the output.

        Assert.That(result, Does.Contain("start_new_game"));
        Assert.That(result, Does.Contain("make_move"));
        Assert.That(result, Does.Contain("display_board"));
        Assert.That(result, Does.Contain("get_available_tokens"));
        Assert.That(result, Does.Contain("explain_game_rules"));
        Assert.That(result, Does.Contain("list_commands"));
    }
}
