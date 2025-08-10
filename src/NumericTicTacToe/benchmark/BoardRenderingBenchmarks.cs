using System.Buffers;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Squire.NumTic.Benchmark;

/// <summary>
///   Benchmarks comparing different board rendering approaches for performance analysis.
/// </summary>
///
[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
[CategoriesColumn]
public class BoardRenderingBenchmarks
{
    /// <summary>The width of each cell in characters.</summary>
    private const int CellWidth = 5;

    /// <summary>The height of each cell in lines.</summary>
    private const int CellHeight = 3;

    /// <summary>The threshold for using stackalloc vs heap allocation.</summary>
    private const int StackAllocThreshold = 1024;

    /// <summary>The string representation of an empty cell.</summary>
    private const string EmptyCell = "     ";

    /// <summary>The game state used for rendering benchmarks.</summary>
    private GameState _gameState = null!;

    /// <summary>The format mask for string.Format operations.</summary>
    private string _formatMask = null!;

    /// <summary>The pre-parsed CompositeFormat for efficient repeated formatting.</summary>
    private CompositeFormat _compositeFormat = null!;

    /// <summary>The pre-formatted token strings for rendering.</summary>
    private string[] _formattedTokens = null!;

    /// <summary>
    ///   Initializes the benchmark test data with a realistic game state.
    /// </summary>
    ///
    [GlobalSetup]
    public void Setup()
    {
        // Create a default game state and place some tokens for realistic testing.

        _gameState = GameState.CreateDefault();

        // Apply moves to place some tokens to make the rendering more realistic.

        _gameState.ApplyMove(new Move(PlayerToken.Odd, 0, 1)); // Odd player places 1
        _gameState.ApplyMove(new Move(PlayerToken.Even, 4, 2)); // Even player places 2
        _gameState.ApplyMove(new Move(PlayerToken.Odd, 2, 3)); // Odd player places 3
        _gameState.ApplyMove(new Move(PlayerToken.Even, 8, 4)); // Even player places 4
        _gameState.ApplyMove(new Move(PlayerToken.Odd, 1, 5)); // Odd player places 5

        _formatMask = BuildGameBoardFormatMask(_gameState.TokensPerRow);
        _compositeFormat = CompositeFormat.Parse(_formatMask);
        _formattedTokens = new string[_gameState.Board.Length];
    }

    /// <summary>
    ///   Benchmarks the current string.Format approach to board rendering.
    /// </summary>
    ///
    /// <returns>The rendered board as a string.</returns>
    ///
    [Benchmark(Baseline = true)]
    public string CurrentStringFormatApproach()
    {
        // Simulate multiple format calls like a real game.

        var maxMoves = _gameState.Board.Length;
        string result = null!;

        for (var move = 0; move < maxMoves; ++move)
        {
            // Current implementation using string.Format.

            for (var index = 0; index < _gameState.Board.Length; ++index)
            {
                var token = _gameState.Board[index];
                _formattedTokens[index] = token == GameState.EmptyBoardSpaceValue
                    ? EmptyCell
                    : FormatToken(token);
            }

            result = string.Format(_formatMask, _formattedTokens);
        }

        return result;
    }

    /// <summary>
    ///   Benchmarks a real-world CompositeFormat approach using the actual ConsoleGameInterface pattern.
    /// </summary>
    ///
    /// <returns>The rendered board as a string.</returns>
    ///
    [Benchmark]
    public string RealWorldCompositeFormatApproach()
    {
        var tokensPerRow = _gameState.TokensPerRow;
        var board = _gameState.Board.AsSpan();
        var maxMoves = tokensPerRow * tokensPerRow;

        // Simulate multiple format calls like a real game.

        string result = null!;
        for (var move = 0; move < maxMoves; ++move)
        {
            // Prepare the formatted tokens (real-world approach).

            var tokenStrings = new object[maxMoves];
            var formattedTokens = tokenStrings.AsSpan();

            for (var index = 0; index < formattedTokens.Length; ++index)
            {
                var token = board[index];
                formattedTokens[index] = token == GameState.EmptyBoardSpaceValue
                    ? EmptyCell
                    : FormatToken(token);
            }

            // Single format operation using pre-parsed CompositeFormat.

            result = string.Format(null, _compositeFormat, tokenStrings);
        }

        return result;
    }

    /// <summary>
    ///   Benchmarks a pure string interpolation approach using TryWrite pattern.
    /// </summary>
    ///
    /// <returns>The rendered board as a string.</returns>
    ///
    [Benchmark]
    public string RealWorldInterpolatedApproach()
    {
        var tokensPerRow = _gameState.TokensPerRow;
        var board = _gameState.Board.AsSpan();
        var estimatedSize = CalculateStringBufferSize(tokensPerRow, CellWidth, CellHeight, 5);

        Span<char> buffer = estimatedSize <= StackAllocThreshold
            ? stackalloc char[estimatedSize]
            : new char[estimatedSize];

        var success = buffer.TryWrite($"{BuildInterpolatedBoard(tokensPerRow, board)}", out int charsWritten);

        return success ? new string(buffer.Slice(0, charsWritten)) : string.Empty;
    }

    /// <summary>
    ///   Benchmarks an optimized StringBuilder approach with pre-calculated capacity.
    /// </summary>
    ///
    /// <returns>The rendered board as a string.</returns>
    ///
    [Benchmark]
    public string RealWorldStringBuilderApproach()
    {
        var tokensPerRow = _gameState.TokensPerRow;
        var board = _gameState.Board.AsSpan();
        var estimatedSize = CalculateStringBufferSize(tokensPerRow, CellWidth, CellHeight, 5);

        var sb = new StringBuilder(estimatedSize);

        for (var boardRow = 0; boardRow < tokensPerRow; ++boardRow)
        {
            for (var cellRow = 0; cellRow < CellHeight; ++cellRow)
            {
                for (var cellColumn = 0; cellColumn < tokensPerRow; ++cellColumn)
                {
                    var tokenCenterRow = CellHeight / 2;
                    var position = boardRow * tokensPerRow + cellColumn;
                    var token = board[position];

                    if (cellRow == tokenCenterRow)
                    {
                        var tokenStr = token == GameState.EmptyBoardSpaceValue
                            ? EmptyCell
                            : FormatToken(token);
                        sb.Append(tokenStr);
                    }
                    else
                    {
                        sb.Append(EmptyCell);
                    }

                    if (cellColumn < tokensPerRow - 1)
                    {
                        sb.Append("[]│[/]");
                    }
                }

                sb.AppendLine();

                if (boardRow < tokensPerRow - 1 && cellRow == CellHeight - 1)
                {
                    for (var col = 0; col < tokensPerRow; ++col)
                    {
                        sb.Append("─────");
                        if (col < tokensPerRow - 1)
                        {
                            sb.Append("[]┼[/]");
                        }
                    }
                    sb.AppendLine();
                }
            }
        }

        return sb.ToString();
    }

    /// <summary>
    ///   Formats a token value for display in a cell.
    /// </summary>
    ///
    /// <param name="token">The token value to format.</param>
    ///
    /// <returns>The formatted token string.</returns>
    ///
    private static string FormatToken(byte token) => $"  {token}  ";

    /// <summary>
    ///   Builds an interpolated board string for the specified size and board state.
    /// </summary>
    ///
    /// <param name="tokensPerRow">The number of tokens per row.</param>
    /// <param name="board">The current board state.</param>
    ///
    /// <returns>The interpolated board string.</returns>
    ///
    private string BuildInterpolatedBoard(int tokensPerRow, ReadOnlySpan<byte> board)
    {
        var result = string.Empty;

        for (var boardRow = 0; boardRow < tokensPerRow; ++boardRow)
        {
            for (var cellRow = 0; cellRow < CellHeight; ++cellRow)
            {
                var tokenCenterRow = CellHeight / 2;

                for (var cellColumn = 0; cellColumn < tokensPerRow; ++cellColumn)
                {
                    if (cellRow == tokenCenterRow)
                    {
                        var position = boardRow * tokensPerRow + cellColumn;
                        var token = board[position];
                        var tokenDisplay = token == GameState.EmptyBoardSpaceValue
                            ? EmptyCell
                            : FormatToken(token);

                        result = $"{result}{tokenDisplay}";
                    }
                    else
                    {
                        result = $"{result}{EmptyCell}";
                    }

                    if (cellColumn < tokensPerRow - 1)
                    {
                        result = $"{result}[]│[/]";
                    }
                }

                result = $"{result}\n";
            }

            if (boardRow < tokensPerRow - 1)
            {
                for (var col = 0; col < tokensPerRow; ++col)
                {
                    result = $"{result}─────";
                    if (col < tokensPerRow - 1)
                    {
                        result = $"{result}[]┼[/]";
                    }
                }
                result = $"{result}\n";
            }
        }

        return result;
    }

    /// <summary>
    ///   Builds the format mask used for string.Format board rendering.
    /// </summary>
    ///
    /// <param name="tokensPerRow">The number of tokens per row in the game board.</param>
    ///
    /// <returns>The format mask string with placeholders.</returns>
    ///
    private static string BuildGameBoardFormatMask(int tokensPerRow)
    {
        var estimatedSize = CalculateStringBufferSize(tokensPerRow, CellWidth, CellHeight, 3);
        var buffer = estimatedSize <= StackAllocThreshold
            ? stackalloc char[estimatedSize]
            : new char[estimatedSize];

        var position = 0;
        var placeholderIndex = 0;
        var tokenCenterRow = CellHeight / 2;

        for (var boardRow = 0; boardRow < tokensPerRow; ++boardRow)
        {
            for (var cellRow = 0; cellRow < CellHeight; ++cellRow)
            {
                for (var cellColumn = 0; cellColumn < tokensPerRow; ++cellColumn)
                {
                    if (cellRow == tokenCenterRow)
                    {
                        buffer[position++] = '{';
                        var indexStr = placeholderIndex.ToString();
                        for (int i = 0; i < indexStr.Length; i++)
                        {
                            buffer[position++] = indexStr[i];
                        }
                        buffer[position++] = '}';
                        placeholderIndex++;
                    }
                    else
                    {
                        EmptyCell.AsSpan().CopyTo(buffer.Slice(position));
                        position += EmptyCell.Length;
                    }

                    if (cellColumn < tokensPerRow - 1)
                    {
                        "[]│[/]".AsSpan().CopyTo(buffer.Slice(position));
                        position += 6;
                    }
                }

                buffer[position++] = '\n';

                if (boardRow < tokensPerRow - 1 && cellRow == CellHeight - 1)
                {
                    for (var col = 0; col < tokensPerRow; ++col)
                    {
                        "─────".AsSpan().CopyTo(buffer.Slice(position));
                        position += 5;
                        if (col < tokensPerRow - 1)
                        {
                            "[]┼[/]".AsSpan().CopyTo(buffer.Slice(position));
                            position += 6;
                        }
                    }
                    buffer[position++] = '\n';
                }
            }
        }

        return new string(buffer.Slice(0, position));
    }

    /// <summary>
    ///   Calculates the estimated buffer size needed for rendering the board.
    /// </summary>
    ///
    /// <param name="tokensPerRow">The number of tokens per row in the game board.</param>
    /// <param name="cellWidth">The width of each cell in characters.</param>
    /// <param name="cellHeight">The height of each cell in lines.</param>
    /// <param name="placeholderLength">The length of placeholder strings.</param>
    ///
    /// <returns>The estimated buffer size in characters.</returns>
    ///
    private static int CalculateStringBufferSize(int tokensPerRow, int cellWidth, int cellHeight, int placeholderLength)
    {
        var contentWidth = tokensPerRow * cellWidth + (tokensPerRow - 1) * 6; // 6 chars for "[]│[/]"
        var contentHeight = tokensPerRow * cellHeight + (tokensPerRow - 1); // separator rows
        return contentWidth * contentHeight + contentHeight; // +contentHeight for newlines
    }
}
