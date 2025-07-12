namespace Squire.NumTic;
/// <summary>
///  The difficulty levels available for the game.
/// </summary>
///
public enum Difficulty
{
    /// <summary>Automated players use less strategy and make random moves more often.</summary>
    Easy,

    /// <summary>Automated players use a moderate level of strategy with occasional random moves.</summary>
    Medium,

    /// <summary>Automated players use advanced strategies and rarely make random moves.</summary>
    Hard,

    /// <summary>Automated players stick rigorously to strategy and do not make mistakes.</summary>
    Perfect
}
