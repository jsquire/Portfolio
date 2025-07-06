using Squire.NumTic.Game;
using Squire.NumTic.Game.Contracts;

namespace Squire.NumTic.Console;

/// <summary>
///   A console-based player implementation that prompts the user for input
///   through a TextReader and provides output through a TextWriter.
/// </summary>
///
public class ConsolePlayer : IPlayer
{
    /// <summary>The game interface to interact with for player operations.</summary>
    private readonly IGameInterface _interface;

    /// <summary>
    ///   Initializes a new instance of the <see cref="ConsolePlayer"/> class.
    /// </summary>
    ///
    /// <param name="gameInterface">The game interface to interact with for player operations.</param>
    ///
    public ConsolePlayer(IGameInterface gameInterface) =>
        _interface = gameInterface ?? throw new ArgumentNullException(nameof(gameInterface));

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

        var currentPlayerName = gameState.CurrentTurn == PlayerToken.Odd ? "Odd Player" : "Even Player";
        var availableTokens = gameState.CurrentPlayerTokens;

        // Display available tokens.

        await _interface.RenderPlayerTextAsync(TextType.Message, $"Available tokens: {string.Join(", ", availableTokens.OrderBy(t => t))}{Environment.NewLine}", cancellationToken);

        // Get token selection.

        byte selectedToken;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await _interface.RenderPlayerTextAsync(TextType.Prompt,"Select a token to place: ", cancellationToken);
            var tokenInput = await _interface.ReadPlayerResponseAsnyc(cancellationToken);

            if (!cancellationToken.IsCancellationRequested)
            {
                if (string.IsNullOrWhiteSpace(tokenInput))
                {
                    await _interface.RenderPlayerTextAsync(TextType.Error, "Please enter a valid token number.", cancellationToken);
                    continue;
                }

                if (!byte.TryParse(tokenInput.Trim(), out selectedToken))
                {
                    await _interface.RenderPlayerTextAsync(TextType.Error, "Please enter a valid number.", cancellationToken);
                    continue;
                }

                if (!availableTokens.Contains(selectedToken))
                {
                    await _interface.RenderPlayerTextAsync(TextType.Error, $"Token {selectedToken} is not available. Please select from: {string.Join(", ", availableTokens.OrderBy(t => t))}", cancellationToken);
                    continue;
                }

                break;
            }
        }

        // Get row selection.

        int selectedRow;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await _interface.RenderPlayerTextAsync(TextType.Prompt, $"{Environment.NewLine}Select a row (1-{gameState.TokensPerRow}): ", cancellationToken);
            var rowInput = await _interface.ReadPlayerResponseAsnyc(cancellationToken);

            if (!cancellationToken.IsCancellationRequested)
            {
                if (string.IsNullOrWhiteSpace(rowInput))
                {
                    await _interface.RenderPlayerTextAsync(TextType.Error, "Please enter a valid row number.", cancellationToken);
                    continue;
                }

                if (!int.TryParse(rowInput.Trim(), out selectedRow))
                {
                    await _interface.RenderPlayerTextAsync(TextType.Error, "Please enter a valid number.", cancellationToken);
                    continue;
                }

                if ((selectedRow < 1) || (selectedRow > gameState.TokensPerRow))
                {
                    await _interface.RenderPlayerTextAsync(TextType.Error, $"Row must be between 1 and {gameState.TokensPerRow}.", cancellationToken);
                    continue;
                }

                break;
            }
        }

        // Get column selection.

        int selectedColumn;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await _interface.RenderPlayerTextAsync(TextType.Prompt, $"Select a column (1-{gameState.TokensPerRow}): ", cancellationToken);
            var columnInput = await _interface.ReadPlayerResponseAsnyc(cancellationToken);

            if (!cancellationToken.IsCancellationRequested)
            {
                if (string.IsNullOrWhiteSpace(columnInput))
                {
                    await _interface.RenderPlayerTextAsync(TextType.Error, "Please enter a valid column number.", cancellationToken);
                    continue;
                }

                if (!int.TryParse(columnInput.Trim(), out selectedColumn))
                {
                    await _interface.RenderPlayerTextAsync(TextType.Error, "Please enter a valid number.", cancellationToken);
                    continue;
                }

                if ((selectedColumn < 1) || (selectedColumn > gameState.TokensPerRow))
                {
                    await _interface.RenderPlayerTextAsync(TextType.Error, $"Column must be between 1 and {gameState.TokensPerRow}.", cancellationToken);
                    continue;
                }

                break;
            }
        }

        // Check if the selected position is occupied.

        if (!gameState.IsEmptyPosition(selectedRow, selectedColumn))
        {
            await _interface.RenderPlayerTextAsync(TextType.Error, $"Position at row {selectedRow}, column {selectedColumn} is already occupied. Please try again.{Environment.NewLine}", cancellationToken);

            // Restart the position selection process.

            return await PlayTurnAsync(gameState, cancellationToken);
        }

        await _interface.RenderPlayerTextAsync(TextType.Message, $"{Environment.NewLine}Placing token {selectedToken} at row {selectedRow}, column {selectedColumn}...{Environment.NewLine}", cancellationToken);
        return new Move(gameState.CurrentTurn, gameState.GetBoardPositionIndex(selectedRow, selectedColumn), selectedToken, null);
    }
}
