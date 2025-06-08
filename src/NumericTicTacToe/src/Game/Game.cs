using Squire.NumTic.Game.Contracts;

namespace Squire.NumTic.Game;

/// <summary>
///   Represents a game of numeric tic-tac-toe.
/// </summary>
///
/// <seealso href="https://github.com/jsquire/Portfolio/tree/main/src/NumericTicTacToe/ReadMe.md#rules"/>
///
public class Game
{
    /// <summary>The players in the game.</summary>
    private readonly IReadOnlyDictionary<PlayerToken, IPlayer> _players;

    /// <summary>The renderer used to display the game state.</summary>
    private readonly IRenderer _renderer;

    /// <summary>The state of the game at this moment in time.  Data will mutate as the game is played.</summary>
    private GameState _state;

    /// <summary>
    ///   Initializes a new instance of the <see cref="Game"/> class.
    /// </summary>
    ///
    /// <param name="oddPlayer">Player to play on odd turns.</param>
    /// <param name="evenPlayer">Player to play on even turns.</param>
    /// <param name="gameRenderer">Renderer to use for displaying the game state.</param>
    ///
    public Game(IPlayer oddPlayer,
                IPlayer evenPlayer,
                IRenderer gameRenderer) : this(evenPlayer, oddPlayer, gameRenderer, GameState.CreateDefault())
    {
    }

    /// <summary>
    ///   Initializes a new instance of the <see cref="Game"/> class.
    /// </summary>
    ///
    /// <param name="oddPlayer">Player to play on odd turns.</param>
    /// <param name="evenPlayer">Player to play on even turns.</param>
    /// <param name="gameRenderer">Renderer to use for displaying the game state.</param>
    /// <param name="state">The state to associate with the game.</param>
    ///
    /// <exception cref="ArgumentNullException">Occurs when the <paramref name="state"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Occurs when the game board of the <paramref name="state"/> is not a square.</exception>
    ///
    public Game(IPlayer oddPlayer,
                IPlayer evenPlayer,
                IRenderer gameRenderer,
                GameState state)
    {
        ArgumentNullException.ThrowIfNull(oddPlayer, nameof(oddPlayer));
        ArgumentNullException.ThrowIfNull(evenPlayer, nameof(evenPlayer));
        ArgumentNullException.ThrowIfNull(gameRenderer, nameof(gameRenderer));
        ArgumentNullException.ThrowIfNull(state, nameof(state));

        _state = state;
        _renderer = gameRenderer;

        _players = new Dictionary<PlayerToken, IPlayer>(2)
        {
            { PlayerToken.Odd, oddPlayer },
            { PlayerToken.Even, evenPlayer }
        };
    }
}
