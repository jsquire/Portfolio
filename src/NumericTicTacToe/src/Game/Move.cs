namespace Squire.NumTic.Game;

/// <summary>
///   A move that was made in the game.
/// </summary>
///
/// <param name="Player">The player that made the move.</param>
/// <param name="PositionIndex">The index of the game board where the token was placed.</param>
/// <param name="Token">The token that was placed on the game board.</param>
/// <param name="Winner">The winning player after the move was made; <c>null</c> if there was no winner resulting from the move.</param>
///
public record Move(PlayerToken Player, int PositionIndex, byte Token, PlayerToken? Winner)
{
}
