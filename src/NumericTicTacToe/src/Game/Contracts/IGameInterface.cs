namespace Squire.NumTic.Game.Contracts;

/// <summary>
///   Defines the contract for an interface of the numeric tic-tac-toe game,
///   responsible for displaying the game state to the user and facilitating
///   interactions.
/// </summary>
///
public interface IGameInterface
{
    /// <summary>
    ///   Renders the current state of the game.
    /// </summary>
    ///
    /// <param name="gameState">The current state of the game.</param>
    /// <param name="cancellationToken">A token that can be used to signal a request for cancellation.</param>
    ///
    Task RenderAsync(GameState gameState,
                     CancellationToken cancellationToken = default);

    /// <summary>
    ///   Renders text associated with the player, such as messages or prompts.
    /// </summary>
    ///
    /// <param name="type">The type of text to render.</param>
    /// <param name="text">The text to render.</param>
    /// <param name="cancellationToken">A token that can be used to signal a request for cancellation.</param>
    ///
    Task RenderPlayerTextAsync(TextType type,
                               string text,
                               CancellationToken cancellationToken = default);

    /// <summary>
    ///   Reads a response from the player asynchronously, allowing them to
    ///   provide input or make selections during the game.
    /// </summary>
    ///
    /// <param name="cancellationToken">A token that can be used to signal a request for cancellation.</param>
    ///
    /// <returns>The player's response that was read, in <see cref="string"/> form.</returns>
    ///
    Task<string?> ReadPlayerResponseAsnyc(CancellationToken cancellationToken = default);
}
