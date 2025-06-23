using Squire.NumTic.Game;
using Squire.NumTic.Game.Contracts;

namespace Squire.NumTic.Console;

/// <summary>
///   A console-based game interface for the numeric tic-tac-toe game that displays
///   the game state in a formatted grid and handles player interactions through console I/O.
/// </summary>
///
public class ConsoleGameInterface : IGameInterface
{
    /// <summary>
    ///   Renders the current state of the game to the console, clearing the screen first
    ///   and displaying the board, player information, and game status.
    /// </summary>
    ///
    /// <param name="gameState">The current state of the game to render.</param>
    /// <param name="cancellationToken">A token that can be used to signal a request for cancellation.</param>
    ///
    public Task RenderAsync(GameState gameState,
                           CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(gameState, nameof(gameState));

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        // Clear the console screen safely, handling test environments.

        try
        {
            System.Console.Clear();
        }
        catch (IOException)
        {
            // Expected in test environments where console clearing may not be supported.
        }


        // Display the title.

        System.Console.WriteLine("===================================");
        System.Console.WriteLine("        NUMERIC TIC-TAC-TOE        ");
        System.Console.WriteLine("===================================");
        System.Console.WriteLine();

        // Display the game board.

        RenderBoard(gameState);
        System.Console.WriteLine();

        // Display player information.

        RenderPlayerInfo(gameState);
        System.Console.WriteLine();

        // Display game status.

        RenderGameStatus(gameState);

        return Task.CompletedTask;
    }

    /// <summary>
    ///   Renders text associated with the player, such as messages, prompts, or errors.
    /// </summary>
    ///
    /// <param name="type">The type of text to render.</param>
    /// <param name="text">The text to render.</param>
    /// <param name="cancellationToken">A token that can be used to signal a request for cancellation.</param>
    ///
    public Task RenderPlayerTextAsync(TextType type,
                                      string text,
                                      CancellationToken cancellationToken = default)
    {
        var originalColor = System.Console.ForegroundColor;

        try
        {
            if (TextType.Error == type)
            {
                System.Console.ForegroundColor = ConsoleColor.Red;
                System.Console.WriteLine(text);
            }
            else if (TextType.Prompt == type)
            {
                System.Console.Write(text);
            }
            else
            {
                System.Console.WriteLine(text);
            }

            return Task.CompletedTask;

        }
        finally
        {
            System.Console.ForegroundColor = originalColor;
        }
    }

    /// <summary>
    ///   Reads a response from the player asynchronously, allowing them to
    ///   provide input or make selections during the game.
    /// </summary>
    ///
    /// <param name="cancellationToken">A token that can be used to signal a request for cancellation.</param>
    ///
    /// <returns>The player's response that was read, in <see cref="string"/> form.</returns>
    ///
    public async Task<string?> ReadPlayerResponseAsnyc(CancellationToken cancellationToken = default) =>
        await System.Console.In.ReadLineAsync(cancellationToken);

    /// <summary>
    ///   Renders the game board in a formatted grid layout.
    /// </summary>
    ///
    /// <param name="gameState">The current state of the game.</param>
    ///
    private static void RenderBoard(GameState gameState)
    {
        var tokensPerRow = gameState.TokensPerRow;
        var board = gameState.Board;

        System.Console.WriteLine("Game Board:");
        System.Console.WriteLine();

        // Render column headers.

        System.Console.Write("   ");

        for (var column = 1; column <= tokensPerRow; ++column)
        {
            System.Console.Write($"  {column}  ");
        }

        System.Console.WriteLine();

        // Render top border.

        System.Console.Write("   ");

        for (var column = 0; column < tokensPerRow; ++column)
        {
            System.Console.Write("+----");
        }

        System.Console.WriteLine("+");

        // Render each row.

        for (var row = 0; row < tokensPerRow; ++row)
        {
            // Row label and content.

            System.Console.Write($" {row + 1} ");

            for (var column = 0; column < tokensPerRow; ++column)
            {
                var boardIndex = (row * tokensPerRow) + column;
                var cellValue = board[boardIndex];
                var displayValue = cellValue == 0 ? " " : cellValue.ToString();

                System.Console.Write($"| {displayValue,2} ");
            }

            System.Console.WriteLine("|");

            // Row separator.

            System.Console.Write("   ");

            for (var column = 0; column < tokensPerRow; ++column)
            {
                System.Console.Write("+----");
            }

            System.Console.WriteLine("+");
        }
    }

    /// <summary>
    ///   Renders information about the players and their available tokens.
    /// </summary>
    ///
    /// <param name="gameState">The current state of the game.</param>
    ///
    private static void RenderPlayerInfo(GameState gameState)
    {
        System.Console.WriteLine("Players:");

        // Odd player information.

        var oddTokens = gameState.GetPlayerTokens(PlayerToken.Odd);
        var oddTokensDisplay = oddTokens.Count > 0
            ? string.Join(", ", oddTokens.OrderBy(t => t))
            : "None";

        var oddMarker = gameState.CurrentTurn == PlayerToken.Odd ? "> " : "  ";
        System.Console.WriteLine($"{oddMarker}Odd Player:  {oddTokensDisplay}");

        // Even player information.

        var evenTokens = gameState.GetPlayerTokens(PlayerToken.Even);
        var evenTokensDisplay = evenTokens.Count > 0
            ? string.Join(", ", evenTokens.OrderBy(t => t))
            : "None";

        var evenMarker = gameState.CurrentTurn == PlayerToken.Even ? "> " : "  ";
        System.Console.WriteLine($"{evenMarker}Even Player: {evenTokensDisplay}");
    }

    /// <summary>
    ///   Renders the current game status including whose turn it is and if there's a winner.
    /// </summary>
    ///
    /// <param name="writer">The writer to render to.</param>
    /// <param name="gameState">The current state of the game.</param>
    ///
    private static void RenderGameStatus(GameState gameState)
    {
        System.Console.WriteLine("Status:");

        if (gameState.Winner is not null)
        {
            var winnerName = gameState.Winner == PlayerToken.Odd ? "Odd Player" : "Even Player";
            System.Console.WriteLine($"*** {winnerName} wins! ***");
        }
        else if (gameState.IsGameOver)
        {
            System.Console.WriteLine("Game Over - It's a draw!");
        }
        else
        {
            var currentPlayerName = gameState.CurrentTurn == PlayerToken.Odd ? "Odd Player" : "Even Player";
            System.Console.WriteLine($"It's {currentPlayerName}'s turn");
            System.Console.WriteLine($"Target sum: {gameState.WinningTotal}");
        }
    }
}
