namespace Squire.NumTic.Players;

/// <summary>
///   The set of options for configuring a <see cref="BotPlayer" />.
/// </summary>
///
public class BotPlayerOptions
{
    /// <summary>The default set of options./summary>
    internal static readonly BotPlayerOptions Default = new();

    /// <summary>
    ///   The difficulty level of the bot player.
    /// </summary>
    ///
    /// <value>The difficulty defaults to <see cref="Difficulty.Perfect"/>, if not specified.</value>
    ///
    public Difficulty Difficulty { get; set; } = Difficulty.Perfect;

    /// <summary>
    ///   Clones this instance.
    /// </summary>
    ///
    /// <returns>A new options instance with the same member values.</returns>
    ///
    internal BotPlayerOptions Clone() =>
        new()
        {
            Difficulty = this.Difficulty
        };
}