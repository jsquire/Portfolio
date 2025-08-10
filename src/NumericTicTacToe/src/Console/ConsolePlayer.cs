using System.Text;
using Squire.NumTic.Contracts;

namespace Squire.NumTic.Console;

/// <summary>
///   A console-based player implementation that prompts the user moves and
///   updates the game state accordingly.
/// </summary>
///
public class ConsolePlayer : IPlayer
{
    /// <summary>The game interface to interact with for player operations.</summary>
    private readonly IGameInterface Interface;

    /// <summary>
    ///   Initializes a new instance of the <see cref="ConsolePlayer"/> class.
    /// </summary>
    ///
    /// <param name="gameInterface">The game interface to interact with for player operations.</param>
    ///
    public ConsolePlayer(IGameInterface gameInterface) =>
        Interface = gameInterface ?? throw new ArgumentNullException(nameof(gameInterface));

    /// <summary>
    ///   Plays a turn in the game by prompting the user for their move selection.
    /// </summary>
    ///
    /// <param name="gameState">The current state of the game.</param>
    /// <param name="cancellationToken">A token that can be used to signal a request for cancellation.</param>
    ///
    /// <returns>The move that was made by the user.</returns>
    ///
    /// <exception cref="ArgumentNullException">Thrown when gameState is null.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    ///
    public async Task<Move> PlayTurnAsync(GameState gameState,
                                          CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(gameState, nameof(gameState));
        cancellationToken.ThrowIfCancellationRequested();

        // Get token selection.

        byte selectedToken;

        while (true)
        {
            await Interface.RenderPlayerTextAsync(TextType.Prompt, "Select a token to place:", cancellationToken);
            var tokenInput = await Interface.ReadPlayerResponseAsnyc(cancellationToken);

            if (tokenInput is { Length: 0 })
            {
                await Interface.RenderPlayerTextAsync(TextType.Error, "Please enter a valid token number.", cancellationToken);
                continue;
            }

            if (!byte.TryParse(tokenInput, out selectedToken))
            {
                await Interface.RenderPlayerTextAsync(TextType.Error, "Please enter a valid number.", cancellationToken);
                continue;
            }

            if (!gameState.CurrentPlayerTokens.Contains(selectedToken))
            {
                await Interface.RenderPlayerTextAsync(TextType.Error, $"Token {selectedToken} is not available. Please select from: {{ {FormatPlayerTokens(gameState.CurrentPlayerTokens)} }}", cancellationToken);
                continue;
            }

            break;
        }

        // Get position selection (1-9 for standard 3x3 board).

        int selectedPosition;

        var maxPosition = gameState.Board.Length;
        var positionPrompt = $"Select a position (1-{gameState.Board.Length}): ";
        var invalidPositionMessage = $"Position must be between 1 and {maxPosition}.";

        while (true)
        {
            await Interface.RenderPlayerTextAsync(TextType.Prompt, positionPrompt, cancellationToken);

            var positionInput = await Interface.ReadPlayerResponseAsnyc(cancellationToken);

            if (string.IsNullOrWhiteSpace(positionInput))
            {
                await Interface.RenderPlayerTextAsync(TextType.Error, "Please enter a valid position number.", cancellationToken);
                continue;
            }

            if (!int.TryParse(positionInput, out selectedPosition))
            {
                await Interface.RenderPlayerTextAsync(TextType.Error, "Please enter a valid number.", cancellationToken);
                continue;
            }

            if ((selectedPosition < 1) || (selectedPosition > maxPosition))
            {
                await Interface.RenderPlayerTextAsync(TextType.Error, invalidPositionMessage, cancellationToken);
                continue;
            }

            // Convert to 0-based board index.

            var boardIndex = selectedPosition - 1;

            // Check if the selected position is occupied.

            if (gameState.Board[boardIndex] != GameState.EmptyBoardSpaceValue)
            {
                await Interface.RenderPlayerTextAsync(TextType.Error, $"Position {selectedPosition} is already occupied. Please try again.", cancellationToken);
                continue;
            }

            break;
        }

        // Convert position to board index (1-based to 0-based).

        return new Move(gameState.CurrentTurn, (selectedPosition - 1), selectedToken);
    }

    /// <summary>
    ///   Formats the set of player tokens for display.
    /// </summary>
    ///
    /// <param name="tokens">The set of player tokens to consider.</param>
    ///
    /// <returns>The set of tokens, formatted for display.</returns>
    ///
    private static string FormatPlayerTokens(HashSet<byte> tokens)
    {
        if (tokens.Count == 0)
        {
            return "None";
        }

        // We know the set of available tokens will be a reasonable size, so
        // sort using a stack allocated array to avoid the allocation needed for
        // IOrderedEnumerable<T> when doing a direct `Order` sort on the hash set.

        var sortedTokens = (Span<byte>)stackalloc byte[tokens.Count];
        var index = 0;

        foreach (var token in tokens)
        {
            sortedTokens[index++] = token;
        }

        sortedTokens.Sort();

        // Pre-calculate capacity to avoid StringBuilder reallocations.  Account for
        // the number of tokens, commas, spaces, and surrounding braces.

        var capacity = 4 + (sortedTokens.Length * 3);
        var builder = new StringBuilder(capacity);

        builder.Append("{ ");

        for (index = 0; index < sortedTokens.Length; ++index)
        {
            if (index > 0)
            {
                builder.Append(", ");
            }

            builder.Append(sortedTokens[index]);
        }

        builder.Append(" }");

        return builder.ToString();
    }
}