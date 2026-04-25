using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Encodings.Web;
using System.Text.Json;
using OpenAI.Responses;
using Squire.NumTic.Contracts;
using Squire.NumTic.Players;

namespace Squire.NumTic.AI;

// OpenAI features are mostly still in an experimental state and require opt-in by disabling warnings.

#pragma warning disable OPENAI001
#pragma warning disable SCME0001

/// <summary>
///   An automated player implementation based on interactions with an OpenAI-based
///   model.
/// </summary>
///
/// <remarks>
///   This player uses the OpenAI API to evaluate the current state of the game and
///   generate moves.  It requires network connectivity and access to an OpenAI-compatible
///   service.
/// </remarks>
///
public class OpenAIPlayer : IPlayer
{
    /// <summary>The set of serializer options to use when processing model responses.</summary>
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        TypeInfoResolver = AISerializerContext.Default
    };

    /// <summary>The game interface to interact with for player operations.</summary>
    private readonly IGameInterface Interface;

    /// <summary>The options to use for player behavior.</summary>
    private readonly OpenAIPlayerOptions Options;

    /// <summary>The client to use for interacting with the OpenAI Responses API.</summary>
    private readonly ResponsesClient ResponseClient;

    /// <summary>The set of options to use for interacting with the OpenAI Responses API.</summary>
    private readonly CreateResponseOptions ResponseOptions;

    /// <summary>The set of items that comprise the conversation history with the LLM for the game.</summary>
    private readonly List<ResponseItem> ConversationHistory = new();

    /// <summary>The maximum moves made by the model since the last random move.</summary>
    private int _movesSinceLastRandom = 0;

    /// <summary>
    ///   Initializes a new instance of the <see cref="ConsolePlayer"/> class.
    /// </summary>
    ///
    /// <param name="gameInterface">The game interface to interact with for player operations.</param>
    /// <param name="responsesClient">The OpenAI Responses client to use for model interactions.</param>
    /// <param name="gameState">The state that the current game is based on.</param>
    /// <param name="options">The set of options to use for configuring player behavior.  If not provided a default set is assumed.</param>
    ///
    /// <exception cref="ArgumentNullException">Occurs when the <paramref name="gameInterface"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">Occurs when the <paramref name="responsesClient"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">Occurs when the <paramref name="gameState"/> is <c>null</c>.</exception>
    ///
    public OpenAIPlayer(IGameInterface gameInterface,
                        ResponsesClient responsesClient,
                        GameState gameState,
                        OpenAIPlayerOptions? options = default)
    {
        Interface = gameInterface ?? throw new ArgumentNullException(nameof(gameInterface));
        Options = options?.Clone() ?? OpenAIPlayerOptions.Default;

        ResponseClient = responsesClient ?? throw new ArgumentNullException(nameof(responsesClient));

        ArgumentNullException.ThrowIfNull(gameState, nameof(gameState));

        ResponseOptions = new CreateResponseOptions
        {
            Model = Options.ModelName,
            Instructions = GenerateInstructions(gameState, Options),
            TextOptions = new ResponseTextOptions
            {
                TextFormat = ResponseTextFormat.CreateJsonSchemaFormat("response", GenerateJsonResponseSchema(gameState))
            }
        };
    }

    /// <summary>
    ///   Plays a turn in the game based on the current game state.
    /// </summary>
    ///
    /// <param name="gameState">The current state of the game.</param>
    /// <param name="cancellationToken">A token that can be used to signal a request for cancellation.</param>
    ///
    /// <returns>The move that was made.</returns>
    ///
    /// <exception cref="ArgumentNullException">Thrown when gameState is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when maximum retries exceeded or response is malformed.</exception>
    /// <exception cref="OperationCanceledException">Occurs when the turn was canceled.</exception>
    ///
    public async Task<Move> PlayTurnAsync(GameState gameState,
                                          CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(gameState, nameof(gameState));
        cancellationToken.ThrowIfCancellationRequested();

        int retryCount = 0;

        // Add current game state to conversation history.

        ConversationHistory.Add(
            ResponseItem.CreateUserMessageItem(CreateTurnPrompt(gameState, _movesSinceLastRandom)));

        while (retryCount < Options.MaxMoveRetries)
        {
            // Call API with conversation history and instructions.

            ResponseOptions.InputItems.Clear();

            foreach (var item in ConversationHistory)
            {
                ResponseOptions.InputItems.Add(item);
            }

            var response =
                await ResponseClient.CreateResponseAsync(
                    ResponseOptions,
                    cancellationToken).ConfigureAwait(false);

            // Extract the response and preserve it in the conversation history.

            var assistantResponse = response.Value.GetOutputText();
            ConversationHistory.Add(ResponseItem.CreateAssistantMessageItem(assistantResponse));

            // Attempt to parse and validate the move.

            var parseResult = ParseMove(assistantResponse, gameState);

            if (parseResult.IsValid)
            {
                _movesSinceLastRandom = parseResult.IsRandom switch
                {
                    true => 0,
                    false => _movesSinceLastRandom + 1
                };

                return parseResult.Move!.Value;
            }

            // Move was invalid - send error message back to the model for a retry.

            ++retryCount;

            if (retryCount < Options.MaxMoveRetries)
            {
                var errorMessage = $"Invalid move: {parseResult.ErrorMessage}. Repeat the previous task and return a valid move in the correct response JSON format.";
                ConversationHistory.Add(ResponseItem.CreateUserMessageItem(errorMessage));
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        throw new InvalidOperationException($"Failed to get valid move after {retryCount} attempts.");
    }

    /// <summary>
    ///   Serializes the game state into a compact pipe-separated format for the user message.
    /// </summary>
    ///
    /// <param name="gameState">The current game state.</param>
    /// <param name="movesSinceLastRandom">The number of moves since the last random move.</param>
    ///
    /// <returns>A formatted string containing board state, available tokens, and move counter.</returns>
    ///
    private static string CreateTurnPrompt(GameState gameState,
                                           int movesSinceLastRandom)
    {
        var board = string.Join("|", gameState.Board);
        var myTokens = string.Join(",", gameState.CurrentPlayerTokens);

        // Get opponent's tokens by checking the other player.

        var opponentPlayer = gameState.CurrentTurn == PlayerToken.Odd
            ? PlayerToken.Even
            : PlayerToken.Odd;

        var opponentTokens = string.Join(",", gameState.GetPlayerTokens(opponentPlayer));

        return $$"""
        You are playing a turn for {{gameState.CurrentTurn}} and should make your move based on the following game state:

        Board: {{board}}
        Your available tokens: {{myTokens}}
        Opponent's available tokens: {{opponentTokens}}
        Moves since last random: {{movesSinceLastRandom}}
        """;
    }

    /// <summary>
    ///   Attempts to parse and validate the model's JSON response into a Move.
    /// </summary>
    ///
    /// <param name="responseJson">The JSON response from the model.</param>
    /// <param name="gameState">The current game state for validation.</param>
    ///
    /// <returns>A <see cref="ParseResult" /> indicating success or failure with error details.</returns>
    ///
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Options with AOT-safe context are used.")]
    [UnconditionalSuppressMessage("Trimming", "IL3050:Using member 'System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)' which has 'RequiresDynamicCodeAttribute' can break functionality when AOT compiling.", Justification = "Options with AOT-safe context are used.")]
    private static ParseResult ParseMove(string responseJson,
                                         GameState gameState)
    {
        if (responseJson is { Length: 0 })
        {
            return ParseResult.Failure("No response JSON was returned.");
        }

        MoveResponse? moveResponse;

        // Attempt to deserialize the JSON response.

        try
        {
            moveResponse = JsonSerializer.Deserialize<MoveResponse>(responseJson, SerializerOptions);

            if (moveResponse is null)
            {
                return ParseResult.Failure("Response was not valid JSON and could not be deserialized.");
            }
        }
        catch (JsonException ex)
        {
            return ParseResult.Failure($"Invalid JSON: {ex.Message}");
        }

        // Validate the position selected.

        if ((moveResponse.Position < 0) || (moveResponse.Position >= gameState.Board.Length))
        {
            return ParseResult.Failure( $"Position {moveResponse.Position} is out of bounds. Valid positions are 0-{gameState.Board.Length - 1}.");
        }

        if (gameState.Board[moveResponse.Position] != GameState.EmptyBoardSpaceValue)
        {
            return ParseResult.Failure($"Position {moveResponse.Position} is already occupied.");
        }

        // Validate token played.

        if (!gameState.CurrentPlayerTokens.Contains(moveResponse.Token))
        {
            var available = string.Join(", ", gameState.CurrentPlayerTokens);
            return ParseResult.Failure($"Token {moveResponse.Token} is not available. Your available tokens are: {available}");
        }

        // Create and return successful result.

        var move = new Move(
            gameState.CurrentTurn,
            moveResponse.Position,
            moveResponse.Token);

        return ParseResult.Success(move, moveResponse.Random);
    }

    /// <summary>
    ///   Generates the instructions for the model to use when playing the game.
    /// </summary>
    ///
    /// <param name="gameState">The initial state that the current game is based on.</param>
    /// <param name="options">The set of options to use for configuring player behavior.</param>
    ///
    /// <returns>The instructions for the model.</returns>
    ///
    private static string GenerateInstructions(GameState gameState,
                                               OpenAIPlayerOptions options)
    {
        var tokensPerRow = gameState.TokensPerRow;
        var boardSize = tokensPerRow * tokensPerRow;
        var winningTotal = gameState.WinningTotal;

         var strategyInstructions = options.Difficulty switch
        {
            Difficulty.Easy => @"""
            Play casually with basic tactics. Consider obvious moves but don't analyze deeply. Make moves that feel natural without extensive calculation.

            RANDOMIZATION: Every 3rd move (when 'Moves since last random' reaches 3), you must make a completely random move. Select any valid position and any available token randomly, then set 'random' to true in your response.
            """,

            Difficulty.Medium => @"""
            Play with moderate strategic thinking. Look for immediate winning opportunities and block obvious threats, but don't calculate multiple moves ahead consistently.

            RANDOMIZATION: Every 5th move (when 'Moves since last random' reaches 5), you must make a completely random move. Select any valid position and any available token randomly, then set 'random' to true in your response.
            """,

            Difficulty.Hard => @"""
            Play with strong tactical awareness. Analyze winning combinations, block opponent threats proactively, and set up multi-move strategies when possible.

            RANDOMIZATION: Every 7th move (when 'Moves since last random' reaches 7), you must make a completely random move. Select any valid position and any available token randomly, then set 'random' to true in your response.
            """,

            Difficulty.Perfect => @"""
            Play optimally. Calculate all winning combinations, anticipate opponent's best moves, create forcing sequences, and never miss tactical opportunities. Execute flawless strategy.

            RANDOMIZATION: Never randomize. Always play your best calculated move and set 'random' to false.
            """,

            _ => throw new ArgumentOutOfRangeException(nameof(Options.Difficulty))
        };

        return $$"""
        You are playing Numeric Tic-Tac-Toe.

        RULES:
        - {{gameState.TokensPerRow}}x{{gameState.TokensPerRow}} board (positions 0-{{boardSize - 1}}, row-major order)
        - Board format: {{boardSize}} pipe-separated values, where {{GameState.EmptyBoardSpaceValue}} = empty
        - You may place tokens only on an empty position, represented by {{GameState.EmptyBoardSpaceValue}}.
        - You may only use each token once per game.
        - Win condition: Three numbers in a line (row, column, or diagonal) sum to exactly {{winningTotal}}

        STRATEGY LEVEL: {{options.Difficulty}}
        {{strategyInstructions}}

        RESPONSE FORMAT (JSON only):
        {
          "position": <0-{boardSize - 1}>,
          "token": <your chosen number>,
          "random": true | false
        }

        IMPORTANT: Respond ONLY with valid JSON. No additional text.
        """;
    }

    /// <summary>
    ///   Generates the JSON schema to be used as the instructions for the model's response format.
    /// </summary>
    ///
    /// <param name="gameState">The initial state that the current game is based on.</param>
    ///
    /// <returns>The response JSON schema.</returns>
    ///
    private static BinaryData GenerateJsonResponseSchema(GameState gameState)
    {
        var allTokens = gameState.GetPlayerTokens(PlayerToken.Even)
            .Concat(gameState.GetPlayerTokens(PlayerToken.Odd));

        return BinaryData.FromString(
        $$"""
        {
          "$schema": "http://json-schema.org/draft-04/schema#",
          "type": "object",
          "properties": {
            "position": {
              "type": "integer",
              "minimum": 0,
              "maximum": {{(gameState.TokensPerRow * gameState.TokensPerRow) - 1}}
            },
            "token": {
              "type": "integer",
              "minimum": {{allTokens.Min()}},
              "maximum": {{allTokens.Max()}}
            },
            "random": {
              "type": "boolean"
            }
          },
          "additionalProperties": false,
          "required": [
            "position",
            "token",
            "random"
          ]
        }
        """);
     }

    /// <summary>
    ///   Represents the structured response from the OpenAI API.
    /// </summary>
    ///
    /// <param name="Position">The board position of the move.</param>
    /// <param name="Token">The token to place for the move.</param>
    /// <param name="Random"><c>true</c> if the model made the move randomly; otherwise, <c>false</c>.</param>
    ///
    internal record MoveResponse(
        int Position,
        byte Token,
        bool Random);

    /// <summary>
    ///   Represents the result of parsing structured response from the OpenAI API.
    /// </summary>
    ///
    /// <param name="IsValid"><c>true</c> if the response was valid; otherwise, <c>false</c>.</param>
    /// <param name="Move">The parsed move, if valid; otherwise, <c>null</c>.</param>
    /// <param name="IsRandom"><c>true</c>< if the model made the move randomly; otherwise, <c>false</c>.</param>
    /// <param name="ParseError">The error message that resulted from parsing an invalid response.</param>
    ///
    private record ParseResult(
        bool IsValid,
        Move? Move,
        bool IsRandom,
        string? ErrorMessage)
    {
        /// <summary>
        ///   Creates a successful parse result.
        /// </summary>
        ///
        /// <param name="move">The validated move made by the model.</param>
        /// <param name="wasRandom"><c>true</c>< if the model made the move randomly; otherwise, <c>false</c>.</param>
        ///
        /// <returns>A <see cref="ParseResult" /> indicating success.</returns>
        ///
        public static ParseResult Success(Move move,
                                          bool isRandom) =>
            new (IsValid: true, Move: move, IsRandom: isRandom, ErrorMessage: null);

        /// <summary>
        ///   Creates a failed parse result.
        /// </summary>
        ///
        /// <param name="errorMessage">The error message describing the validation failure.</param>
        ///
        /// <returns>A <see cref="ParseResult" /> indicating failure.</returns>
        ///
        public static ParseResult Failure(string errorMessage) =>
            new (IsValid: false, Move: null, IsRandom: false, ErrorMessage: errorMessage);
    }
}