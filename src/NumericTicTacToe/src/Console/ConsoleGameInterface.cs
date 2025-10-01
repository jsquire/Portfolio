using System.Text;
using Spectre.Console;
using Spectre.Console.Rendering;
using Squire.NumTic.Contracts;

namespace Squire.NumTic.Console;

/// <summary>
///   A console-based game interface for the numeric tic-tac-toe game that displays
///   the game state in an enhanced visual format using modern console rendering.
/// </summary>
///
public class ConsoleGameInterface : IGameInterface
{
    /// <summary>The threshold, in bytes, for using a stack allocated arrays.</summary>
    private const int StackAllocThreshold = 4096;

    /// <summary>The width of each cell, in characters, for the game board.</summary>
    private const int CellWidth = 9;

    /// <summary>The height of each cell, in characters, for the game board.</summary>
    private const int CellHeight = 4;

    /// <summary>The width of each cell, in characters, for the position guide.</summary>
    private const int PositionGuideCellWidth = 3;

    /// <summary>The height of each cell, in characters, for the position guide.</summary>
    private const int PositionGuideCellHeight = 1;

    /// <summary>The character used to represent an empty cell in the game board.</summary>
    private const string EmptyCell = "   ";

    /// <summary>The vertical bar line art used as an internal border for the game board.</summary>
    private static readonly string VerticalBar = $"[{MarkupColor.Border}]│[/]";

    /// <summary>The horizontal bar line art used as an internal border for the game board.</summary>
    private static readonly string HorizontalBar = $"[{MarkupColor.Border}]─[/]";

    /// <summary>The intersection of vertical and horizontal bar line art used as an internal border for the game board.</summary>
    private static readonly string Corner = $"[{MarkupColor.Border}]┼[/]";

    /// <summary>Pre-computed formatted strings for all byte values to eliminate runtime allocation and formatting.</summary>
    private readonly string[] FormattedTokenLookup;

    /// <summary>Pre-parsed composite format for the main game board layout to eliminate runtime parsing overhead.</summary>
    private readonly CompositeFormat GameBoardFormatMask;

    /// <summary>The content for the board position guide.</summary>
    private readonly IRenderable PositionGuideContent;

    /// <summary>The console instance for enhanced rendering.</summary>
    private readonly IAnsiConsole AnsiConsole;

    /// <summary>The prompt pending player text that should await a response.</summary>
    private string? _pendingPlayerPrompt;

    /// <summary>The prompt pending player input validation failures that should await a response.</summary>
    private string? _pendingValidationPrompt;

    /// <summary>
    ///   Initializes a new instance of the <see cref="ConsoleGameInterface"/> class.
    /// </summary>
    ///
    /// <param name="gameState">The state that the current game is based on.</param>
    ///
    public ConsoleGameInterface(GameState gameState) : this(gameState, null)
    {
    }

    /// <summary>
    ///   Initializes a new instance of the <see cref="ConsoleGameInterface"/> class.
    /// </summary>
    ///
    /// <param name="gameState">The state that the current game is based on.</param>
    /// <param name="console">The console instance for enhanced rendering. If <c>null</c>, uses AnsiConsole.Console.</param>
    ///
    public ConsoleGameInterface(GameState gameState,
                                IAnsiConsole? console)
    {
        ArgumentNullException.ThrowIfNull(gameState, nameof(gameState));

        AnsiConsole = console ?? Spectre.Console.AnsiConsole.Console;
        FormattedTokenLookup = BuildFormattedTokenLookup(gameState);
        PositionGuideContent = BuildPositionGuideMarkup(gameState.TokensPerRow);
        GameBoardFormatMask =  CompositeFormat.Parse(BuildBoardFormattingMask(gameState.TokensPerRow));

    }

    /// <summary>
    ///   Renders the current state of the game to the console, clearing the screen first
    ///   and displaying the enhanced board layout with position guide and game information.
    /// </summary>
    ///
    /// <param name="gameState">The current state of the game to render.</param>
    /// <param name="cancellationToken">A token that can be used to signal a request for cancellation.</param>
    ///
    public Task RenderAsync(GameState gameState,
                            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(gameState, nameof(gameState));
        cancellationToken.ThrowIfCancellationRequested();

        AnsiConsole.Clear();
        AnsiConsole.Write(CreateMainLayout(gameState, GameBoardFormatMask, PositionGuideContent, FormattedTokenLookup));

        // If the game is complete, show the final status.

        if (gameState.IsGameOver)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.Write(CreateGameStatusContent(gameState));
            AnsiConsole.WriteLine();
        }

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
    /// <remarks>
    ///   Because Spectre.Console provides a specific experience for user prompts, rather than
    ///   directly displaying prompts and errors, this method will set the tracking state that
    ///   will be rendered when reading the player's response.
    ///
    ///   If an error is provided while a prompt is pending, it is assumed to be a validation
    ///   error and will be displayed when the player is prompted for input.  Otherwise, it will
    ///   be rendered immediately.
    /// </remarks>
    ///
    /// <exception cref="ArgumentException">Occurs when <paramref name="text" /> is <c>null</c> or empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Occurs when <paramref name="type" /> is invalid.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    ///
    public Task RenderPlayerTextAsync(TextType type,
                                      string text,
                                      CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(text, nameof(text));

        switch (type)
        {
            case TextType.Message:
                AnsiConsole.WriteLine($"[{MarkupColor.InformationPrompt}]{text.EscapeMarkup()}[/]");
                break;

            case TextType.Error:
                var errorText = $"[{MarkupColor.Error}]{text.EscapeMarkup()}[/]";

                // If there is a pending prompt, assume the error message is a validation
                // failure related to the prompt and store it for display when prompting.

                if (!string.IsNullOrEmpty(_pendingPlayerPrompt))
                {
                    _pendingValidationPrompt = errorText;
                }
                else
                {
                    AnsiConsole.WriteLine(errorText);
                }

                break;

            case TextType.Prompt:
                _pendingPlayerPrompt = text;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, "Invalid text type specified.");
        };

        return Task.CompletedTask;
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
    public async Task<string?> ReadPlayerResponseAsnyc(CancellationToken cancellationToken = default)
    {
        if (_pendingValidationPrompt is not null)
        {
            AnsiConsole.MarkupLine(_pendingValidationPrompt);
            _pendingValidationPrompt = null;
        }

        return await new TextPrompt<string>(_pendingPlayerPrompt ?? string.Empty)
            .PromptStyle(MarkupColor.InputPrompt)
            .ValidationErrorMessage($"[{MarkupColor.Error}]You must provide an answer.[/]")
            .Validate(input => input?.Trim() is { Length: > 0 })
            .ShowAsync(AnsiConsole, cancellationToken);
    }

    /// <summary>
    ///   Creates the main layout for the game display.
    /// </summary>
    ///
    /// <param name="gameState">The current state of the game.</param>
    /// <param name="boardFormatMask">The composite formatting mask for the main game board layout.</param>
    /// <param name="positionGuideContent">The content for the board position guide.</param>
    /// <param name="formattedTokenLookup">A lookup table containing pre-formatted token strings.</param>
    ///
    /// <returns>An <see cref="IRenderable"/> containing the game information.</returns>
    ///
    private static IRenderable CreateMainLayout(GameState gameState,
                                                CompositeFormat boardFormatMask,
                                                IRenderable positionGuideContent,
                                                string[] formattedTokenLookup) =>
        new Rows(
            CreateHeaderContent(),
            new Grid()
                .AddColumn()
                .AddColumn()
                .AddColumn()
                .AddRow(
                    CreatePlayerInformationContent(gameState),
                    CreateGameContent(gameState, boardFormatMask, formattedTokenLookup),
                    CreatePositionGuidePanel(positionGuideContent)));

    /// <summary>
    ///   Creates the header panel for the game.
    /// </summary>
    ///
    /// <returns>The <see cref="Panel" /> that contains the game header.</returns>
    ///
    private static IRenderable CreateHeaderContent()
    {
        var headerContent = new Markup(
            $"[bold {MarkupColor.HeaderText}]NUMERIC TIC-TAC-TOE[/]")
        .Centered();

        return new Panel(headerContent)
        {
            Border = BoxBorder.Double,
            BorderStyle = new Style(Color.Blue),
            Expand = true,
        };
    }

    /// <summary>
    ///   Creates the content of the game board.
    /// </summary>
    ///
    /// <param name="gameState">The current state of the game.</param>
    /// <param name="boardFormatMask">The composite formatting mask for the main game board layout.</param>
    /// <param name="formattedTokenLookup">A lookup table containing pre-formatted token strings.</param>
    ///
    /// <returns>The <see cref="IRenderable" /> that contains the game board.</returns>
    ///
    private static IRenderable CreateGameContent(GameState gameState,
                                                 CompositeFormat boardFormatMask,
                                                 string[] formattedTokenLookup) =>
        new Align(
            new Panel(CreateGameBoard(gameState, boardFormatMask, formattedTokenLookup))
            {
                Border = BoxBorder.None,
                Padding = new Padding(0, 2, 0, 0),
                Expand = false
            },
            HorizontalAlignment.Center,
            VerticalAlignment.Top);

    /// <summary>
    ///   Creates a position guide panel containing the pre-computed position guide markup.
    /// </summary>
    ///
    /// <param name="positionGuideContent">The content for the board position guide.</param>
    ///
    /// <returns>The <see cref="IRenderable" /> that contains the position guide.</returns>
    ///
    private static IRenderable CreatePositionGuidePanel(IRenderable positionGuideContent) =>
        new Align(
            new Panel(positionGuideContent)
            {
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Color.Blue),
                Header = new PanelHeader(" Position Guide "),
                Padding = new Padding(3, 1, 3, 0),
                Expand = false
            },
            HorizontalAlignment.Right,
            VerticalAlignment.Top);

    /// <summary>
    ///   Creates the tic-tac-toe board display.
    /// </summary>
    ///
    /// <param name="gameState">The current state of the game.</param>
    /// <param name="boardFormatMask">The composite formatting mask for the main game board layout.</param>
    /// <param name="formattedTokenLookup">A lookup table containing pre-formatted token strings.</param>
    ///
    /// <returns>A table representing the game board.</returns>
    ///
    private static IRenderable CreateGameBoard(GameState gameState,
                                               CompositeFormat boardFormatMask,
                                               string[] formattedTokenLookup)
    {
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
                : formattedTokenLookup[token];
        }

        // Single format operation.

        var boardMarkup = new Markup(string.Format(null, boardFormatMask, tokenStrings)).Centered();
        return new Padder(boardMarkup, new Padding(1, 0, 1, 1));
    }

    /// <summary>
    ///   Creates the player information display.
    /// </summary>
    ///
    /// <param name="gameState">The current state of the game.</param>
    ///
    /// <returns>The <see cref="IRenderable" /> that contains player information.</returns>
    ///
    private static IRenderable CreatePlayerInformationContent(GameState gameState)
    {
        var grid = new Grid()
            .AddColumn()
            .AddColumn();

        // Odd player information.

        var oddMarker = gameState.CurrentTurn == PlayerToken.Odd ? $"[{MarkupColor.CurrentPlayerMarker}]>[/] [bold {MarkupColor.CurrentPlayerMarker}]" : "  []";
        var oddLabel = $"{oddMarker}Odd Player:[/]";
        var oddValues = $"[{MarkupColor.PlayerValue}]{FormatPlayerTokens(gameState.GetPlayerTokens(PlayerToken.Odd))}[/]";

        // Even player information.

        var evenMarker = gameState.CurrentTurn == PlayerToken.Even ? $"[{MarkupColor.CurrentPlayerMarker}]>[/] [bold {MarkupColor.CurrentPlayerMarker}]" : "  []";
        var evenLabel = $"{evenMarker}Even Player:[/]";
        var evenValues = $"[{MarkupColor.PlayerValue}]{FormatPlayerTokens(gameState.GetPlayerTokens(PlayerToken.Even))}[/]";

        grid.AddRow(oddLabel, oddValues);
        grid.AddRow(evenLabel, evenValues);

        return new Align(
            new Panel(grid)
            {
                Border = BoxBorder.None,
                Padding = new Padding(1, 8, 0, 0),
                Expand = false
            },
            HorizontalAlignment.Left, VerticalAlignment.Middle);
    }

    /// <summary>
    ///   Creates the status content representing showing current game state.
    /// </summary>
    ///
    /// <param name="gameState">The current state of the game.</param>
    ///
    /// <returns>Markup content for the current game status.</returns>
    ///
    private static IRenderable CreateGameStatusContent(GameState gameState)
    {
        if (!gameState.IsGameOver)
        {
            return Text.Empty;
        }

        var status = new Markup(
            gameState.Winner switch
            {
                null => $"[{MarkupColor.Draw} bold]It's a draw![/]",
                _ => $"[{MarkupColor.Winner} bold]>>> {gameState.Winner} Player wins! <<<[/]"
            })
            .Centered();

        return new Panel(status)
        {
            Border = BoxBorder.Double,
            BorderStyle = new Style(Color.Blue),
            Expand = true,
        };
    }

    /// <summary>
    ///   Builds the string formatting mask for the game board based on the
    ///   board size for the current game.
    /// </summary>
    ///
    /// <param name="tokensPerRow">The number of tokens per row.</param>
    ///
    /// <returns>A string formatting template for the main game board.</returns>
    ///
    private static string BuildBoardFormattingMask(int tokensPerRow)
    {
        // Calculate exact size needed.

        var estimatedSize = CalculateStringBufferSize(tokensPerRow, CellWidth, CellHeight, 3); // 3 for placeholder length

        // Use stackalloc for building if reasonable size.

        var buffer = estimatedSize <= StackAllocThreshold
            ? stackalloc char[estimatedSize]
            : new char[estimatedSize];

        var position = 0;
        var placeholderIndex = 0;
        var tokenCenterRow = CellHeight / 2;

        // Build the template with placeholders.

        for (var boardRow = 0; boardRow < tokensPerRow; ++boardRow)
        {
            // Render cell rows.

            for (var cellRow = 0; cellRow < CellHeight; ++cellRow)
            {
                for (var cellColumn = 0; cellColumn < tokensPerRow; ++cellColumn)
                {
                    if (cellRow == tokenCenterRow)
                    {
                        // Add padding and placeholder.

                        var padding = (CellWidth - 3) / 2;

                        WriteBoardSpaces(buffer, padding, ref position);
                        WriteBoardPlaceholder(buffer, placeholderIndex++, ref position);
                        WriteBoardSpaces(buffer, CellWidth - padding - 3, ref position);
                    }
                    else
                    {
                        WriteBoardSpaces(buffer, CellWidth, ref position);
                    }

                    if (cellColumn < tokensPerRow - 1)
                    {
                        WriteBoardMarkup(buffer, VerticalBar, ref position);
                    }
                }
                WriteBoardNewLine(buffer, ref position);
            }

            // Add separator row if not last row.

            if (boardRow < tokensPerRow - 1)
            {
                WriteBoardSeparatorRow(buffer, tokensPerRow, CellWidth, ref position);
                WriteBoardNewLine(buffer, ref position);
            }
        }

        return new string(buffer.Slice(0, position));
    }

    /// <summary>
    ///   Builds the markup string for the position guide showing numbered positions for the board.
    /// </summary>
    ///
    /// <param name="tokensPerRow">The number of tokens per row/column on the board.</param>
    ///
    /// <returns>A markup string for the position guide.</returns>
    ///
    private static Markup BuildPositionGuideMarkup(int tokensPerRow)
    {
        // Calculate exact size needed.

        var maxPosition = tokensPerRow * tokensPerRow;
        var maxPositionMarkupLength = FormatPosition(maxPosition).Length;
        var bufferSize = CalculateStringBufferSize(tokensPerRow, PositionGuideCellWidth, PositionGuideCellHeight, maxPositionMarkupLength);

        // Use stackalloc for building if reasonable size.

        var buffer = bufferSize <= StackAllocThreshold
            ? stackalloc char[bufferSize]
            : new char[bufferSize];

        var position = 0;
        var tokenCenterRow = PositionGuideCellHeight / 2;

        // Build the position guide directly with position numbers.

        for (var boardRow = 0; boardRow < tokensPerRow; ++boardRow)
        {
            // Render cell rows.

            for (var cellRow = 0; cellRow < PositionGuideCellHeight; ++cellRow)
            {
                for (var cellColumn = 0; cellColumn < tokensPerRow; ++cellColumn)
                {
                    if (cellRow == tokenCenterRow)
                    {
                        // Calculate position number and add it directly.

                        var positionNumber = (boardRow * tokensPerRow) + cellColumn + 1;
                        var positionText = FormatPosition(positionNumber);
                        var padding = (PositionGuideCellWidth - 3) / 2;

                        WriteBoardSpaces(buffer, padding, ref position);
                        WriteBoardMarkup(buffer, positionText, ref position);
                        WriteBoardSpaces(buffer, PositionGuideCellWidth - padding - 3, ref position);
                    }
                    else
                    {
                        WriteBoardSpaces(buffer, PositionGuideCellWidth, ref position);
                    }

                    if (cellColumn < tokensPerRow - 1)
                    {
                        WriteBoardMarkup(buffer, VerticalBar, ref position);
                    }
                }

                WriteBoardNewLine(buffer, ref position);
            }

            // Add separator row if not last row.

            if (boardRow < tokensPerRow - 1)
            {
                WriteBoardSeparatorRow(buffer, tokensPerRow, PositionGuideCellWidth, ref position);
                WriteBoardNewLine(buffer, ref position);
            }
        }

        var result = new string(buffer.Slice(0, position));
        return new Markup(result).Centered();
    }

    /// <summary>
    ///   Creates the lookup table for formatted token strings for the player tokens
    ///   active in the current game.
    /// </summary>
    ///
    /// <param name="gameState">The source of player tokens for the current game.</param>
    ///
    /// <returns>An array of formatted strings indexed by byte value.</returns>
    ///
    private static string[] BuildFormattedTokenLookup(GameState gameState)
    {
        // Determine the maximum token value in use to size the lookup table.

        var maxValue = byte.MinValue;

        foreach (var playerType in Enum.GetValues<PlayerToken>())
        {
            var tokens = gameState.GetPlayerTokens(playerType);

            foreach (var token in tokens)
            {
                if (token > maxValue)
                {
                    maxValue = token;
                }
            }
        }

        // Build the lookup table, which will contain formatted strings for
        // tokens from 0 to maxValue.

        var lookup = new string[maxValue + 1];
        var buffer = (Span<char>)stackalloc char[3];

        for (var index = 0; index <= maxValue; ++index)
        {
            var token = (byte)index;

            var tokenColor = (token % 2 == 0)
                ? MarkupColor.EvenToken
                : MarkupColor.OddToken;

            if (token < 10)
            {
                buffer[0] = ' ';
                buffer[1] = (char)('0' + token);
                buffer[2] = ' ';
            }
            else if (token < 100)
            {
                buffer[0] = ' ';
                buffer[1] = (char)('0' + token / 10);
                buffer[2] = (char)('0' + token % 10);
            }

            lookup[index] = token switch
            {
                < 100 => $"[{tokenColor} {MarkupColor.TokenStyle}]{buffer}[/]",
                _ => $"[{tokenColor} {MarkupColor.TokenStyle}]{token}[/]",
            };

        }

        return lookup;
    }

    /// <summary>
    ///   Formats a board position value with appropriate color markup.
    /// </summary>
    ///
    /// <param name="position">The board position value to format.</param>
    ///
    /// <returns>The formatted token string with color markup.</returns>
    ///
    private static string FormatPosition(int position) => position switch
        {
            < 10 => $"[{MarkupColor.TokenStyle} {MarkupColor.PositionGuide}] {position} [/]",
            < 100 => $"[{MarkupColor.TokenStyle} {MarkupColor.PositionGuide}] {position}[/]",
            _ => $"[{MarkupColor.TokenStyle} {MarkupColor.PositionGuide}]{position}[/]"
        };

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
    private static void WriteBoardPlaceholder(Span<char> buffer,
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
    private static void WriteBoardSpaces(Span<char> buffer,
                                         int count,
                                         ref int position)
    {
        for (var index = 0; index < count; ++index)
        {
            buffer[position++] = ' ';
        }
    }

    /// <summary>
    ///   Adds markup text to the buffer.
    /// </summary>
    ///
    /// <param name="buffer">The character buffer.</param>
    /// <param name="markup">The markup text to add.</param>
    /// <param name="position">The current position in the buffer.</param>
    ///
    private static void WriteBoardMarkup(Span<char> buffer,
                                         string markup,
                                         ref int position)
    {
        markup.AsSpan().CopyTo(buffer.Slice(position));
        position += markup.Length;
    }

    /// <summary>
    ///   Adds a newline to the buffer.
    /// </summary>
    ///
    /// <param name="buffer">The character buffer.</param>
    /// <param name="position">The current position in the buffer.</param>
    ///
    private static void WriteBoardNewLine(Span<char> buffer,
                                          ref int position)
    {
        buffer[position++] = '\n';
    }

    /// <summary>
    ///   Adds a separator row to the buffer.
    /// </summary>
    ///
    /// <param name="buffer">The character buffer.</param>
    /// <param name="tokensPerRow">The number of tokens per row.</param>
    /// <param name="cellWidth">The width of each cell in the separator row.</param>
    /// <param name="position">The current position in the buffer.</param>
    ///
    private static void WriteBoardSeparatorRow(Span<char> buffer,
                                               int tokensPerRow,
                                               int cellWidth,
                                               ref int position)
    {
        for (var cellIndex = 0; cellIndex < tokensPerRow; ++cellIndex)
        {
            // Add the cell separator (multiple bars).

            for (var barIndex = 0; barIndex < cellWidth; ++barIndex)
            {
                WriteBoardMarkup(buffer, HorizontalBar, ref position);
            }

            // Add corner between cells (except after last cell).

            if (cellIndex < tokensPerRow - 1)
            {
                WriteBoardMarkup(buffer, Corner, ref position);
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

    /// <summary>
    ///   Calculates the total buffer size needed for a grid template.
    /// </summary>
    ///
    /// <param name="tokensPerRow">The number of tokens per row/column on the board.</param>
    /// <param name="cellWidth">The width of each cell in characters.</param>
    /// <param name="cellHeight">The height of each cell in characters.</param>
    /// <param name="maxContentLength">The maximum length of content that will be placed in cells. For placeholders, use 3. For dynamic content, calculate the actual maximum.</param>
    ///
    /// <returns>The total buffer size needed.</returns>
    ///
    private static int CalculateStringBufferSize(int tokensPerRow,
                                                 int cellWidth,
                                                 int cellHeight,
                                                 int maxContentLength)
    {
        var cellArea = tokensPerRow * tokensPerRow;

        // Calculate size for all cell content (spaces).

        var cellContent = cellArea * cellWidth * cellHeight;

        // Calculate size for vertical separators between cells.

        var verticalSeparators = (tokensPerRow - 1) * tokensPerRow * cellHeight * VerticalBar.Length;

        // Calculate size for horizontal separator rows.

        var horizontalSeparatorRows = tokensPerRow - 1;
        var horizontalSeparatorContent = horizontalSeparatorRows * ((cellWidth * HorizontalBar.Length * tokensPerRow) + ((tokensPerRow - 1) * Corner.Length));

        // Calculate size for newlines.

        var newLines = (tokensPerRow * cellHeight) + horizontalSeparatorRows;

        // Calculate size for actual content that will be placed in cells.

        var contentMarkup = cellArea * maxContentLength;

        return cellContent + verticalSeparators + horizontalSeparatorContent + newLines + contentMarkup;
    }

    /// <summary>
    ///   Defines color constants used for markup formatting throughout the game interface.
    /// </summary>
    ///
    private static class MarkupColor
    {
        /// <summary>The color used for board borders and separators.</summary>
        public const string HeaderText = "blue";

        /// <summary>The color used for board borders and separators.</summary>
        public const string Border = "blue1";

        /// <summary>The color used for even-numbered tokens.</summary>
        public const string EvenToken = "cyan";

        /// <summary>The color used for odd-numbered tokens.</summary>
        public const string OddToken = "magenta";

        /// <summary>The style applied to token text.</summary>
        public const string TokenStyle = "bold";

        /// <summary>The color used for the current player marker.</summary>
        public const string CurrentPlayerMarker = "green";

        /// <summary>The color used for player token values.</summary>
        public const string PlayerValue = "yellow";

        /// <summary>The color used for winner announcements.</summary>
        public const string Winner = "yellow2";

        /// <summary>The color used for draw game announcements.</summary>
        public const string Draw = "grey62";

        /// <summary>The color used for current turn indicators.</summary>
        public const string CurrentTurn = "white";

        /// <summary>The color used for error messages.</summary>
        public const string Error = "red";

        /// <summary>The color used for position guide elements.</summary>
        public const string PositionGuide = "white";

        /// <summary>The color used for prompting for input.</summary>
        public const string InputPrompt = "white";

        /// <summary>The color used displaying game information.</summary>
        public const string InformationPrompt = "yellow";
    }
}