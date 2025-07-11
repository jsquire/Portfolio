using System.Runtime.CompilerServices;

namespace Squire.NumTic.Game;

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
    /// <summary>The default tokens per row in a standard 3x3 game of tic-tac-toe.</summary>
    private const int DefaultTokensPerRow = 3;

    /// <summary>The token that represents an empty board space.</summary>
    private static readonly int EmptyBoardValue = default;

    /// <summary>The default winning combinations for a standard 3x3 game of tic-tac-toe.</summary>
    private static readonly int[][] DefaultWinningCombinations = ComputeWinningCombinations(DefaultTokensPerRow);

    /// <summary>The pre-computed winning combinations for the current board.</summary>
    private readonly int[][] _winningCombinations;

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
    public bool IsGameOver => ((Winner is not null) || (CurrentPlayerTokens.Count == 0));

    /// <summary>
    ///  The number of tokens per row on the game board.
    /// </summary>
    ///
    public int TokensPerRow { get; private set; }

    /// <summary>
    ///   The board for a game of numeric tic-tac-toe.  The underlying data
    ///   is mutable and its value will change as the game progresses.
    /// </summary>
    ///
    /// <value>
    ///   The game board represented as a 1-dimensional array.
    /// </value>
    ///
    public int[] Board { get; init; }

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
                     int[] board,
                     int winningTotal,
                     HashSet<byte>[] tokens)
    {
        ArgumentNullException.ThrowIfNull(board, nameof(board));
        ArgumentNullException.ThrowIfNull(tokens, nameof(tokens));

        var boardLength = board.Length;
        var expectedTokensPerRow = (int)Math.Sqrt(boardLength);

        if (boardLength != (expectedTokensPerRow * expectedTokensPerRow))
        {
            throw new InvalidOperationException("Board must be a perfect square for tic-tac-toe games.");;
        }

        TokensPerRow = expectedTokensPerRow;
        CurrentTurn = startingTurn;
        Board = board;
        WinningTotal = winningTotal;
        Tokens = tokens;

        _winningCombinations = expectedTokensPerRow switch
        {
            DefaultTokensPerRow => DefaultWinningCombinations,
            _ => ComputeWinningCombinations(expectedTokensPerRow)
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
    public void AssertValidBoardPosition(int row,
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
    ///   Gets the token at the specified row and column position of the board
    ///   without explicit bounds checking.
    /// </summary>
    ///
    /// <param name="row">The row position.</param>
    /// <param name="column">The column position.</param>
    ///
    /// <returns>The token value at the specified position.</returns>
    ///
    public int GetBoardToken(int row,
                             int column) =>
        Board[GetBoardPositionIndexUnchecked(row, column, TokensPerRow)];

    /// <summary>
    ///   Sets a token at the specified position of the board
    ///   without explicit bounds checking.
    /// </summary>
    ///
    /// <param name="row">The row position.</param>
    /// <param name="column">The column position.</param>
    /// <param name="token">The token value to set.</param>M
    ///
    /// <remarks>
    ///   Setting a token will mutate the game board.
    ///
    ///   This operation does not perform any validation.  Callers
    ///   are responsible for ensuring the token is available to the
    ///   player and that there is no existing token at that position.
    /// </remarks>
    ///
    public void SetBoardToken(int row,
                              int column,
                              int token) =>
        Board[GetBoardPositionIndexUnchecked(row, column, TokensPerRow)] = token;

    /// <summary>
    ///   Determines whether the specified board position is empty.
    /// </summary>
    ///
    /// <param name="row">The row of the board position to check.</param>
    /// <param name="column">The column of the board position to check.</param>
    ///
    /// <returns><c>true</c> if the specified position is empty; otherwise, <c>false</c>.</returns>
    ///
    public bool IsEmptyPosition(int row,
                                int column)
    {
        AssertValidBoardPosition(row, column);
        return Board[GetBoardPositionIndexUnchecked(row, column, TokensPerRow)] == EmptyBoardValue;
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
    /// <exception cref="ArgumentNullException">Occurs when the <paramref name="move"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Occurs when the <paramref name="move.PositionIndex"/> is out of bounds for the game board.</exception>
    /// <exception cref="InvalidOperationException">Occurs when the requested token is not available for the current player.</exception>
    /// <exception cref="InvalidOperationException">The requested position for the move is already occupied.</exception>
    ///
    public void ApplyMove(Move move)
    {
        ArgumentNullException.ThrowIfNull(move, nameof(move));

        if (!CurrentPlayerTokens.Contains(move.Token))
        {
            throw new InvalidOperationException($"The token {move.Token} is not available for the current player.");
        }

        if ((uint)move.PositionIndex >= (uint)Board.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(move.PositionIndex), $"The position index must be between 0 and {Board.Length - 1}, inclusive.");
        }

        if (Board[move.PositionIndex] != EmptyBoardValue)
        {
            var (row, column) = GetBoardPositionFromIndex(move.PositionIndex);
            throw new InvalidOperationException($"The position at row {row}, column {column} is already occupied.");
        }

        Board[move.PositionIndex] = move.Token;
        CurrentPlayerTokens.Remove(move.Token);

        Winner = ScanForWinner();

        // Alternate the player's turn only if there is no winner.

        if (Winner is null)
        {
            AlternatePlayerTurn();
        }
    }

    /// <summary>
    ///   Converts an array index to the corresponding row and column position.
    /// </summary>
    ///
    /// <param name="index">The array index to convert.</param>
    ///
    /// <returns>A named tuple containing the row and column positions (1-based).</returns>
    ///
    /// <exception cref="ArgumentNullException">Thrown when instance is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when index is not valid for the game board.</exception>
    ///
    public (int Row, int Column) GetBoardPositionFromIndex(int index)
    {
        if ((uint)index >= (uint)Board.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index), $"The index must be between 0 and {Board.Length}, inclusive.");
        }

        var row = (index / TokensPerRow) + 1;
        var column = (index % TokensPerRow) + 1;
        return (row, column);
    }

    /// <summary>
    ///   Gets the index of the board array for the provided <paramref name="row"/> and
    ///   <paramref name="column"/>.
    /// </summary>
    ///
    /// <param name="row">The row to query the board position index for.</param>
    /// <param name="column">The column to query the board position index for.</param>
    ///
    /// <returns>The index of the board position.</returns>
    ///
    /// <exception cref="ArgumentOutOfRangeException">Thrown when row or column is not valid for the game board.</exception>
    ///
    public int GetBoardPositionIndex(int row,
                                     int column)
    {
        AssertValidBoardPosition(row, column);
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
    ///   Determines if the game has been won and returns the winning player and, if so,
    ///   sets the <see cref="Winner"/> property.
    /// </summary>
    ///
    /// <returns>The player token of the winner if the game has been won, null otherwise.</returns>
    ///
    internal PlayerToken? ScanForWinner()
    {
        foreach (var combination in _winningCombinations)
        {
            var sum = 0;

            for (var index = 0; index < combination.Length; ++index)
            {
                sum += Board[combination[index]];
            }

            if (sum == WinningTotal)
            {
                Winner = CurrentTurn;
                return CurrentTurn;
            }
        }

        return null;
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
    private static int[][] ComputeWinningCombinations(int tokensPerRow)
    {

        // Calculate the total number of winning combinations: rows + columns + 2 diagonals.

        var totalCombinations = (2 * tokensPerRow) + 2;
        var combinations = new int[totalCombinations][];
        var combinationIndex = 0;

        // Add rows.

        for (var row = 0; row < tokensPerRow; ++row)
        {
            var rowCombination = new int[tokensPerRow];

            for (var column = 0; column < tokensPerRow; column++)
            {
                rowCombination[column] = row * tokensPerRow + column;
            }

            combinations[combinationIndex++] = rowCombination;
        }

        // Add columns.

        for (var column = 0; column < tokensPerRow; ++column)
        {
            var colCombination = new int[tokensPerRow];

            for (var row = 0; row < tokensPerRow; row++)
            {
                colCombination[row] = row * tokensPerRow + column;
            }

            combinations[combinationIndex++] = colCombination;
        }

        // Add main diagonal (top-left to bottom-right).

        var mainDiagonal = new int[tokensPerRow];

        for (var index = 0; index < tokensPerRow; index++)
        {
            mainDiagonal[index] = index * tokensPerRow + index;
        }

        combinations[combinationIndex++] = mainDiagonal;

        // Add anti-diagonal (top-right to bottom-left).

        var antiDiagonal = new int[tokensPerRow];

        for (var index = 0; index < tokensPerRow; ++index)
        {
            antiDiagonal[index] = index * tokensPerRow + (tokensPerRow - 1 - index);
        }

        combinations[combinationIndex] = antiDiagonal;

        return combinations;
    }

    /// <summary>
    ///   Creates a new game using the defaults of a standard
    ///   3x3 board and maximum score of 15.
    /// </summary>
    ///
    /// <returns>An instance of state representing a new game.</returns>
    ///
    internal static GameState CreateDefault() => new (
            PlayerToken.Odd,
            new int[DefaultTokensPerRow * DefaultTokensPerRow],
            15,
            [
                new HashSet<byte> { 1, 3, 5, 7, 9 },
                new HashSet<byte> { 2, 4, 6, 8 }
            ]);
}
