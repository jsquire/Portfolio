using Squire.NumTic.Contracts;

namespace Squire.NumTic.Mcp;

/// <summary>
///   A minimal IGameInterface implementation for BotPlayer that converts interface
///   operations to exceptions suitable for MCP tool execution.
/// </summary>
///
/// <remarks>
///   This interface is designed specifically for BotPlayer usage in MCP tools where
///   user interaction is not needed and all errors should be thrown as exceptions
///   rather than rendered as text.
/// </remarks>
///
/// <seealso cref="IGameInterface"/>
/// <seealso cref="Players.BotPlayer"/>
///
internal sealed class McpBotPlayerGameInterface : IGameInterface
{
    /// <summary>
    ///   Renders the current state of the game.
    /// </summary>
    ///
    /// <param name="gameState">The current state of the game.</param>
    /// <param name="cancellationToken">A token that can be used to signal a request for cancellation.</param>
    ///
    /// <exception cref="NotSupportedException">Always thrown; BotPlayer should never render.</exception>
    ///
    /// <remarks>
    ///   This is a no-op for BotPlayer usage since game state rendering is handled
    ///   by the MCP tools through separate rendering logic.
    /// </remarks>
    ///
    public Task RenderAsync(GameState gameState, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("BotPlayer should never attempt to render in the MCP tool context.");

    /// <summary>
    ///   Converts player text rendering to appropriate exceptions for MCP tool context.
    /// </summary>
    ///
    /// <param name="type">The type of text to render.</param>
    /// <param name="text">The text to render.</param>
    /// <param name="cancellationToken">A token that can be used to signal a request for cancellation.</param>
    ///
    /// <exception cref="ArgumentNullException">Occurs when text is null.</exception>
    /// <exception cref="InvalidOperationException">Occurs when BotPlayer encounters error conditions.</exception>
    /// <exception cref="NotSupportedException">Occurs when the BotPlayer attempts to render non-error text.</exception>
    /// <exception cref="OperationCanceledException">Occurs when operation is cancelled.</exception>
    ///
    public Task RenderPlayerTextAsync(TextType type,
                                      string text,
                                      CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text, nameof(text));
        cancellationToken.ThrowIfCancellationRequested();

        // Convert BotPlayer error messages to exceptions for MCP tool handling.

        if (type == TextType.Error)
        {
            throw new InvalidOperationException($"BotPlayer error: {text}");
        }

        throw new NotSupportedException("BotPlayer should never render non-error text in the MCP tool context.");
    }

    /// <summary>
    ///   Throws NotSupportedException as BotPlayer never requires user input.
    /// </summary>
    ///
    /// <param name="cancellationToken">A token that can be used to signal a request for cancellation.</param>
    ///
    /// <returns>Unsupported; always throws.</returns>
    ///
    /// <exception cref="NotSupportedException">Always thrown; BotPlayer should never request user input.</exception>
    ///
    public Task<string?> ReadPlayerResponseAsnyc(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("BotPlayer should never request user input in the MCP tool context.");
}
