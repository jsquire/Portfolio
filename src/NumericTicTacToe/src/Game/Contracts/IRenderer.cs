namespace Squire.NumTic.Game.Contracts;

/// <summary>
///   Defines the contract for a renderer of the numeric tic-tac-toe game,
///   responsible for displaying the game state to the user.
/// </summary>
///
public interface IRenderer
{
    /// <summary>
    ///   Renders the current state of the game.
    /// </summary>
    ///
    /// <param name="gameState">The current state of the game.</param>
    /// <param name="winner">The winner, if the game has completed.</param>
    /// <param name="cancellationToken">A token that can be used to signal a request for cancellation.</param>
    ///
    Task RenderAsync(GameState gameState,
                     PlayerToken? winner,
                     CancellationToken cancellationToken = default);
}
