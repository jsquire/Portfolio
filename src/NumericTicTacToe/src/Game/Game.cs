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
    private readonly IGameInterface _interface;

    /// <summary>The state of the game at this moment in time.  Data will mutate as the game is played.</summary>
    private GameState _state;

    /// <summary>
    ///   Initializes a new instance of the <see cref="Game"/> class.
    /// </summary>
    ///
    /// <param name="oddPlayer">Player to play on odd turns.</param>
    /// <param name="evenPlayer">Player to play on even turns.</param>
    /// <param name="gameInterface">Renderer to use for displaying the game state.</param>
    ///
    public Game(IPlayer oddPlayer,
                IPlayer evenPlayer,
                IGameInterface gameInterface) : this(evenPlayer, oddPlayer, gameInterface, GameState.CreateDefault())
    {
    }

    /// <summary>
    ///   Initializes a new instance of the <see cref="Game"/> class.
    /// </summary>
    ///
    /// <param name="oddPlayer">Player to play on odd turns.</param>
    /// <param name="evenPlayer">Player to play on even turns.</param>
    /// <param name="gameInterface">Renderer to use for displaying the game state.</param>
    /// <param name="state">The state to associate with the game.</param>
    ///
    /// <exception cref="ArgumentNullException">Occurs when the <paramref name="state"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Occurs when the game board of the <paramref name="state"/> is not a square.</exception>
    ///
    public Game(IPlayer oddPlayer,
                IPlayer evenPlayer,
                IGameInterface gameInterface,
                GameState state)
    {
        ArgumentNullException.ThrowIfNull(oddPlayer, nameof(oddPlayer));
        ArgumentNullException.ThrowIfNull(evenPlayer, nameof(evenPlayer));
        ArgumentNullException.ThrowIfNull(gameInterface, nameof(gameInterface));
        ArgumentNullException.ThrowIfNull(state, nameof(state));

        _state = state;
        _interface = gameInterface;

        _players = new Dictionary<PlayerToken, IPlayer>(2)
        {
            { PlayerToken.Odd, oddPlayer },
            { PlayerToken.Even, evenPlayer }
        };
    }


    /// <summary>
    ///   Plays the game until a winner is determined or a cancellation is requested.
    /// </summary>
    ///
    /// <param name="cancellationToken">A token that can be used to signal a request for cancellation.</param>
    ///
    public async Task PlayAsync(CancellationToken cancellationToken = default)
    {

        var move = default(Move);

        // Render the initial state of the game.

        await _interface.RenderAsync(_state, cancellationToken);

        // Loop until the game has a winner or a cancellation is requested.

        while ((!_state.IsGameOver) && (!cancellationToken.IsCancellationRequested))
        {
            try
            {
                move = await _players[_state.CurrentTurn].PlayTurnAsync(_state, cancellationToken);
                _state.ApplyMove(move);

                await _interface.RenderAsync(_state, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // If the operation was canceled, exit the loop.

                break;
            }
        }
    }

    /// <summary>
    ///   Resets the game, restarting from the beginning of the game.
    /// </summary>
    ///
    public void Reset() => Reset(GameState.CreateDefault());

    /// <summary>
    ///   Resets the game to the specified state.
    /// </summary>
    ///
    /// <param name="state">The state to reset the game to.</param>
    ///
    /// <exception cref="System.ArgumentNullException">state</exception>
    ///
    public void Reset(GameState state)
    {
        ArgumentNullException.ThrowIfNull(state, nameof(state));
        _state = state;
    }
}
