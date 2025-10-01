using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Caching.Memory;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Squire.NumTic.Contracts;
using Squire.NumTic.Players;

namespace Squire.NumTic.Mcp;

/// <summary>
///   The set of tools to be used by the LLM for game play.
/// </summary>
///
[McpServerToolType]
[Description("A set of tools for managing and playing games of Numeric Tic-Tac-Toe.")]
public class NumericTicTacToeGameTools
{
    /// <summary>The default set of options for game state cache items.</summary>
    private static readonly MemoryCacheEntryOptions DefaultCacheEntryOptions =
        new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(30))
            .SetAbsoluteExpiration(TimeSpan.FromHours(2))
            .SetPriority(CacheItemPriority.Normal);

    /// <summary>The game interface to use with the bot player.</summary>
    private readonly IGameInterface BotGameInterface;

    /// <summary>The memory cache for storing game state across tool invocations.</summary>
    private readonly IMemoryCache Cache;

    /// <summary>The renderer for the default game state.</summary>
    private readonly McpRenderer Renderer;

    /// <summary>The factory to use for creating an instance of <see cref="GameState" />.</summary>
    private readonly Func<GameState> GameStateFactory;

    /// <summary>
    ///   Initializes a new instance of the <see cref="NumericTicTacToeGameTools"/> class.
    /// </summary>
    ///
    /// <param name="cache">The memory cache for storing game state across tool invocations.</param>
    /// <param name="botGameInterface">The game interface to use with the bot player.</param>
    /// <param name="renderer">The renderer for the game state associated with a default 3x3 board.</param>
    ///
    public NumericTicTacToeGameTools(IMemoryCache cache,
                                     IGameInterface botGameInterface,
                                     McpRenderer renderer) : this(cache, botGameInterface, renderer, GameState.CreateDefault)
    {
    }

    /// <summary>
    ///   Initializes a new instance of the <see cref="NumericTicTacToeGameTools"/> class.
    /// </summary>
    ///
    /// <param name="cache">The memory cache for storing game state across tool invocations.</param>
    /// <param name="botGameInterface">The game interface to use with the bot player.</param>
    /// <param name="renderer">The renderer for the default game state.</param>
    /// <param name="gameStateFactory">The factory to use for creating an instance of <see cref="GameState" />.</param>
    ///
    /// <remarks>
    ///   This constructor is intended to be called only in unit testing scenarios
    ///   to allow for mocking or substituting the <see cref="GameState" /> instance.
    /// </remarks>
    ///
    internal NumericTicTacToeGameTools(IMemoryCache cache,
                                       IGameInterface botGameInterface,
                                       McpRenderer renderer,
                                       Func<GameState> gameStateFactory)
    {
        ArgumentNullException.ThrowIfNull(cache, nameof(cache));
        ArgumentNullException.ThrowIfNull(botGameInterface, nameof(botGameInterface));

        Cache = cache;
        BotGameInterface = botGameInterface;
        Renderer = renderer;
        GameStateFactory = gameStateFactory;
    }

    /// <summary>
    ///   Starts a new game with a human player and automated bot player.
    /// </summary>
    ///
    /// <param name="humanPlayerType">The type of tokens that the human will play (Odd or Even).  If not specified, the human will play the Odd tokens.</param>
    /// <param name="selectedDifficulty">The difficulty level of the bot player.  If not specified, the bot player will use medium difficulty.</param>
    /// <param name="gameId">Unique identifier for this game session.  If not specified, the identifier "default" will be used.</param>
    /// <param name="cancellationToken">A token that can be used to signal a request for cancellation.</param>
    ///
    /// <returns>The rendered content of the initial game state.</returns>
    ///
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    ///
    [McpServerTool(Name ="start_new_game")]
    [Description("Starts a new game of Numeric Tic-Tac-Toe against a bot opponent.")]
    public async Task<string> StartNewGame([Description("The type of tokens the human will play (Odd or Even).")] PlayerToken humanPlayerType = PlayerToken.Odd,
                                           [Description("The difficulty level of the bot player.")] Difficulty selectedDifficulty = Difficulty.Medium,
                                           [Description("Unique identifier for this game session.")] string gameId = "default",
                                           CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // If a new game is started while one is already in progress, clean up first.

        if (TryGetCachedGameState(gameId, out _))
        {
            RemoveCachedGameState(gameId);
        }

        // Start a new game.

        var gameState = GameStateFactory();

        var botPlayer = new BotPlayer(BotGameInterface, new BotPlayerOptions
        {
            Difficulty = selectedDifficulty
        });

        // If the bot player is odd, it goes first.  Make its move so that
        // the next turn belongs to the human player.

        if (humanPlayerType != PlayerToken.Odd)
        {
            var botMove = await botPlayer.PlayTurnAsync(gameState, cancellationToken);
            gameState.ApplyMove(botMove);
        }

        // Cache the game state for future tool calls.

        CacheGameState(gameId, gameState, humanPlayerType, selectedDifficulty);

        // Render the initial game state.

        return Renderer.RenderGameState(gameState);
    }

    /// <summary>
    ///   Makes a move for the human player in the current game.
    /// </summary>
    ///
    /// <param name="position">The position (1-9) where to place the token.</param>
    /// <param name="token">The token value to place (must be byte: 1,3,5,7,9 for odd player, 2,4,6,8 for even player).</param>
    /// <param name="gameId">Unique identifier for this game session.  If not specified, the identifier "default" will be used.</param>
    /// <param name="cancellationToken">A token that can be used to signal a request for cancellation.</param>
    ///
    /// <returns>The rendered content of the updated game state.</returns>
    ///
    /// <exception cref="InvalidOperationException">Thrown when no game is in progress or move is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="position"/> or <paramref name="token"/> are invalid.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    ///
    [McpServerTool(Name = "make_move")]
    [Description("Makes a move for the human player in the current game of Numeric Tic-Tac-Toe.")]
    public async Task<string> MakeMove([Description("The position (1-9) where to place the token.")] int position,
                                       [Description("The token value to place (must be byte: { 1,3,5,7,9 } for the odd player, { 2,4,6,8 } for the even player).")] byte token,
                                       [Description("Unique identifier for this game session.")] string gameId = "default",
                                       CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // If there is no active game, then no move can be made.

        var (gameState, humanPlayerType, difficulty) = TryGetCachedGameState(gameId, out var cachedGame) switch
        {
            true => cachedGame.Value,
            false => throw new InvalidOperationException($"No game found for gameId '{gameId}'. Start a new game first.")
        };

        if (gameState.IsGameOver)
        {
            throw new InvalidOperationException("No game in progress. Start a new game before making a move");
        }

        // If the position is out of range after adjusting to an index, throw.

        --position;

        if ((uint)position >= (uint)gameState.Board.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(position), $"The position must be between 1 and {gameState.Board.Length}, inclusive.");
        }

        if (!gameState.GetPlayerTokens(humanPlayerType).Contains(token))
        {
            throw new ArgumentOutOfRangeException(nameof(token), $"The token value {token} is not valid for the player {humanPlayerType}. Valid values are: {string.Join(", ", gameState.GetPlayerTokens(humanPlayerType))}");
        }

        // Make the human player's move.

        var move = new Move(humanPlayerType, position, token);
        gameState.ApplyMove(move);

        // Allow the bot to make a move if the game is not over.

        if (!gameState.IsGameOver)
        {
            var botPlayer = new BotPlayer(BotGameInterface, new BotPlayerOptions
            {
                Difficulty = difficulty
            });

            var botMove = await botPlayer.PlayTurnAsync(gameState, cancellationToken);
            gameState.ApplyMove(botMove);
        }

        // If the game is over, remove it from the cache. Otherwise,
        // refresh the cached state.

        if (gameState.IsGameOver)
        {
            RemoveCachedGameState(gameId);
        }
        else
        {
            CacheGameState(gameId, gameState, humanPlayerType, difficulty);
        }

        // Render the current game state.

        return Renderer.RenderGameState(gameState);
    }

    /// <summary>
    ///   Displays the Numeric Tic-Tac-Toe board for the game in progress.
    /// </summary>
    ///
    /// <param name="gameId">Unique identifier for this game session.  If not specified, the identifier "default" will be used.</param>
    /// <param name="cancellationToken">A token that can be used to signal a request for cancellation.</param>
    ///
    /// <returns>The rendered content for the board and current game state.</returns>
    ///
    /// <exception cref="InvalidOperationException">Thrown when no game is in progress.</exception>
    ///
    [McpServerTool(Name = "display_board")]
    [Description("Displays the Numeric Tic-Tac-Toe board for the current game in progress.")]
    public Task<string> DisplayBoard([Description("Unique identifier for this game session.")] string gameId = "default",
                                     CancellationToken cancellationToken = default)
    {
        // If there is no active game, then no move can be made.

        var (gameState, _, _) = TryGetCachedGameState(gameId, out var cachedGame) switch
        {
            true => cachedGame.Value,
            false => throw new InvalidOperationException($"No game found for gameId '{gameId}'. Start a new game first.")
        };

        return Task.FromResult(Renderer.RenderBoard(gameState));
    }

    /// <summary>
    ///   Gets the available tokens for the specified player type.
    /// </summary>
    ///
    /// <param name="playerType">Which player's tokens should be checked (Odd or Even)."</param>
    /// <param name="gameId">Unique identifier for this game session.  If not specified, the identifier "default" will be used.</param>
    /// <param name="cancellationToken">A token that can be used to signal a request for cancellation.</param>
    ///
    /// <returns>The available tokens for the specified player type.</returns>
    ///
    /// <exception cref="InvalidOperationException">Thrown when no game is in progress.</exception>
    ///
    [McpServerTool(Name = "get_available_tokens")]
    [Description("Gets the available tokens for the specified player type.")]
    public Task<string> GetAvailableTokens([Description("Which player's tokens should be checked (Odd or Even).")] PlayerToken playerType = PlayerToken.Odd,
                                           [Description("Unique identifier for this game session.")] string gameId = "default",
                                           CancellationToken cancellationToken = default)
    {
        // If there is no active game, then no move can be made.

        var (gameState, _, _) = TryGetCachedGameState(gameId, out var cachedGame) switch
        {
            true => cachedGame.Value,
            false => throw new InvalidOperationException($"No game found for gameId '{gameId}'. Start a new game first.")
        };

        var tokensText = string.Join(", ", gameState.GetPlayerTokens(playerType));
        return Task.FromResult($"Available tokens for {playerType} player: {{ {tokensText} }}");
    }

    /// <summary>
    ///   Explains the rules of Numeric Tic-Tac-Toe.
    /// </summary>
    ///
    /// <param name="cancellationToken">A token that can be used to signal a request for cancellation.</param>
    ///
    /// <returns>The game rules explanation content.</returns>
    ///
    [McpServerTool(Name = "explain_game_rules")]
    [Description("Explains the rules of Numeric Tic-Tac-Toe and provides examples of common game commands.")]
    public Task<string> ExplainGameRulesAsync(CancellationToken cancellationToken = default)
    {
        const string rules = """
            # Numeric Tic-Tac-Toe Rules

            ## Objective

            Win by getting three tokens in a row that sum to exactly **15**.

            ## Game Setup

            - Players take turns placing numbered tokens on a 3×3 grid
            - **Odd Player** uses numbers: 1, 3, 5, 7, 9
            - **Even Player** uses numbers: 2, 4, 6, 8
            - Each number can only be used once in the game

            ## How to Win

            - Get three tokens in a row, column, or diagonal that sum to **15**
            - Examples of winning combinations:
              - 1 + 5 + 9 = 15 (odd numbers)
              - 2 + 6 + 7 = 15 (mixed numbers)
              - 3 + 4 + 8 = 15 (mixed numbers)

            ## Board Positions

            Use position numbers 1-9 to place tokens:
            ```text
             1 | 2 | 3
            -----------
             4 | 5 | 6
            -----------
             7 | 8 | 9
            ```

            ## Common Commands to Try

            Ask me things like:
            - "Start a new game" or "Begin playing with me using odd tokens"
            - "Place token 7 at position 5" or "Put 3 in the center"
            - "Show the current board" or "What does the board look like?"
            - "What tokens can I use?" or "What numbers are available for the odd player?"
            - "Make a move with 4 at position 1" or "Place 2 in the top-left corner"

            Good luck and have fun!
            """;

        return Task.FromResult(rules);
    }

    /// <summary>
    ///   Returns detailed information about all available Numeric Tic-Tac-Toe tools and their parameters.
    /// </summary>
    ///
    /// <returns>Detailed help information about all available tools.</returns>
    ///
    [McpServerTool(Name = "list_commands")]
    [Description("Returns detailed information about all available tools and their usage. Provides comprehensive help for all Numeric Tic-Tac-Toe MCP tools.")]
    public Task<string> ListCommands()
    {
        var helpText = """
            # Numeric Tic-Tac-Toe MCP Tools

            ## start_new_game
            Starts a new game of Numeric Tic-Tac-Toe against a bot opponent.
            - **Parameters**:
              - `humanPlayerType`: PlayerToken (Odd|Even, default: Odd)
              - `selectedDifficulty`: Difficulty (Easy|Medium|Hard, default: Medium)
            - **Example**: humanPlayerType=Odd, selectedDifficulty=Medium

            ## make_move
            Makes a move for the human player in the current game.
            - **Parameters**:
              - `position`: int (1-9, board position using standard numpad layout)
              - `token`: byte (Odd player: 1,3,5,7,9 | Even player: 2,4,6,8)
            - **Example**: position=5, token=3

            ## display_board
            Displays the current game state with board visualization.
            - **Parameters**: None
            - **Description**: Shows the current board state, player turn, and game status

            ## get_available_tokens
            Gets the available tokens for the specified player type.
            - **Parameters**:
              - `playerType`: PlayerToken (Odd|Even, default: Odd)
            - **Example**: playerType=Odd

            ## explain_game_rules
            Displays the rules of Numeric Tic-Tac-Toe and provides examples of common game commands.

            ## list_commands
            Displays this detailed help information.
            - **Parameters**: None
            """;

        return Task.FromResult(helpText);
    }

    /// <summary>
    ///   Caches game state with associated metadata for retrieval by game.
    /// </summary>
    ///
    /// <param name="gameId">The unique identifier for the game session.</param>
    /// <param name="gameState">The game state to cache.</param>
    /// <param name="humanPlayerType">The human player token type.</param>
    /// <param name="botDifficulty">The difficulty level of the bot player.</param>
    ///
    private void CacheGameState(string gameId,
                                GameState gameState,
                                PlayerToken humanPlayerType,
                                Difficulty botDifficulty)
    {
        ArgumentNullException.ThrowIfNull(gameState, nameof(gameState));

        var cacheData = (gameState, humanPlayerType, botDifficulty);
        Cache.Set(CreateCacheKey(gameId), cacheData, DefaultCacheEntryOptions);
    }

    /// <summary>
    ///   Retrieves cached game state and metadata by game, if present.
    /// </summary>
    ///
    /// <param name="gameId">The unique identifier for the game session.</param>
    /// <param name="state">The cached game state, human player type, and bot difficulty, if found; otherwise, <c>null</c>.</param>
    ///
    /// <returns><c>true</c> if a game with <paramref name="gameId" /> was found in the cache; otherwise, <c>false</c>.</returns>
    ///
    private bool TryGetCachedGameState(string gameId,
                                       [NotNullWhen(true)] out (GameState GameState, PlayerToken HumanPlayerType, Difficulty botDifficulty)? state)
    {
       var isCached = Cache.TryGetValue(CreateCacheKey(gameId), out var cached);

       state = cached switch
       {
           null => null,
           _ => ((GameState, PlayerToken, Difficulty))cached
        };

       return isCached;
    }

    /// <summary>
    ///   Removes cached game state for the specified game.
    /// </summary>
    ///
    /// <param name="gameId">The unique identifier for the game session.</param>
    ///
    private void RemoveCachedGameState(string gameId) => Cache.Remove(CreateCacheKey(gameId));

    /// <summary>
    ///   Creates a cache key for the specified game.
    /// </summary>
    ///
    /// <param name="gameId">The unique identifier for the game session.</param>
    ///
    /// <returns>The cache key for the specified <paramref name="gameId" />.</returns>
    ///
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string CreateCacheKey(string gameId)
    {
        ArgumentNullException.ThrowIfNull(gameId, nameof(gameId));
        return $"{nameof(NumericTicTacToeGameTools)}_{gameId}";
    }
}
