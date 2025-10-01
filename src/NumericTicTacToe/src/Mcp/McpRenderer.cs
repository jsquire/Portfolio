using System.Text;
using ModelContextProtocol.Protocol;
using Squire.NumTic;

/// <summary>
///   Renders game state and user interface elements in a markdown format suitable for
///   the MCP tool context.
/// </summary>
///
public class McpRenderer
{
    /// <summary>The threshold for using stack allocated arrays in MCP context.</summary>
    private const int StackAllocThreshold = 1024;

    /// <summary>The width of each cell for the game board.</summary>
    private const int CellWidth = 11;

    /// <summary>The height of each cell for the game board.</summary>
    private const int CellHeight = 5;

    /// <summary>The width of each cell for the position guide.</summary>
    private const int PositionGuideCellWidth = 3;

    /// <summary>The height of each cell for the position guide.</summary>
    private const int PositionGuideCellHeight = 1;

    /// <summary>The number of spaces to add before position guide content for alignment.</summary>
    private const int PositionGuideSpacing = 20;

    /// <summary>The character used to represent an empty cell in the game board.</summary>
    private static readonly string EmptyCell = new string(' ', 3);

    /// <summary>Composite format mask for game board rendering.</summary>
    private readonly CompositeFormat GameBoardFormatMask;

    /// <summary>Composite format mask for complete UI with board and position guide.</summary>
    private readonly CompositeFormat BoardWithGuideFormatMask;

    /// <summary>Composite format mask for complete UI including headers, code blocks, and player info.</summary>
    private readonly CompositeFormat GameStateFormatMask;

    /// <summary>Formatted strings for token values to eliminate runtime formatting.</summary>
    private readonly string[] FormattedTokenLookup;

    /// <summary>
    ///   Initializes the <see cref="McpRenderer"/> class.
    /// </summary>
    ///
    public McpRenderer(GameState gameState)
    {
        var tokensPerRow = gameState.TokensPerRow;
        var board = gameState.Board.AsSpan();
        var maxTokenValue = byte.MinValue;

        // Scan the board to determine the maximum token value that was played.

        for (var index = 0; index < board.Length; ++index)
        {
            var token = board[index];

            if ((token != GameState.EmptyBoardSpaceValue) && (token > maxTokenValue))
            {
                maxTokenValue = token;
            }
        }

        // Scan the player tokens to determine the maximum token value remaining.

        foreach (var player in Enum.GetValues<PlayerToken>())
        {
            foreach (var token in gameState.GetPlayerTokens(player))
            {
                if (token > maxTokenValue)
                {
                    maxTokenValue = token;
                }
            }
        }

        GameBoardFormatMask = CompositeFormat.Parse(BuildBoardFormattingMask(tokensPerRow));
        BoardWithGuideFormatMask = CompositeFormat.Parse(BuildBoardWithGuideFormattingMask(tokensPerRow));
        GameStateFormatMask = CompositeFormat.Parse(BuildGameStateFormattingMask(tokensPerRow));
        FormattedTokenLookup = BuildFormattedTokenLookup(maxTokenValue);
    }

    /// <summary>
    ///   Renders game state to markdown-formatted ContentBlocks.
    /// </summary>
    ///
    /// <param name="gameState">The game state to render.</param>
    ///
    /// <returns>A ContentBlock containing the rendered game state.</returns>
    ///
    /// <exception cref="ArgumentNullException">Thrown when gameState is null.</exception>
    ///
    public virtual string RenderGameState(GameState gameState)
    {
        ArgumentNullException.ThrowIfNull(gameState, nameof(gameState));

        var allFormatArgs = BuildGameStateFormatArgs(gameState);
        var gameStateContent = string.Format(null, GameStateFormatMask, allFormatArgs);

        // Add game completion messaging if game is over.

        if (gameState.IsGameOver)
        {
            var completionMessage = gameState.Winner switch
            {
                null => """

                ---

                ## 🏁 **Game Over - It's a draw!** 🏁

                ---

                **🎮 Ready for another round?** Just ask me to start a new game!
                """,

                _ => $"""

                ---

                ## 🎉 **Game Over - {gameState.Winner} Player wins!** 🎉

                ---

                **🎮 Ready for another round?** Just ask me to start a new game!
                """
            };

            return $"{gameStateContent}\n{completionMessage}";
        }

        return gameStateContent;
    }

    /// <summary>
    ///   Renders only the game board without additional elements.
    /// </summary>
    ///
    /// <param name="gameState">The game state to render.</param>
    ///
    /// <returns>A markdown-formatted string containing only the game board.</returns>
    ///
    /// <exception cref="ArgumentNullException">Thrown when gameState is null.</exception>
    ///
    public virtual string RenderBoard(GameState gameState)
    {
        ArgumentNullException.ThrowIfNull(gameState, nameof(gameState));

        var tokensPerRow = gameState.TokensPerRow;
        var board = gameState.Board.AsSpan();

        // Prepare the formatted tokens.

        var tokenStrings = new object[tokensPerRow * tokensPerRow];
        var formattedTokens = tokenStrings.AsSpan();

        for (var index = 0; index < formattedTokens.Length; ++index)
        {
            var token = board[index];

            formattedTokens[index] = token == GameState.EmptyBoardSpaceValue
                ? EmptyCell
                : FormattedTokenLookup[token];
        }

        return string.Format(null, GameBoardFormatMask, tokenStrings);
    }

    /// <summary>
    ///   Builds the complete argument array for the full UI composite format operations.
    /// </summary>
    ///
    /// <param name="gameState">The current game state.</param>
    ///
    /// <returns>Array of formatted strings for the complete UI template.</returns>
    ///
    private object[] BuildGameStateFormatArgs(GameState gameState)
    {
        var tokensPerRow = gameState.TokensPerRow;
        var board = gameState.Board.AsSpan();

        // Player info consists of:
        //   - Current turn
        //   - Odd pointer
        //   - Odd tokens
        //   - Even pointer
        //   - Even tokens

        var playerInfoCount = 5;
        var boardTokenCount = tokensPerRow * tokensPerRow;
        var formatArgs = new object[boardTokenCount + playerInfoCount];

        // Build board token arguments.

        for (var index = 0; index < boardTokenCount; ++index)
        {
            var token = board[index];

            formatArgs[index] = token == GameState.EmptyBoardSpaceValue
                ? EmptyCell
                : FormattedTokenLookup[token];
        }

        // Build player info arguments.

        formatArgs[boardTokenCount] = gameState.CurrentTurn.ToString();

        var oddPointer = gameState.CurrentTurn == PlayerToken.Odd ? "→ " : "  ";
        var evenPointer = gameState.CurrentTurn == PlayerToken.Even ? "→ " : "  ";

        formatArgs[boardTokenCount + 1] = oddPointer;
        formatArgs[boardTokenCount + 2] = FormatPlayerTokens(gameState.GetPlayerTokens(PlayerToken.Odd));
        formatArgs[boardTokenCount + 3] = evenPointer;
        formatArgs[boardTokenCount + 4] = FormatPlayerTokens(gameState.GetPlayerTokens(PlayerToken.Even));

        return formatArgs;
    }

    /// <summary>
    ///   Builds the composite format mask for board rendering.
    /// </summary>
    ///
    /// <param name="tokensPerRow">The number of tokens per row.</param>
    ///
    /// <returns>A composite format string for the board layout.</returns>
    ///
    private static string BuildBoardFormattingMask(int tokensPerRow)
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
                        var padding = (CellWidth - 3) / 2;
                        WriteSpaces(buffer, padding, ref position);
                        WritePlaceholder(buffer, placeholderIndex++, ref position);
                        WriteSpaces(buffer, CellWidth - padding - 3, ref position);
                    }
                    else
                    {
                        WriteSpaces(buffer, CellWidth, ref position);
                    }

                    if (cellColumn < tokensPerRow - 1)
                    {
                        buffer[position++] = '|';
                    }
                }
                buffer[position++] = '\n';
            }

            if (boardRow < tokensPerRow - 1)
            {
                WriteSeparatorRow(buffer, tokensPerRow, ref position);
                buffer[position++] = '\n';
            }
        }

        return $"```text\n{new string(buffer.Slice(0, position))}\n```";
    }

    /// <summary>
    ///   Builds the composite format mask for complete UI with board and position guide.
    /// </summary>
    ///
    /// <param name="tokensPerRow">The number of tokens per row.</param>
    ///
    /// <returns>A composite format string for the board with position guide layout.</returns>
    ///
    private static string BuildBoardWithGuideFormattingMask(int tokensPerRow)
    {
        var estimatedSize = CalculateGameStateBufferSize(tokensPerRow);

        var buffer = estimatedSize <= StackAllocThreshold
            ? stackalloc char[estimatedSize]
            : new char[estimatedSize];

        var position = 0;
        var placeholderIndex = 0;
        var tokenCenterRow = CellHeight / 2;
        var boardHeight = (CellHeight * tokensPerRow) + (tokensPerRow - 1);
        var guideHeight = 1 + 1 + tokensPerRow + (tokensPerRow - 1); // Header + blank + content rows + separator rows
        var guideRowIndex = 0; // Track which guide row we're on

        for (var boardRow = 0; boardRow < tokensPerRow; ++boardRow)
        {
            for (var cellRow = 0; cellRow < CellHeight; ++cellRow)
            {
                var absoluteRow = (boardRow * (CellHeight + 1)) + cellRow;

                // Render board cells.

                for (var cellColumn = 0; cellColumn < tokensPerRow; ++cellColumn)
                {
                    if (cellRow == tokenCenterRow)
                    {
                        var padding = (CellWidth - 3) / 2;
                        WriteSpaces(buffer, padding, ref position);
                        WritePlaceholder(buffer, placeholderIndex++, ref position);
                        WriteSpaces(buffer, CellWidth - padding - 3, ref position);
                    }
                    else
                    {
                        WriteSpaces(buffer, CellWidth, ref position);
                    }

                    if (cellColumn < tokensPerRow - 1)
                    {
                        buffer[position++] = '|';
                    }
                }

                // Add position guide content only for rows within the guide height.

                if (guideRowIndex < guideHeight)
                {
                    WritePositionGuideForRow(buffer, guideRowIndex, tokensPerRow, ref position);
                }

                guideRowIndex++;
                buffer[position++] = '\n';
            }

            // Add separator row if not last row.

            if (boardRow < tokensPerRow - 1)
            {
                WriteSeparatorRow(buffer, tokensPerRow, ref position);

                // Add position guide for separator row.

                if (guideRowIndex < guideHeight)
                {
                    WritePositionGuideForRow(buffer, guideRowIndex, tokensPerRow, ref position);
                }

                guideRowIndex++;
                buffer[position++] = '\n';
            }
        }

        return new string(buffer.Slice(0, position));
    }

    /// <summary>
    ///   Builds the composite format mask for the complete UI including headers, code blocks, and player info.
    /// </summary>
    ///
    /// <param name="tokensPerRow">The number of tokens per row.</param>
    ///
    /// <returns>A composite format string for the complete markdown UI template.</returns>
    ///
    private static string BuildGameStateFormattingMask(int tokensPerRow)
    {
        var boardTokenCount = tokensPerRow * tokensPerRow;
        var boardWithGuideFormat = BuildBoardWithGuideFormattingMask(tokensPerRow);

        var playerContentSizeEstimate = 250;
        var builder = new StringBuilder(playerContentSizeEstimate + boardWithGuideFormat.Length);

        // Headers and opening code block.

        builder.AppendLine("## Current Game Board");
        builder.AppendLine();
        builder.AppendLine("```text");

        // Get the board with guide format mask and embed it.

        builder.Append(boardWithGuideFormat);

        // Closing code block and player information template.

        builder.AppendLine("```");
        builder.AppendLine();
        builder.AppendLine("## Player Information");
        builder.AppendLine();

        // Current turn placeholder - first placeholder after board tokens.

        builder.Append("**Current Turn:** {");
        builder.Append(boardTokenCount);
        builder.AppendLine("} Player");
        builder.AppendLine();

        // Player token information with current turn pointers.
        // Odd pointer: {boardTokenCount + 1}, Odd tokens: {boardTokenCount + 2}
        // Even pointer: {boardTokenCount + 3}, Even tokens: {boardTokenCount + 4}

        builder.Append('{');
        builder.Append(boardTokenCount + 1);
        builder.Append("}**Odd Player Tokens:** {");
        builder.Append(boardTokenCount + 2);
        builder.AppendLine("}");
        builder.AppendLine();
        builder.Append('{');
        builder.Append(boardTokenCount + 3);
        builder.Append("}**Even Player Tokens:** {");
        builder.Append(boardTokenCount + 4);
        builder.AppendLine("}");

        return builder.ToString();
    }



    /// <summary>
    ///   Builds a lookup table of pre-formatted token strings to eliminate runtime formatting overhead.
    /// </summary>
    ///
    /// <param name="maxTokenValue">The maximum token value to create formatted strings for.</param>
    ///
    /// <returns>An array of pre-formatted token strings indexed by token value.</returns>
    ///
    private static string[] BuildFormattedTokenLookup(byte maxTokenValue)
    {
        // Build the lookup table, which will contain formatted strings for tokens from 0 to maxTokenValue.
        // Tokens must be exactly 3 characters wide to match the board layout expectations.

        var lookup = new string[maxTokenValue + 1];
        var buffer = (Span<char>)stackalloc char[3];

        for (var index = 0; index <= maxTokenValue; ++index)
        {
            var token = (byte)index;

            if (token < 10)
            {
                // Single digit: center in 3 characters: " X "
                buffer[0] = ' ';
                buffer[1] = (char)('0' + token);
                buffer[2] = ' ';
            }
            else if (token < 100)
            {
                // Two digits: left align in 3 characters: "XY "
                buffer[0] = (char)('0' + token / 10);
                buffer[1] = (char)('0' + token % 10);
                buffer[2] = ' ';
            }
            else
            {
                // Three digits: use all 3 characters: "XYZ"
                buffer[0] = (char)('0' + token / 100);
                buffer[1] = (char)('0' + (token / 10) % 10);
                buffer[2] = (char)('0' + token % 10);
            }

            lookup[index] = new string(buffer);
        }

        return lookup;
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

        // Calculate exact string length to eliminate allocation waste.

        var length = CalculateFormattedTokenStringLength(sortedTokens);

        // Use String.Create for single-allocation string building with direct character writing.

        return string.Create(length, sortedTokens.ToArray(), static (span, tokenArray) =>
        {
            var position = 0;

            // Write opening brace and space.

            span[position++] = '{';
            span[position++] = ' ';

            // Write tokens with comma separators.

            for (var index = 0; index < tokenArray.Length; ++index)
            {
                if (index > 0)
                {
                    span[position++] = ',';
                    span[position++] = ' ';
                }

                // Write token digits directly to avoid ToString() allocation.

                position += WritePlayerTokenDigits(span.Slice(position), tokenArray[index]);
            }

            // Write closing space and brace.

            span[position++] = ' ';
            span[position++] = '}';
        });
    }

    /// <summary>
    ///   Adds a placeholder to the buffer.
    /// </summary>
    ///
    /// <param name="buffer">The character buffer.</param>
    /// <param name="index">The placeholder index.</param>
    /// <param name="position">The current position in the buffer.</param>
    ///
    private static void WritePlaceholder(Span<char> buffer,
                                         int index,
                                         ref int position)
    {
        buffer[position++] = '{';

        if (index >= 10)
        {
            buffer[position++] = (char)('0' + (index / 10));
        }

        buffer[position++] = (char)('0' + (index % 10));
        buffer[position++] = '}';
    }

    /// <summary>
    ///   Adds spaces to the buffer.
    /// </summary>
    ///
    /// <param name="buffer">The character buffer.</param>
    /// <param name="count">The number of spaces to add.</param>
    /// <param name="position">The current position in the buffer.</param>
    ///
    private static void WriteSpaces(Span<char> buffer,
                                    int count,
                                    ref int position)
    {
        for (var index = 0; index < count; ++index)
        {
            buffer[position++] = ' ';
        }
    }

    /// <summary>
    ///   Adds a separator row to the buffer for markdown table formatting.
    /// </summary>
    ///
    /// <param name="buffer">The character buffer.</param>
    /// <param name="tokensPerRow">The number of tokens per row.</param>
    /// <param name="position">The current position in the buffer.</param>
    ///
    private static void WriteSeparatorRow(Span<char> buffer,
                                          int tokensPerRow,
                                          ref int position)
    {
        for (var col = 0; col < tokensPerRow; ++col)
        {
            for (var cellIndex = 0; cellIndex < CellWidth; ++cellIndex)
            {
                buffer[position++] = '-';
            }

            if (col < tokensPerRow - 1)
            {
                buffer[position++] = '+';
            }
        }
    }

    /// <summary>
    ///   Adds position guide content for a specific row.
    /// </summary>
    ///
    /// <param name="buffer">The character buffer.</param>
    /// <param name="row">The row index in the position guide.</param>
    /// <param name="tokensPerRow">The number of tokens per row.</param>
    /// <param name="position">The current position in the buffer.</param>
    ///
    private static void WritePositionGuideForRow(Span<char> buffer,
                                                 int row,
                                                 int tokensPerRow,
                                                 ref int position)
    {
        if (row == 0)
        {
            // Write "Position Guide:" header.

            WriteSpaces(buffer, PositionGuideSpacing, ref position);
            WriteString(buffer, "Position Guide:", ref position);
        }
        else if (row == 1)
        {
            // Write blank line after header.

            WriteSpaces(buffer, PositionGuideSpacing, ref position);
        }
        else if ((row - 2) % 2 == 0)
        {
            // Write content row (rows 2, 4, 6, etc.).

            var contentRowIndex = (row - 2) / 2;
            WriteSpaces(buffer, PositionGuideSpacing, ref position);

            for (var column = 0; column < tokensPerRow; ++column)
            {
                var positionNumber = (contentRowIndex * tokensPerRow) + column + 1;
                buffer[position++] = (char)('0' + positionNumber);

                if (column < tokensPerRow - 1)
                {
                    buffer[position++] = ' ';
                    buffer[position++] = '|';
                    buffer[position++] = ' ';
                }
            }
        }
        else
        {
            // Write separator row (rows 3, 5, etc.).

            WriteSpaces(buffer, PositionGuideSpacing, ref position);
            WritePositionGuideSeparator(buffer, tokensPerRow, ref position);
        }
    }

    /// <summary>
    ///   Adds position guide separator line to the buffer.
    /// </summary>
    ///
    /// <param name="buffer">The character buffer.</param>
    /// <param name="tokensPerRow">The number of tokens per row.</param>
    /// <param name="position">The current position in the buffer.</param>
    ///
    private static void WritePositionGuideSeparator(Span<char> buffer,
                                                    int tokensPerRow,
                                                    ref int position)
    {
        for (var column = 0; column < tokensPerRow; ++column)
        {
            buffer[position++] = '-';

            if (column < tokensPerRow - 1)
            {
                buffer[position++] = ' ';
                buffer[position++] = '+';
                buffer[position++] = ' ';
            }
        }
    }

    /// <summary>
    ///   Writes the digits of a token value directly to a character span.
    /// </summary>
    ///
    /// <param name="destination">The span to write the token digits to.</param>
    /// <param name="token">The token value to write.</param>
    ///
    /// <returns>The number of characters written to the destination span.</returns>
    ///
    private static int WritePlayerTokenDigits(Span<char> destination,
                                              byte token)
    {
        if (token < 10)
        {
            destination[0] = (char)('0' + token);
            return 1;
        }
        else if (token < 100)
        {
            destination[0] = (char)('0' + token / 10);
            destination[1] = (char)('0' + token % 10);
            return 2;
        }
        else
        {
            destination[0] = (char)('0' + token / 100);
            destination[1] = (char)('0' + (token / 10) % 10);
            destination[2] = (char)('0' + token % 10);
            return 3;
        }
    }

    /// <summary>
    ///   Adds a string to the buffer.
    /// </summary>
    ///
    /// <param name="buffer">The character buffer.</param>
    /// <param name="text">The text to add.</param>
    /// <param name="position">The current position in the buffer.</param>
    ///
    private static void WriteString(Span<char> buffer,
                                    string text,
                                    ref int position)
    {
        for (var index = 0; index < text.Length; ++index)
        {
            buffer[position++] = text[index];
        }
    }

    /// <summary>
    ///   Calculates the estimated buffer size needed for board formatting.
    /// </summary>
    ///
    /// <param name="tokensPerRow">The number of tokens per row.</param>
    /// <param name="cellWidth">The width of each cell.</param>
    /// <param name="cellHeight">The height of each cell.</param>
    /// <param name="placeholderLength">The length of placeholder strings.</param>
    ///
    /// <returns>The estimated buffer size in characters.</returns>
    ///
    private static int CalculateStringBufferSize(int tokensPerRow,
                                                 int cellWidth,
                                                 int cellHeight,
                                                 int placeholderLength)
    {
        var cellsPerRow = tokensPerRow;
        var separatorsPerRow = tokensPerRow - 1;
        var rowWidth = (cellsPerRow * cellWidth) + separatorsPerRow;
        var boardRows = tokensPerRow * cellHeight;
        var separatorRows = tokensPerRow - 1;
        var totalRows = boardRows + separatorRows;
        var newlineCount = totalRows;

        return (rowWidth * totalRows) + newlineCount + (placeholderLength * tokensPerRow * tokensPerRow);
    }

    /// <summary>
    ///   Calculates the estimated buffer size needed for complete UI formatting.
    /// </summary>
    ///
    /// <param name="tokensPerRow">The number of tokens per row.</param>
    ///
    /// <returns>The estimated buffer size in characters.</returns>
    ///
    private static int CalculateGameStateBufferSize(int tokensPerRow)
    {
        var boardSize = CalculateStringBufferSize(tokensPerRow, CellWidth, CellHeight, 3);
        var spacingSize = PositionGuideSpacing * (tokensPerRow * CellHeight + tokensPerRow - 1);

        return boardSize + spacingSize + 100;
    }

    /// <summary>
    ///   Calculates the exact length needed for the formatted token string.
    /// </summary>
    ///
    /// <param name="sortedTokens">The sorted token values to format.</param>
    ///
    /// <returns>The total character count needed for the formatted string.</returns>
    ///
    private static int CalculateFormattedTokenStringLength(ReadOnlySpan<byte> sortedTokens)
    {
        // The base length assumes the display pattern:
        // "{ " + " }"

        var length = 4;

        for (var index = 0; index < sortedTokens.Length; ++index)
        {
            // For each token after the first, account for ", " separator.

            if (index > 0)
            {
                length += 2;
            }

            // Calculate digit count for this token, assuming the constraint of
            // valid tokens being in the range of 1-255.

            length += sortedTokens[index] switch
            {
                < 10 => 1,
                < 100 => 2,
                _ => 3
            };
        }

        return length;
    }
}