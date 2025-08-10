using System.Runtime.CompilerServices;

namespace Squire.NumTic;

/// <summary>
///   The current state of the game.  Data is expected to mutate
///   as the game is played.
/// </summary>
///
/// <param name="CurrentTurn">The token that is to play next.</param>
/// <param name="Board">The game board.</param>
/// <param name="WinningTotal">The total that a winning row, column, or vertical should add up to for a win.</param>
/// <param name="Tokens">The available tokens for each player.</param>
///
public record GameState
{
    /// <summary>The token that represents an empty board space.</summary>
    public static readonly byte EmptyBoardSpaceValue = default;

    /// <summary>The default tokens per row in a standard 3x3 game of tic-tac-toe.</summary>
    private const int DefaultTokensPerRow = 3;

    /// <summary>The default winning set of lines on a standard 3x3 game of tic-tac-toe board that need to be scanned for winning conditions.</summary>
    private static readonly int[] DefaultWinningLines = ComputeWinningLines(DefaultTokensPerRow);

    /// <summary>The pre-computed set of lines on the board that need to be scanned for winning conditions.</summary>
    private readonly int[] WinningLines;

    /// <summary>
    ///   Identifies the player who is next to play a turn.  This member is mutable and its
    ///   value will change as the game progresses.
    /// </summary>
    ///
    public PlayerToken CurrentTurn { get; set; }

    /// <summary>
    ///   The player who has won the game, if any.  This member is mutable and its
    ///   value will change as the game progresses.
    /// </summary>
    ///
    /// <value>
    ///   The <see cref="PlayerToken"/> of the winner, or <c>null</c> if the game has no winner."/>
    /// </value>
    ///
    public PlayerToken? Winner { get; private set; }

    /// <summary>
    ///   Indicates whether the game is over.  This member is mutable and its
    ///   value will change as the game progresses.
    /// </summary>
    ///
    /// <value>
    ///   <c>true</c> if this game is over; otherwise, <c>false</c>.
    /// </value>
    ///
    public bool IsGameOver => ((Winner is not null) || (CurrentPlayerTokens.Count == 0) || (!AreEmptySpaces(Board)));

    /// <summary>
    ///  The number of tokens per row on the game board.
    /// </summary>
    ///
    public int TokensPerRow { get; init; }

    /// <summary>
    ///   The board for a game of numeric tic-tac-toe.  The underlying data
    ///   is mutable and its value will change as the game progresses.
    /// </summary>
    ///
    /// <value>
    ///   The game board represented as a 1-dimensional array.
    /// </value>
    ///
    public byte[] Board { get; init; }

    /// <summary>
    ///   The total that a winning row, column, or diagonal
    ///   should add up to for a win.
    /// </summary>
    ///
    public int WinningTotal { get; init; }

    /// <summary>
    ///   The numeric tokens remaining for each player.  The underlying data
    ///   is mutable and its value will change as the game progresses.
    /// </summary>
    ///
    HashSet<byte>[] Tokens { get; init; }

    /// <summary>
    ///   Gets the available tokens for the current player.
    /// </summary>
    ///
    /// <returns>The available tokens for the current player.</returns>
    ///
    public HashSet<byte> CurrentPlayerTokens => Tokens[(int)CurrentTurn];

    /// <summary>
    ///   Initializes a new instance of the <see cref="GameState"/> record.
    /// </summary>
    ///
    /// <param name="startingTurn">The player who has the first turn.</param>
    /// <param name="board">The game board, represented as a 1-dimensional array.</param>
    /// <param name="winningTotal">The total that a winning row, column, or diagonal should add up to for a win.</param>
    /// <param name="tokens"> The numeric tokens available to each player at the start of the game.</param>
    ///
    /// <exception cref="ArgumentNullException">Occurs when <paramref name="board"/> or <paramref name="tokens"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Occurs when the board is not a perfect square.</exception>
    ///
    public GameState(PlayerToken startingTurn,
                     byte[] board,
                     int winningTotal,
                     HashSet<byte>[] tokens)
    {
        ArgumentNullException.ThrowIfNull(board, nameof(board));
        ArgumentNullException.ThrowIfNull(tokens, nameof(tokens));

        var boardLength = board.Length;
        var expectedTokensPerRow = (int)Math.Sqrt(boardLength);

        if (boardLength != (expectedTokensPerRow * expectedTokensPerRow))
        {
            throw new InvalidOperationException("Board must be a perfect square for tic-tac-toe games.");
            ;
        }

        TokensPerRow = expectedTokensPerRow;
        CurrentTurn = startingTurn;
        Board = board;
        WinningTotal = winningTotal;
        Tokens = tokens;

        WinningLines = expectedTokensPerRow switch
        {
            DefaultTokensPerRow => DefaultWinningLines,
            _ => ComputeWinningLines(expectedTokensPerRow)
        };
    }

    /// <summary>
    ///   Validates that the row and column positions are within the expected range for the board.
    /// </summary>
    ///
    /// <param name="row">The row position to validate.</param>
    /// <param name="column">The column position to validate.</param>
    ///
    /// <exception cref="ArgumentOutOfRangeException">Thrown when row or column is not valid for the game board.</exception>
    ///
    public void AssertValidBoardCoordinates(int row,
                                            int column)
    {
        var rowAndColSize = (uint)TokensPerRow;

        if ((uint)row - 1 >= rowAndColSize)
        {
            throw new ArgumentOutOfRangeException(nameof(row), $"The row must be between 1 and {TokensPerRow}, inclusive.");
        }

        if ((uint)column - 1 >= rowAndColSize)
        {
            throw new ArgumentOutOfRangeException(nameof(column), $"The column must be between 1 and {TokensPerRow}, inclusive.");
        }
    }

    /// <summary>
    ///   Gets the available tokens for the specified player.
    /// </summary>
    ///
    /// <param name="player">The player token type.</param>
    ///
    /// <returns>The available tokens for the specified player.</returns>
    ///
    public HashSet<byte> GetPlayerTokens(PlayerToken player) => Tokens[(int)player];

    /// <summary>
    ///   Determines whether the specified board position is empty.
    /// </summary>
    ///
    /// <param name="row">The row of the board coordinates to check.</param>
    /// <param name="column">The column of the board coordinates to check.</param>
    ///
    /// <returns><c>true</c> if the specified position is empty; otherwise, <c>false</c>.</returns>
    ///
    public bool IsEmptyPosition(int row,
                                int column)
    {
        AssertValidBoardCoordinates(row, column);
        return Board[GetBoardPositionIndexUnchecked(row, column, TokensPerRow)] == EmptyBoardSpaceValue;
    }

    /// <summary>
    ///   Applies a move to the game board and updates the game state accordingly.
    /// </summary>
    ///
    /// <param name="move">The move to apply.</param>
    ///
    /// <remarks>
    ///   Applying a move will mutate the state of the game.  It will:
    ///     - Update the game board
    ///     - Remove the token from the current player's available tokens
    ///     - Update the current player's turn, if the game has not been won
    ///     - Update the winner, if the game has been won
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">Occurs when the <paramref name="move.PositionIndex"/> is out of bounds for the game board.</exception>
    /// <exception cref="InvalidOperationException">Occurs when the requested token is not available for the current player.</exception>
    /// <exception cref="InvalidOperationException">The requested position for the move is already occupied.</exception>
    ///
    public void ApplyMove(Move move)
    {
        if (!CurrentPlayerTokens.Contains(move.Token))
        {
            throw new InvalidOperationException($"The token {move.Token} is not available for the current player.");
        }

        if ((uint)move.PositionIndex >= (uint)Board.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(move.PositionIndex), $"The position index must be between 0 and {Board.Length - 1}, inclusive.");
        }

        if (Board[move.PositionIndex] != EmptyBoardSpaceValue)
        {
            var (row, column) = GetBoardCoordinates(move.PositionIndex);
            throw new InvalidOperationException($"The position at row {row}, column {column} is already occupied.");
        }

        Board[move.PositionIndex] = move.Token;
        CurrentTurn = move.Player;
        CurrentPlayerTokens.Remove(move.Token);

        Winner = ScanForWinner();

        // Alternate the player's turn only if there is no winner.

        if (Winner is null)
        {
            AlternatePlayerTurn();
        }
    }

    /// <summary>
    ///   Resets a previous move by the cu game board and updates the game state accordingly.
    /// </summary>
    ///
    /// <param name="move">The move to revert.</param>
    ///
    /// <exception cref="ArgumentOutOfRangeException">Occurs when the <paramref name="move.PositionIndex"/> is out of bounds for the game board.</exception>
    /// <exception cref="InvalidOperationException">The position is already empty.</exception>
    ///
    public void UndoMove(Move move)
    {
        if ((uint)move.PositionIndex >= (uint)Board.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(move.PositionIndex), $"The position index must be between 0 and {Board.Length - 1}, inclusive.");
        }

        if (Board[move.PositionIndex] == EmptyBoardSpaceValue)
        {
            var (row, column) = GetBoardCoordinates(move.PositionIndex);
            throw new InvalidOperationException($"The position at row {row}, column {column} is already empty.");
        }

        Board[move.PositionIndex] = EmptyBoardSpaceValue;
        CurrentTurn = move.Player;
        CurrentPlayerTokens.Add(move.Token);

        // Re-scan for winner after undoing the move.

        if (Winner is not null)
        {
            Winner = ScanForWinner();
        }
    }

    /// <summary>
    ///   Converts an board position into the corresponding row and column coordinates.
    /// </summary>
    ///
    /// <param name="position">The board position to convert.</param>
    ///
    /// <returns>A named tuple containing the row and column coordinates (1-based).</returns>
    ///
    /// <exception cref="ArgumentNullException">Thrown when instance is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when index is not valid for the game board.</exception>
    ///
    public (int Row, int Column) GetBoardCoordinates(int position)
    {
        if ((uint)position >= (uint)Board.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(position), $"The index must be between 0 and {Board.Length}, inclusive.");
        }

        var row = (position / TokensPerRow) + 1;
        var column = (position % TokensPerRow) + 1;
        return (row, column);
    }

    /// <summary>
    ///   Gets the position on the board that corresponds to the provided <paramref name="row"/> and
    ///   <paramref name="column"/> coordinates.
    /// </summary>
    ///
    /// <param name="row">The row to query the board position for.</param>
    /// <param name="column">The column to query the board position for.</param>
    ///
    /// <returns>The corresponding the board position.</returns>
    ///
    /// <exception cref="ArgumentOutOfRangeException">Thrown when row or column is not valid for the game board.</exception>
    ///
    public int GetBoardPosition(int row,
                                int column)
    {
        AssertValidBoardCoordinates(row, column);
        return GetBoardPositionIndexUnchecked(row, column, TokensPerRow);
    }

    /// <summary>
    ///   Alternates the current player turn.
    /// </summary>
    ///
    /// <remarks>
    ///   Alternating the player turn will mutate the state of the game.
    /// </remarks>
    ///
    public void AlternatePlayerTurn()
    {
        CurrentTurn = CurrentTurn switch
        {
            PlayerToken.Odd => PlayerToken.Even,
            PlayerToken.Even => PlayerToken.Odd,
            _ => throw new InvalidOperationException($"Unknown player token: {CurrentTurn}")
        };
    }

    /// <summary>
    ///   Attempts to find a winning move for the specified player.
    /// </summary>
    ///
    /// <param name="player">The player to consider when searching.</param>
    ///
    /// <returns>A <see cref="Move"/> that will win the game for the requested <paramref name="player"/>, if one exists; otherwise, <c>null</c>.</returns>
    ///
    /// <remarks>
    ///   If multiple winning moves exist, there is no guarantee which one will be returned.  The search is
    ///   stable, and calls for the same player and state will return the same result.
    /// </remarks>
    ///
    public Move? FindWinningMove(PlayerToken player)
    {
        var tokensPerRow = TokensPerRow;
        var winningTotal = WinningTotal;
        var tokens = Tokens[(int)player];
        var winningLines = WinningLines.AsSpan();
        var board = Board.AsSpan();

        // Attempt to see if any of the current player's tokens can win the game.

        for (var index = 0; index < winningLines.Length; index += tokensPerRow)
        {
            var emptyIndex = -1;
            var boardIndex = 0;
            var value = 0;
            var sum = 0;

            for (var offset = 0; offset < tokensPerRow; ++offset)
            {
                boardIndex = winningLines[index + offset];
                value = board[boardIndex];

                if (value != EmptyBoardSpaceValue)
                {
                    sum += value;
                }
                else
                {
                    // If there is already an empty space, this line cannot be a winning move.

                    if (emptyIndex != -1)
                    {
                        emptyIndex = -1;
                        break;
                    }

                    emptyIndex = boardIndex;
                }
            }

            // If there is a single empty space, determine if one of the current player's
            // tokens can win.

            if (emptyIndex != -1)
            {
                var neededToken = (byte)(winningTotal - sum);

                if (tokens.Contains(neededToken))
                {
                    return new Move(player, emptyIndex, neededToken);
                }
            }
        }

        // No winning move was found.

        return null;
    }

    /// <summary>
    ///   Determines if the game has been won and returns the winning player and, if so,
    ///   sets the <see cref="Winner"/> property.
    /// </summary>
    ///
    /// <returns>The player token of the winner if the game has been won, null otherwise.</returns>
    ///
    internal PlayerToken? ScanForWinner()
    {
        var tokensPerRow = TokensPerRow;
        var winningTotal = WinningTotal;
        var winningLines = WinningLines.AsSpan();
        var board = Board.AsSpan();

        for (var index = 0; index < winningLines.Length; index += tokensPerRow)
        {
            var sum = 0;

            for (var offset = 0; offset < tokensPerRow; ++offset)
            {

                var value =  board[winningLines[index + offset]];

                // Winning requires that every board position for the combination is
                // occupied.  If any position is empty, a win is not possible and
                // there is no need to keep scanning this combination.

                if (value == EmptyBoardSpaceValue)
                {
                    sum = 0;
                    break;
                }

                sum += value;
            }

            if (sum == winningTotal)
            {
                Winner = CurrentTurn;
                return CurrentTurn;
            }
        }

        return null;
    }

    /// <summary>
    ///   Creates a deep copy of the current game state.
    /// </summary>
    ///
    /// <returns>A new <see cref="GameState"/> instance that is a deep copy of the current state.</returns>
    ///
    internal GameState CreateCopy() =>
        new GameState(
            CurrentTurn,
            [.. Board],
            WinningTotal,
            [[.. GetPlayerTokens(PlayerToken.Odd)], [.. GetPlayerTokens(PlayerToken.Even)]])
        {
           Winner = this.Winner
        };

    /// <summary>
    ///   Creates a new game using the defaults of a standard
    ///   3x3 board and maximum score of 15.
    /// </summary>
    ///
    /// <returns>An instance of state representing a new game.</returns>
    ///
    public static GameState CreateDefault() => new(
            PlayerToken.Odd,
            new byte[DefaultTokensPerRow * DefaultTokensPerRow],
            15,
            [
                new HashSet<byte> { 1, 3, 5, 7, 9 },
                new HashSet<byte> { 2, 4, 6, 8 }
            ]);

    /// <summary>
    ///   Computes the winning combinations for a given square game board..
    /// </summary>
    ///
    /// <param name="tokensPerRow">The number of tokens per row in the game board.</param>
    ///
    /// <returns>An array of winning combinations.</returns>
    ///
    /// <remarks>
    ///   It is assumed that the game board has been validated externally and represents
    ///   a square.
    /// </remarks>
    ///
    private static int[] ComputeWinningLines(int tokensPerRow)
    {

        // Calculate the total number of winning combinations:
        // (rows + columns + 2 diagonals) * number of tokens per row.

        var totalCombinations = (tokensPerRow + tokensPerRow + 2) * tokensPerRow;
        var combinations = new int[totalCombinations];
        var combinationIndex = 0;

        // Add rows.

        for (var row = 0; row < tokensPerRow; ++row)
        {
            for (var column = 0; column < tokensPerRow; ++column)
            {
                combinations[combinationIndex++] = row * tokensPerRow + column;
            }
        }

        // Add columns.

        for (var column = 0; column < tokensPerRow; ++column)
        {
            for (var row = 0; row < tokensPerRow; ++row)
            {
                combinations[combinationIndex++] = row * tokensPerRow + column;
            }
        }

        // Add main diagonal (top-left to bottom-right).

        for (var index = 0; index < tokensPerRow; ++index)
        {
            combinations[combinationIndex++] = index * tokensPerRow + index;
        }

        // Add anti-diagonal (top-right to bottom-left).

        for (var index = 0; index < tokensPerRow; ++index)
        {
            combinations[combinationIndex++] = index * tokensPerRow + (tokensPerRow - 1 - index);
        }

        return combinations;
    }

    /// <summary>
    ///  Determines if there are any empty spaces left on the board.
    /// </summary>
    ///
    /// <param name="board">The board to consider.</param>
    ///
    /// <returns><c>true</c> if at least one empty space remains on the board; otherwise, <c>false</c>.</returns>
    ///
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool AreEmptySpaces(byte[] board)
    {
        // Check if there are any empty spaces left on the board.

        for (var index = 0; index < board.Length; ++index)
        {
            if (board[index] == EmptyBoardSpaceValue)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///   Gets the index of the board array for the provided <paramref name="row"/> and
    ///   <paramref name="column"/> without performing any bounds checking or validation.
    /// </summary>
    ///
    /// <param name="row">The row to query the board position index for.</param>
    /// <param name="column">The column to query the board position index for.</param>
    /// <param name="tokensPerRow">The number tokens per row in the board.</param>
    ///
    /// <returns>The index of the board position, assuming the input is valid.</returns>
    ///
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetBoardPositionIndexUnchecked(int row,
                                                      int column,
                                                      int tokensPerRow)
    {
        return ((row - 1) * tokensPerRow) + (column - 1);
    }
}