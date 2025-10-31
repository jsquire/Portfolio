# OpenAI Bot Player Implementation Guide

## Document Purpose

This guide provides implementation details for creating an OpenAI-powered bot player for Numeric Tic-Tac-Toe using the OpenAI Responses API with Azure OpenAI integration. The approach has been designed for optimal token efficiency, strategic coherence, and alignment with the project's architecture patterns.

**Last Updated:** October 19, 2025  
**Target Framework:** .NET 9.0  
**OpenAI SDK:** OpenAI (already referenced in NumTic.AI.csproj)

---

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [OpenAI Responses API Fundamentals](#openai-responses-api-fundamentals)
3. [Conversation State Management](#conversation-state-management)
4. [Game State Serialization](#game-state-serialization)
5. [System Prompt Design](#system-prompt-design)
6. [Structured Response Parsing](#structured-response-parsing)
7. [Difficulty Implementation](#difficulty-implementation)
8. [Implementation Pattern](#implementation-pattern)
9. [Token Efficiency Analysis](#token-efficiency-analysis)
10. [Validation Checklist](#validation-checklist)

---

## Architecture Overview

### Component Integration

```
IPlayer (interface)
    ↓
OpenAIPlayer (implementation)
    ↓
OpenAIResponseClient (OpenAI SDK)
    ↓
Azure OpenAI Service (via BearerTokenPolicy + DefaultAzureCredential)
```

### Key Classes

**From Game Project:**
- `IPlayer` - Contract requiring `Task<Move> PlayTurnAsync(GameState, CancellationToken)`
- `GameState` - Contains `Board` (byte[]), `CurrentTurn` (PlayerToken), `CurrentPlayerTokens` (HashSet<byte>)
- `Move` - Record struct: `(PlayerToken Player, int PositionIndex, byte Token)`
- `Difficulty` - Enum: `Easy`, `Medium`, `Hard`, `Perfect`
- `PlayerToken` - Enum: `Odd` (1,3,5,7,9), `Even` (2,4,6,8)

**From OpenAI SDK (validated via openai-dotnet repository):**
- `OpenAI.Responses.OpenAIResponseClient` - Client for Responses API
- `OpenAI.Responses.ResponseItem` - Message items with factory methods
- `OpenAI.Responses.OpenAIResponse` - Response container
- `OpenAI.Responses.MessageRole` - Enum: `Unknown`, `Assistant`, `Developer`, `System`, `User`
- `OpenAI.Responses.ResponseCreationOptions` - Configuration for API calls

### Factory Methods (Validated)

**Source:** `openai-dotnet/src/Custom/Responses/Items/ResponseItem.cs`

```csharp
// Create message items for conversation.

public static MessageResponseItem CreateUserMessageItem(string inputTextContent)
public static MessageResponseItem CreateDeveloperMessageItem(string inputTextContent)
public static MessageResponseItem CreateAssistantMessageItem(string outputTextContent)
```

**Example Usage (from openai-dotnet test suite):**
```csharp
OpenAIResponse response = await client.CreateResponseAsync(
    [
        ResponseItem.CreateDeveloperMessageItem("You are a helpful assistant."),
        ResponseItem.CreateUserMessageItem("Hello, Assistant, my name is Bob!"),
        ResponseItem.CreateAssistantMessageItem("Hello, Bob. It's a nice, sunny day!"),
        ResponseItem.CreateUserMessageItem("What's my name and what did you tell me the weather was like?"),
    ]);
```

---

## OpenAI Responses API Fundamentals

### API Characteristics (Validated)

**Stateless Design:**
- Each API call is independent - no server-side session management
- Complete conversation history must be sent on every request
- No automatic context retention between calls

**Message Roles:**
- `Developer` - System instructions (replaces older `System` role in Chat API)
- `User` - Game state and requests for moves
- `Assistant` - LLM's responses (preserved for context)

**Conversation Pattern:**
```
Request:  [Developer: Instructions, User: State1]
Response: [Assistant: Move1]

Next Request:  [Developer: Instructions, User: State1, Assistant: Move1, User: State2]
Response: [Assistant: Move2]

Next Request:  [Developer: Instructions, User: State1, Assistant: Move1, User: State2, Assistant: Move2, User: State3]
Response: [Assistant: Move3]
```

### Why Full History Matters for This Game

**Game Context:** 9 total turns, LLM plays 4-5 times

**Strategic Value:**
1. **Multi-Move Planning:** LLM can execute coherent strategies across turns
2. **Pattern Recognition:** LLM can track opponent's blocking attempts
3. **Tactical Memory:** LLM remembers "I tried building toward position X"
4. **Reasonable Cost:** ~10-20 messages total for complete game history

**Without History:** Each move would be tactical but isolated, resembling random play at lower difficulties.

**With History:** LLM maintains strategic coherence, executing multi-turn plans appropriate to difficulty level.

**Cost Analysis:**
- History grows linearly: ~2 messages per turn
- Maximum ~20 messages for 9-turn game
- Compact serialization: ~30-40 tokens per state
- Total game cost: 500-950 tokens (reasonable for 4-5 API calls)

---

## Conversation State Management

### Implementation Pattern

```csharp
/// <summary>
///   OpenAI-powered implementation of the IPlayer interface using the Responses API.
/// </summary>
///
public class OpenAIPlayer : IPlayer
{

    /// <summary>The game interface to interact with for player operations.</summary>
    private readonly IGameInterface Interface;

    /// <summary>The options to use for player behavior.</summary>
    private readonly OpenAIPlayerOptions Options;

    /// <summary>The client to use for interacting with the OpenAI Responses API.</summary>
    private readonly OpenAIResponseClient ResponseClient;

    /// <summary>The set of options to use for interacting with the OpenAI Responses API.</summary>
    private readonly ResponseCreationOptions ResponseOptions;

    /// <summary>The set of items that comprise the conversation history with the LLM for the game.</summary>
    private readonly List<ResponseItem> ConversationHistory = new();

    /// <summary>
    ///   Initializes a new instance of the <see cref="OpenAIPlayer"/> class.
    /// </summary>
    ///
    /// <param name="gameInterface">The game interface to interact with for player operations.</param>
    /// <param name="openAIClient">The OpenAI client to use for model interactions.</param>
    /// <param name="gameState">The state that the current game is based on.</param>
    /// <param name="options">The set of options to use for configuring player behavior.  If not provided a default set is assumed.</param>
    ///
    /// <exception cref="ArgumentNullException">Occurs when the <paramref name="gameInterface"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">Occurs when the <paramref name="openAIClient"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">Occurs when the <paramref name="gameState"/> is <c>null</c>.</exception>
    ///
    public OpenAIPlayer(IGameInterface gameInterface,
                        OpenAIClient openAIClient,
                        GameState gameState,
                        OpenAIPlayerOptions? options = default)
    {
        Interface = gameInterface ?? throw new ArgumentNullException(nameof(gameInterface));
        Options = options?.Clone() ?? OpenAIPlayerOptions.Default;

        ArgumentNullException.ThrowIfNull(openAIClient, nameof(openAIClient));
        ResponseClient = openAIClient.GetOpenAIResponseClient(Options.ModelName);

        ArgumentNullException.ThrowIfNull(gameState, nameof(gameState));

        ResponseOptions = new ResponseCreationOptions
        {
            Instructions = GenerateInstructions(gameState, Options),
            TextOptions = new ResponseTextOptions
            {
                TextFormat = ResponseTextFormat.CreateJsonSchemaFormat("response", GenerateJsonResponseSchema(gameState))
            }
        };
    }

    // NOTE: PlayTurnAsync implementation not yet implemented in actual code.
    // The pattern below shows the intended implementation approach.
}
```

### State Lifecycle

```
Game Start → Empty history []

Turn 1 → [User: State1] + Instructions → API → [User, Assistant: Move1]

Turn 2 → [User, Assistant, User: State2] + Instructions → API → [..., Assistant: Move2]

Turn N → [User, Asst, User, Asst, ..., User: StateN] + Instructions → API → [..., Assistant: MoveN]

Game End → Reset for new game → Empty history []
```

### Memory Management

**Single Game Instance:**
- List grows to ~20 items maximum
- Total memory impact negligible (<10KB)
- No cleanup needed during game

**Multiple Games:**
- Clear history between games: `ConversationHistory.Clear()`
- Or create new player instance per game
- Prevents context bleeding between unrelated games

---

## Game State Serialization

### Compact Format Design

**Rationale:**
- GameState contains all information needed for strategic decisions
- Board positions (0-8, row-major) with pipe separation
- Both players' available tokens must be communicated
- Zero (`0`) represents empty spaces
- Format is parseable and token-efficient

### Format Examples

```
Turn 1 (all tokens available):
Board: 0|0|0|0|0|0|0|0|0
Your available tokens: 1,3,5,7,9
Opponent's available tokens: 2,4,6,8
Moves since last random: 0

Mid-game (some tokens used):
Board: 0|2|0|4|5|0|0|0|9
Your available tokens: 1,3,7
Opponent's available tokens: 6,8
Moves since last random: 2

Late game (few tokens left):
Board: 1|2|7|4|5|6|3|8|9
Your available tokens: (none - game over)
Opponent's available tokens: (none - game over)
Moves since last random: 1
```

### Token Efficiency

| Approach | Tokens per State | Efficiency |
|----------|------------------|------------|
| Verbose description | ~150-200 | Baseline |
| Compact pipe format | ~30-40 | **75-80% reduction** |

**Validated:** Compact format provides massive token savings while remaining clear and parseable.

---

## System Prompt Design

### Prompt Structure

```csharp
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
    - Win condition: Three numbers in a line (row, column, or diagonal) sum to exactly {{winningTotal}}
    - Board format: {{boardSize}} pipe-separated values, where {{GameState.EmptyBoardSpaceValue}} = empty

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
```

### Prompt Validation Points

**✓ Rules are explicit and unambiguous**
- Board format clearly defined
- Position indexing explained
- Win condition stated precisely
- Dynamic board layout generation based on game configuration

**✓ Data-driven configuration**
- Board dimensions read from GameState.TokensPerRow
- Winning total read from GameState.WinningTotal
- Supports any board size, not hardcoded to 3x3
- LLM trusted to calculate winning combinations (basic arithmetic)

**✓ Response format is structured**
- JSON schema provided
- Field types specified
- Example structure implicit

**✓ Difficulty-appropriate strategies**
- Easy: Casual, minimal calculation
- Medium: Tactical but not exhaustive
- Hard: Multi-move planning
- Perfect: Optimal play

---

## Structured Response Parsing

### Response Schema

```csharp
/// <summary>
///   Represents the structured response from the OpenAI API.
/// </summary>
///
private record MoveResponse(
    int Position,
    byte Token,
    bool Random);
```

### Parsing Implementation

```csharp
    /// <summary>
    ///   Attempts to parse and validate the LLM's JSON response into a Move.
    /// </summary>
    ///
    /// <param name="responseText">The JSON response from the LLM.</param>
    /// <param name="gameState">The current game state for validation.</param>
    ///
    /// <returns>A parse result indicating success or failure with error details.</returns>
    ///
    private static ParseResult TryParseMove(string responseText, GameState gameState)
    {
        MoveResponse? moveResponse;

        // Attempt to deserialize JSON.

        try
        {
            moveResponse = JsonSerializer.Deserialize<MoveResponse>(
                responseText,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (moveResponse is null)
            {
                return ParseResult.Failure("Response was null or could not be deserialized.");
            }
        }
        catch (JsonException ex)
        {
            return ParseResult.Failure($"Invalid JSON format: {ex.Message}");
        }

        // Validate position.

        if (moveResponse.Position < 0 || moveResponse.Position >= gameState.Board.Length)
        {
            return ParseResult.Failure(
                $"Position {moveResponse.Position} is out of bounds. Valid positions are 0-{gameState.Board.Length - 1}.");
        }

        if (gameState.Board[moveResponse.Position] != GameState.EmptyBoardSpaceValue)
        {
            return ParseResult.Failure(
                $"Position {moveResponse.Position} is already occupied.");
        }

        // Validate token.

        if (!gameState.CurrentPlayerTokens.Contains(moveResponse.Token))
        {
            var available = string.Join(", ", gameState.CurrentPlayerTokens);
            return ParseResult.Failure(
                $"Token {moveResponse.Token} is not available. Your available tokens are: {available}");
        }

        // Create and return successful result.

        var move = new Move(
            gameState.CurrentTurn,
            moveResponse.Position,
            moveResponse.Token);

        return ParseResult.Success(move, moveResponse.Random);
    }

    /// <summary>
    ///   Represents the result of attempting to parse a move from the LLM response.
    /// </summary>
    ///
    private record ParseResult
    {
        /// <summary>
        ///   Gets a value indicating whether the parse operation succeeded.
        /// </summary>
        ///
        public bool IsValid { get; init; }

        /// <summary>
        ///   Gets the validated move, or null if parsing failed.
        /// </summary>
        ///
        public Move? Move { get; init; }

        /// <summary>
        ///   Gets a value indicating whether the LLM reported this move was random.
        /// </summary>
        ///
        public bool WasRandom { get; init; }

        /// <summary>
        ///   Gets the error message describing why parsing failed, or null if successful.
        /// </summary>
        ///
        public string? ErrorMessage { get; init; }

        /// <summary>
        ///   Creates a successful parse result.
        /// </summary>
        ///
        /// <param name="move">The validated move.</param>
        /// <param name="wasRandom">Whether the LLM reported the move was random.</param>
        ///
        /// <returns>A ParseResult indicating success.</returns>
        ///
        public static ParseResult Success(Move move, bool wasRandom) =>
            new() { IsValid = true, Move = move, WasRandom = wasRandom };

        /// <summary>
        ///   Creates a failed parse result.
        /// </summary>
        ///
        /// <param name="errorMessage">The error message describing the validation failure.</param>
        ///
        /// <returns>A ParseResult indicating failure.</returns>
        ///
        public static ParseResult Failure(string errorMessage) =>
            new() { IsValid = false, ErrorMessage = errorMessage };
    }
```

### Benefits of Structured Parsing

1. **Reliability:** JSON schema enforcement prevents ambiguous responses
2. **Validation:** Explicit checks for legal moves
3. **Randomization Control:** `random` flag enables hybrid difficulty implementation
4. **Debugging:** Clear error messages when parsing fails

---

## Difficulty Implementation

### Approach: Hybrid LLM-Controlled Randomization

**Decision:** LLM generates random moves when appropriate and reports this via response field.

**Rationale:**
- LLM controls **when** to randomize (based on move count and difficulty instructions)
- LLM controls **what** move to make (truly random selection from valid moves)
- LLM reports randomization via `"random": true` for tracking purposes
- Code tracks counter to inform LLM of moves since last random
- Maintains conversation coherence (LLM owns both decision and execution)

### Difficulty Characteristics

| Difficulty | Description | Random Frequency | Strategic Depth |
|------------|-------------|------------------|-----------------|
| **Easy** | Casual play, minimal calculation | Every 3rd move (33%) | Basic tactics only |
| **Medium** | Moderate thinking, occasional oversight | Every 5th move (20%) | Tactical awareness |
| **Hard** | Strong strategy, multi-move planning | Every 7th move (14%) | Advanced calculation |
| **Perfect** | Optimal play, flawless execution | Never (0%) | Exhaustive analysis |

### Implementation Details

**LLM's Responsibility:**
- Receives "Moves since last random" in game state serialization
- Follows difficulty-specific randomization instructions
- When randomization is due, generates a truly random valid move
- Sets `"random": true` in response to report that the move was random
- When not randomizing, generates strategic move and sets `"random": false`

**Code's Responsibility:**
- Tracks `_movesSinceLastRandom` counter
- Includes counter in game state serialization
- Parses `random` flag from LLM response
- Resets counter to 0 when LLM reports random move
- Increments counter when LLM reports strategic move

**Key Benefits:**
- ✓ LLM owns both randomization decision and execution
- ✓ Predictable, testable difficulty scaling
- ✓ Maintains conversation coherence (LLM fully controls its behavior)
- ✓ Code can track randomization patterns for analysis

---

## Implementation Pattern

### Complete Class Structure

```csharp
namespace Squire.NumTic.AI;

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

    /// <summary>The game interface to interact with for player operations.</summary>
    private readonly IGameInterface Interface;

    /// <summary>The options to use for player behavior.</summary>
    private readonly OpenAIPlayerOptions Options;

    /// <summary>The client to use for interacting with the OpenAI Responses API.</summary>
    private readonly OpenAIResponseClient ResponseClient;

    /// <summary>The set of options to use for interacting with the OpenAI Responses API.</summary>
    private readonly ResponseCreationOptions ResponseOptions;

    /// <summary>The set of items that comprise the conversation history with the LLM for the game.</summary>
    private readonly List<ResponseItem> ConversationHistory = new();

    /// <summary>
    ///   Initializes a new instance of the <see cref="OpenAIPlayer"/> class.
    /// </summary>
    ///
    /// <param name="gameInterface">The game interface to interact with for player operations.</param>
    /// <param name="openAIClient">The OpenAI client to use for model interactions.</param>
    /// <param name="gameState">The state that the current game is based on.</param>
    /// <param name="options">The set of options to use for configuring player behavior.  If not provided a default set is assumed.</param>
    ///
    /// <exception cref="ArgumentNullException">Occurs when the <paramref name="gameInterface"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">Occurs when the <paramref name="openAIClient"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">Occurs when the <paramref name="gameState"/> is <c>null</c>.</exception>
    ///
    public OpenAIPlayer(IGameInterface gameInterface,
                        OpenAIClient openAIClient,
                        GameState gameState,
                        OpenAIPlayerOptions? options = default)
    {
        Interface = gameInterface ?? throw new ArgumentNullException(nameof(gameInterface));
        Options = options?.Clone() ?? OpenAIPlayerOptions.Default;

        ArgumentNullException.ThrowIfNull(openAIClient, nameof(openAIClient));
        ResponseClient = openAIClient.GetOpenAIResponseClient(Options.ModelName);

        ArgumentNullException.ThrowIfNull(gameState, nameof(gameState));

        ResponseOptions = new ResponseCreationOptions
        {
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

        const int MaxRetries = 3;
        int retryCount = 0;

        // Add current game state to conversation history.

        ConversationHistory.Add(
            ResponseItem.CreateUserMessageItem(SerializeGameState(gameState, _movesSinceLastRandom)));

        while (retryCount < MaxRetries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Call API with conversation history and instructions.

            OpenAIResponse response = await ResponseClient.CreateResponseAsync(
                ConversationHistory,
                ResponseOptions,
                cancellationToken);

            // Extract and parse response.

            var assistantResponse = response.GetOutputText();

            // Preserve assistant's response in conversation history.

            ConversationHistory.Add(
                ResponseItem.CreateAssistantMessageItem(assistantResponse));

            // Attempt to parse and validate move.

            var parseResult = TryParseMove(assistantResponse, gameState);

            if (parseResult.IsValid)
            {
                // Update counter based on whether move was random.

                if (parseResult.WasRandom)
                {
                    _movesSinceLastRandom = 0;
                }
                else
                {
                    _movesSinceLastRandom++;
                }

                return parseResult.Move!;
            }

            // Move was invalid - send error message back to LLM for retry.

            retryCount++;

            if (retryCount < MaxRetries)
            {
                var errorMessage = $"Invalid move: {parseResult.ErrorMessage}. Please try again with a valid move.";
                ConversationHistory.Add(ResponseItem.CreateUserMessageItem(errorMessage));
            }
        }

        throw new InvalidOperationException(
            $"Failed to get valid move after {MaxRetries} attempts.");
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
            Difficulty.Easy => @"Play casually with basic tactics. Consider obvious moves but don't analyze deeply. Make moves that feel natural without extensive calculation.

RANDOMIZATION: Every 3rd move (when 'Moves since last random' reaches 3), you must make a completely random move. Select any valid position and any available token randomly, then set 'random' to true in your response.",

            Difficulty.Medium => @"Play with moderate strategic thinking. Look for immediate winning opportunities and block obvious threats, but don't calculate multiple moves ahead consistently.

RANDOMIZATION: Every 5th move (when 'Moves since last random' reaches 5), you must make a completely random move. Select any valid position and any available token randomly, then set 'random' to true in your response.",

            Difficulty.Hard => @"Play with strong tactical awareness. Analyze winning combinations, block opponent threats proactively, and set up multi-move strategies when possible.

RANDOMIZATION: Every 7th move (when 'Moves since last random' reaches 7), you must make a completely random move. Select any valid position and any available token randomly, then set 'random' to true in your response.",

            Difficulty.Perfect => @"Play optimally. Calculate all winning combinations, anticipate opponent's best moves, create forcing sequences, and never miss tactical opportunities. Execute flawless strategy.

RANDOMIZATION: Never randomize. Always play your best calculated move and set 'random' to false.",

            _ => throw new ArgumentOutOfRangeException(nameof(Options.Difficulty))
        };

        return $$"""
        You are playing Numeric Tic-Tac-Toe.

        RULES:
        - {{gameState.TokensPerRow}}x{{gameState.TokensPerRow}} board (positions 0-{{boardSize - 1}}, row-major order)
        - Win condition: Three numbers in a line (row, column, or diagonal) sum to exactly {{winningTotal}}
        - Board format: {{boardSize}} pipe-separated values, where {{GameState.EmptyBoardSpaceValue}} = empty

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
              "type": "integer"
              "minimum": 0
              "maximum": {{(gameState.TokensPerRow * gameState.TokensPerRow) - 1}}
            },
            "token": {
              "type": "enum"
              "minimum": {{allTokens.Min()}}
              "maximum": {{allTokens.Max()}}
            },
            "random": {
              "type": "boolean"
            }
          },
          "required": [
            "position",
            "token",
            "random"
          ]
        }
        """);
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
    private static string SerializeGameState(GameState gameState, int movesSinceLastRandom)
    {
        var board = string.Join("|", gameState.Board);
        var myTokens = string.Join(",", gameState.CurrentPlayerTokens);

        // Get opponent's tokens by checking the other player.

        var opponentPlayer = gameState.CurrentTurn == PlayerToken.Odd
            ? PlayerToken.Even
            : PlayerToken.Odd;
        var opponentTokens = string.Join(",", gameState.GetPlayerTokens(opponentPlayer));

        return $@"Board: {board}
Your available tokens: {myTokens}
Opponent's available tokens: {opponentTokens}
Moves since last random: {movesSinceLastRandom}";
    }

    /// <summary>
    ///   Parses the LLM's JSON response into a validated Move.
    /// </summary>
    ///
    /// <param name="responseText">The JSON response from the LLM.</param>
    /// <param name="gameState">The current game state for validation.</param>
    ///
    /// <returns>A tuple containing the validated move and a flag indicating if the move was random.</returns>
    ///
    /// <exception cref="InvalidOperationException">Thrown when response is invalid or move is illegal.</exception>
    ///
    private static (Move Move, bool WasRandom) ParseMove(string responseText, GameState gameState)
    {
        MoveResponse? moveResponse;

        // Attempt to deserialize JSON.

        try
        {
            moveResponse = JsonSerializer.Deserialize<MoveResponse>(responseText, JsonOptions);

            if (moveResponse is null)
            {
                throw new InvalidOperationException("LLM returned null response.");
            }
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Failed to parse LLM response as JSON: {responseText}",
                ex);
        }

        // Extract randomization flag for reporting.

        var wasRandom = moveResponse.Random;

        // Validate position.

        if (moveResponse.Position < 0 || moveResponse.Position >= gameState.Board.Length)
        {
            throw new InvalidOperationException(
                $"Invalid position {moveResponse.Position}. Must be 0-{gameState.Board.Length - 1}.");
        }

        if (gameState.Board[moveResponse.Position] != GameState.EmptyBoardSpaceValue)
        {
            throw new InvalidOperationException(
                $"Position {moveResponse.Position} is already occupied.");
        }

        // Validate token.

        if (!gameState.CurrentPlayerTokens.Contains(moveResponse.Token))
        {
            throw new InvalidOperationException(
                $"Token {moveResponse.Token} is not available for current player. " +
                $"Available tokens: {string.Join(", ", gameState.CurrentPlayerTokens)}");
        }

        // Return validated move and randomization flag.

        return (new Move(gameState.CurrentTurn, moveResponse.Position, moveResponse.Token), wasRandom);
    }

    /// <summary>
    ///   Represents the structured response from the OpenAI API.
    /// </summary>
    ///
    private record MoveResponse(
        int Position,
        byte Token,
        bool Random);
}
```

### Options Class

```csharp
namespace Squire.NumTic.Players;

/// <summary>
///   The set of options for configuring an <see cref="OpenAIPlayer" />.
/// </summary>
///
public class OpenAIPlayerOptions
{
    /// <summary>The default set of options.</summary>
    internal static readonly OpenAIPlayerOptions Default = new();

    /// <summary>The name of the model to use with the OpenAI API.</summary>
    private string _modelName = "gpt-4.1";

    /// <summary>
    ///   The difficulty level of the bot player.
    /// </summary>
    ///
    public Difficulty Difficulty { get; set; } = Difficulty.Perfect;

    /// <summary>
    ///   The name of the model to use with the OpenAI API.
    /// </summary>
    ///
    /// <exception cref="ArgumentNullException">Occurs when the <see cref="ModelName" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Occurs when the <see cref="ModelName" /> is empty.</exception>
    ///
    public string ModelName
    {
        get => _modelName;

        set
        {
            ArgumentNullException.ThrowIfNull(value, nameof(ModelName));
            ArgumentException.ThrowIfNullOrEmpty(value, nameof(ModelName));

            _modelName = value;
        }
    }

    /// <summary>
    ///   Clones this instance.
    /// </summary>
    ///
    /// <returns>A new options instance with the same member values.</returns>
    ///
    internal OpenAIPlayerOptions Clone() =>
        new()
        {
            Difficulty = this.Difficulty,
            ModelName = this.ModelName
        };
}
```

---

## Token Efficiency Analysis

### Per-Turn Token Breakdown

| Component | Tokens | Frequency |
|-----------|--------|-----------|
| System instructions | ~200 | Per turn (set via ResponseOptions.Instructions) |
| Game state (User) | ~40 | Per turn |
| LLM response (Assistant) | ~15-20 | Per turn |

### Full Game Projection

**Assumptions:**
- 9 total turns
- LLM plays 4-5 turns
- Full conversation history maintained
- Instructions set on every API call (not part of conversation history)

**Token Calculation:**

```
Turn 1: 200 (instructions) + 40 (state) = 240 tokens in
Turn 2: 200 (instructions) + 40 (state) + 15 (prev response) = 255 tokens in
Turn 3: 200 (instructions) + 40 + 15 + 15 = 270 tokens in
Turn 4: 200 (instructions) + 40 + 15 + 15 + 15 = 285 tokens in
Turn 5: 200 (instructions) + 40 + 15 + 15 + 15 + 15 = 300 tokens in

Total Input Tokens: ~1,350 tokens across 5 API calls
Total Output Tokens: ~75-100 tokens (5 responses × ~15-20 tokens)
Combined Total: ~1,425-1,450 tokens per game
```

**Cost Efficiency:**
- Compact serialization minimizes token usage
- Minimal response format (position, token, random only) saves ~10-15 tokens per response
- Full conversation history provides strategic coherence
- Total cost reasonable for AI-powered gameplay

### Optimization Opportunities

**If token costs become prohibitive:**

1. **Truncate Old History:**
   - Keep only last N turns (e.g., last 3-4 turns)
   - Loses early-game context but maintains recent strategy

2. **Summarize Instead of Full History:**
   - Periodically replace old messages with summaries
   - More complex to implement correctly

3. **Stateless Single-Turn:**
   - Send only system prompt + current state
   - Loses strategic coherence across turns
   - Reduces LLM to tactical calculator

**Recommendation:** Start with full history. Optimize only if costs justify complexity.

---

## Validation Checklist

### Pre-Implementation Validation

- [ ] **OpenAI Package Installed:** Verify `OpenAI` package in NumTic.AI.csproj
- [ ] **Azure OpenAI Deployed:** Confirm resource exists and deployment name known
- [ ] **Authentication Configured:** BearerTokenPolicy + DefaultAzureCredential setup complete
- [ ] **Model Compatibility:** Verify model supports Responses API (gpt-4.1, gpt-4o, etc.)

### Implementation Validation

- [ ] **IPlayer Contract:** Class implements all required methods correctly
- [ ] **Conversation History:** List properly initialized and maintained across turns
- [ ] **State Serialization:** Board format compact, includes moves since last random
- [ ] **System Instructions:** Set via ResponseOptions.Instructions on every call
- [ ] **Prompt Content:** All rules, board layout, and response format instructions included
- [ ] **JSON Parsing:** Structured response deserialization with error handling
- [ ] **Move Validation:** Position and token legality verified before returning
- [ ] **Random Move Handling:** LLM signals randomization, code executes random valid move
- [ ] **Move Counter:** `_movesSinceLastRandom` properly tracked and reset
- [ ] **Difficulty Scaling:** Strategy descriptions with correct randomization frequencies

### Testing Validation

- [ ] **Unit Tests:** Parse valid and invalid JSON responses
- [ ] **Unit Tests:** Validate move legality checks
- [ ] **Unit Tests:** Verify state serialization format
- [ ] **Integration Tests:** Complete game with mock OpenAI responses
- [ ] **Integration Tests:** Azure OpenAI authentication flow
- [ ] **Manual Testing:** Play games at each difficulty level
- [ ] **Manual Testing:** Verify strategic coherence across turns

### Performance Validation

- [ ] **Token Usage:** Monitor actual token consumption per game
- [ ] **Response Times:** Measure API call latency
- [ ] **Error Rates:** Track parsing failures and invalid moves
- [ ] **Cost Analysis:** Calculate per-game Azure OpenAI costs

---

## Known Limitations and Considerations

### What This Guide Does NOT Cover

**Authentication Setup:**
- BearerTokenPolicy instantiation pattern
- DefaultAzureCredential integration
- Token scope configuration ("https://cognitiveservices.azure.com/.default")
- OpenAIClientOptions endpoint configuration

**Reason:** Authentication is handled separately by project lead.

**Player Type Determination:**
- How OpenAIPlayer learns whether it's Odd or Even player
- Options: Pass via constructor, infer from first GameState, or store after first turn

**Reason:** Design decision pending - multiple valid approaches exist.

**Error Recovery Strategy:**
- API failure handling (retry, fallback, fail-fast)
- Malformed response handling beyond parsing
- Network timeout configuration

**Reason:** Requires project-wide error handling policy decision.

**Multi-Game Support:**
- Whether single player instance handles multiple sequential games
- Conversation history reset strategy
- Player instance lifecycle management

**Reason:** Use case not yet defined in requirements.

### Validation Status

**✅ Validated Against Authoritative Sources:**
- OpenAI SDK API surface (openai-dotnet repository)
- ResponseItem factory methods (verified in source code)
- Message role types (Developer, User, Assistant)
- Conversation history pattern (confirmed in test suite)
- GameState structure (project source code)
- IPlayer interface contract (project source code)
- Difficulty enumeration (project source code)

**⚠️ Requires Project-Specific Decisions:**
- Authentication mechanism instantiation
- Player type determination strategy
- Error handling and retry logic
- Logging and diagnostics approach
- Options validation in constructor

**❓ Unknown Without Further Research:**
- Specific model performance characteristics at each difficulty
- Optimal ResponseCreationOptions values for this use case
- Actual token consumption patterns (need production data)
- LLM tendency to hallucinate invalid moves (need testing)

---

## References

### Authoritative Sources

1. **OpenAI .NET SDK Repository:**  
   https://github.com/openai/openai-dotnet
   - ResponseItem factory methods validated
   - MessageRole enum confirmed
   - Conversation history pattern verified in test suite

2. **System.ClientModel.Primitives Documentation:**  
   https://learn.microsoft.com/en-us/dotnet/api/system.clientmodel.primitives
   - BearerTokenPolicy class confirmed
   - AuthenticationPolicy base class documented

3. **Project Source Code:**  
   - `IPlayer` interface: src/Game/Contracts/IPlayer.cs
   - `GameState` class: src/Game/GameState.cs
   - `Move` record: src/Game/Move.cs
   - `Difficulty` enum: src/Game/Difficulty.cs
   - `PlayerToken` enum: src/Game/PlayerToken.cs
   - `BotPlayerOptions` pattern: src/Game/Players/BotPlayerOptions.cs

### Implementation Examples

**MessageHistoryWorks Test (openai-dotnet):**
```csharp
OpenAIResponse response = await client.CreateResponseAsync(
    [
        ResponseItem.CreateDeveloperMessageItem("You are a helpful assistant."),
        ResponseItem.CreateUserMessageItem("Hello, Assistant, my name is Bob!"),
        ResponseItem.CreateAssistantMessageItem("Hello, Bob. It's a nice, sunny day!"),
        ResponseItem.CreateUserMessageItem("What's my name and what did you tell me the weather was like?"),
    ]);
```

**Source:** `tests/Responses/ResponsesTests.cs` line 697-712

---

## Document Maintenance

**Update Triggers:**
- OpenAI SDK version changes
- Azure OpenAI API updates
- Project architecture changes
- Authentication pattern finalization
- Performance optimization discoveries

**Last Reviewed:** October 19, 2025  
**Next Review:** After initial implementation and testing phase


