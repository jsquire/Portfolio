namespace Squire.NumTic;

/// <summary>
///   A move that was made in the game.
/// </summary>
///
/// <param name="Player">The player that made the move.</param>
/// <param name="PositionIndex">The index of the game board where the token was placed.</param>
/// <param name="Token">The token that was placed on the game board.</param>
///
public readonly record struct Move(PlayerToken Player, int PositionIndex, byte Token)
{
}