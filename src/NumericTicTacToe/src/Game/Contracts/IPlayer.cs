namespace Squire.NumTic.Game;

/// <summary>
///   Defines the contract for a numeric tic-tac-toe player.
/// </summary>
///
public interface IPlayer
{
    /// <summary>
    ///   Plays a turn in the game based on the current game state.
    /// </summary>
    ///
    /// <param name="gameState">The current state of the game.</param>
    /// <param name="cancellationToken">A token that can be used to signal a request for cancellation.</param>
    ///
    /// <returns>The move that was made.</returns>
    ///
    /// <exception cref="OperationCanceledException">Occurs when the turn was canceled.</exception>
    ///
    Task<Move> PlayTurnAsync(GameState gameState,
                             CancellationToken cancellationToken = default);
}
