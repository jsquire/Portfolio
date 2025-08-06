using System.Collections.Concurrent;
using Squire.NumTic.Contracts;

namespace Squire.NumTic.Players;

/// <summary>
///   An automated player implementation based on an algorithmic strategy.
/// </summary>
///
/// <remarks>
///   A basic alpha-beta (min-max) pruning strategy is applied to reduce the number of moves that need to be
///   evaluated while recursively looking forward without impacting the outcome.
///
///   It works as follows:
///
///   - Track the best guaranteed outcome for the desired player (currentBestScoreForDesiredPlayer) and the
///     worst outcome the opponent will accept (currentWorstScoreForOpponent) per call.
///
///   - If the desired player's guaranteed score becomes equal to or better than what the opponent will accept,
///     it is safe to short-circuit because the opponent is expected to choose a different path to avoid this outcome.
/// </remarks>
///
/// <seealso href="https://en.wikipedia.org/wiki/Alpha%E2%80%93beta_pruning"/>
///
public class BotPlayer : IPlayer
{
    /// <summary>The game interface to interact with for player operations.</summary>
    private readonly IGameInterface Interface;

    /// <summary>The options to use for player behavior.</summary>
    private readonly BotPlayerOptions Options;

    /// <summary>The maximum number of moves to look ahead when calculating the best move.</summary>
    private int _maxLookAhead = default;

    /// <summary>
    ///   Initializes a new instance of the <see cref="ConsolePlayer"/> class.
    /// </summary>
    ///
    /// <param name="gameInterface">The game interface to interact with for player operations.</param>
    /// <param name="options">The set of options to use for configuring player behavior.  If not provided a default set is assumed.</param>
    ///
    public BotPlayer(IGameInterface gameInterface,
                     BotPlayerOptions? options = default)
    {
        Interface = gameInterface ?? throw new ArgumentNullException(nameof(gameInterface));
        Options = options?.Clone() ?? BotPlayerOptions.Default;
    }

    /// <summary>
    ///   Initializes a new instance of the <see cref="BotPlayer"/> class.
    /// </summary>
    ///
    /// <param name="gameInterface">The game interface to interact with for player operations.</param>
    /// <param name="options">The set of options to use for configuring player behavior.</param>
    /// <param name="maxLookAhead">The maximum look ahead.</param>
    ///
    /// <remarks>This member is intended only to enable test scenarios.</remarks>
    ///
    internal BotPlayer(IGameInterface gameInterface,
                       BotPlayerOptions options,
                       int maxLookAhead) : this(gameInterface, options) => _maxLookAhead = maxLookAhead;

    /// <summary>
    ///   Plays a turn in the game by prompting the user for their move selection.
    /// </summary>
    ///
    /// <param name="gameState">The current state of the game.</param>
    /// <param name="cancellationToken">A token that can be used to signal a request for cancellation.</param>
    ///
    /// <returns>The move to be made by the bot.</returns>
    ///
    /// <exception cref="ArgumentNullException">Thrown when gameState is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the game is already over or the current player has no remaining tokens.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    ///
    public async Task<Move> PlayTurnAsync(GameState gameState,
                                          CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(gameState, nameof(gameState));

        // If the game is already over, no turn can be played.

        if (gameState.IsGameOver)
        {
            var errorMessage = "The game is already over. No further moves can be made.";

            await Interface.RenderPlayerTextAsync(TextType.Error, errorMessage, cancellationToken);
            throw new InvalidOperationException(errorMessage);
        }

        // Calculate the maximum look-ahead depth based on the difficulty level the first
        // time that a turn is played.

        if (_maxLookAhead == default)
        {
            var maxLookAhead = CalculateMaxLookAhead(gameState, Options.Difficulty);
            Interlocked.CompareExchange(ref _maxLookAhead, maxLookAhead, default);
        }

        // Calculate the best move for the current player.

        var move = await CalculateMoveAsync(gameState, 0, _maxLookAhead, cancellationToken).ConfigureAwait(false);

        // If no valid move was found, this indicates a bug in the game logic.
        // This should never happen because GameState.IsGameOver should be true
        // if no moves are possible (no tokens, no empty spaces, or game won).

        if (move is null)
        {
            var errorMessage = "Internal error: No valid moves could be calculated despite game not being over. This indicates a bug in the game logic.";

            await Interface.RenderPlayerTextAsync(TextType.Error, errorMessage, cancellationToken);
            throw new InvalidOperationException(errorMessage);
        }

        return move.Value;
    }

    /// <summary>
    ///   Calculates which move provides the best outcome for the current player for a
    ///   given state of the game.
    /// </summary>
    ///
    /// <param name="gameState">The current state of the game.</param>
    /// <param name="currentDepth">The current look-ahead (recursive) depth.</param>
    /// <param name="maxLookAhead">The maximum look-ahead depth allowed.</param>
    /// <param name="cancellationToken">A token that can be used to signal a request for cancellation.</param>
    ///
    /// <returns>The move that was made by the user.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    ///
    private async Task<Move?> CalculateMoveAsync(GameState gameState,
                                                 int currentDepth,
                                                 int maxLookAhead,
                                                 CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // No further moves to calculate.

        if (gameState.IsGameOver)
        {
            return null;
        }

        var move = gameState.FindWinningMove(gameState.CurrentTurn);

        // If a winning move was found, return it immediately.

        if (move is not null)
        {
            return move;
        }

        var board = gameState.Board;
        var baseScore = Math.Max(1000, maxLookAhead * 100);

        var firstMove = default(Move?);
        var scoredMoves = new ConcurrentDictionary<Move, int>();
        var tasks = new List<Task<int>>();

        // There were no winning moves, calculate the best move
        // by evaluating the possible moves and their outcomes.

        for (var index = 0; index < board.Length; ++index)
        {
            if (board[index] == GameState.EmptyBoardSpaceValue)
            {
                foreach (var token in gameState.CurrentPlayerTokens)
                {
                    // If this is not the first available move, run scoring in a separate task
                    // to allow for parallel evaluation of moves.

                    if (firstMove is not null)
                    {
                        var scoreTask = Task.Factory.StartNew(state =>
                        {
                            var (currentState, currentIndex, currentToken) = ((GameState, int, byte))state!;
                            var move = new Move(currentState.CurrentTurn, currentIndex, currentToken);

                            return ScoreMove(move, currentState, currentState.CurrentTurn, currentDepth + 1, maxLookAhead, int.MinValue, int.MaxValue, baseScore, scoredMoves, cancellationToken);

                        }, (gameState.CreateCopy(), index, token), cancellationToken);

                        tasks.Add(scoreTask);
                    }
                    else
                    {
                        firstMove = new Move(gameState.CurrentTurn, index, token);
                    }
                }
            }
        }

        // Run the first move synchronously to avoid pushing to the thread pool just to wait for tasks to complete.

        tasks.Add(Task.FromResult(
            ScoreMove(
                firstMove!.Value,
                gameState,
                gameState.CurrentTurn,
                currentDepth + 1,
                maxLookAhead,
                int.MinValue,
                int.MaxValue,
                baseScore,
                scoredMoves,
                cancellationToken)));

        // Choose a random move with the highest score.

        _ = await Task.WhenAll(tasks).ConfigureAwait(false);

        return scoredMoves
            .Where(scoredMove => scoredMove.Key.Player == gameState.CurrentTurn)
            .GroupBy(static scoredMove => scoredMove.Value)
            .MaxBy(static group => group.Key)!
            .OrderBy(static _ => Random.Shared.Next())
            .First()
            .Key;
    }

    /// <summary>
    ///   Evaluates a potential move and calculates its score based on the number of winning,
    ///   losing, or draw outcome with a min/max pruning consideration.
    /// </summary>
    ///
    /// <param name="move">The move to compute a score for.</param>
    /// <param name="gameState">State of the game before the move is applied. This is assumed safe to mutate in-place.</param>
    /// <param name="desiredWinner">The desired winner of the game.</param>
    /// <param name="currentDepth">The current recursive depth of the move.</param>
    /// <param name="maxDepth">The maximum recursive depth allowed when evaluating future moves.</param>
    /// <param name="currentBestScoreForDesiredPlayer">The best score guaranteed for the desired player so far in the current search path.</param>
    /// <param name="currentWorstScoreForOpponent">The worst score the opponent will tolerate before choosing a different path.</param>
    /// <param name="baseScore">The number to use as the basis for scoring before win/lose adjustments are applied.</param>
    /// <param name="scoredMoves">The set of moves that have been scored. This set will be mutated by this call.</param>
    /// <param name="cancellationToken">A token that can be used to signal a request for cancellation.</param>
    ///
    /// <returns>A score for the move, where a higher value indicates a better chance to win.</returns>
    ///
    /// <remarks>
    ///   As part of the scoring evaluation, the <paramref name="gameState"/> will be mutated
    ///   in-place to apply the move. When the call completes, the state will be restored to its
    ///   previous state.
    ///
    ///   This method will also mutate the <paramref name="scoredMoves"/> collection, using it as
    ///   a memoization cache to avoid re-evaluating moves that have already been scored, and adding
    ///   new moves that have been scored during this evaluation.
    ///
    ///   As a result, this method is not fully thread-safe and should only be called with an
    ///   instance of <paramref name="gameState"/> that is not shared with other threads.
    /// </remarks>
    ///
    private static int ScoreMove(Move move,
                                 GameState gameState,
                                 PlayerToken desiredWinner,
                                 int currentDepth,
                                 int maxDepth,
                                 int currentBestScoreForDesiredPlayer,
                                 int currentWorstScoreForOpponent,
                                 int baseScore,
                                 ConcurrentDictionary<Move, int> scoredMoves,
                                 CancellationToken cancellationToken)
    {
        // If the move has already been scored, return the cached score.

        if (scoredMoves.TryGetValue(move, out var cachedScore))
        {
            return cachedScore;
        }

        // No further moves to calculate.

        if (gameState.IsGameOver)
        {
            _ = scoredMoves.TryAdd(move, 0);
            return 0;
        }

        // Apply the move to see what the resulting position looks like.

        gameState.ApplyMove(move);

        // Determine which player has the turn after this move.  If it is the desired player's turn, they want to maximize
        // their score; if it is the opponent's turn, they will try to minimize the desired player's score.

        var isDesiredPlayersTurn = (gameState.CurrentTurn == desiredWinner);
        var bestScoreFromThisPosition = isDesiredPlayersTurn ? int.MinValue : int.MaxValue;

        try
        {
            // Check if this move immediately creates a winning opportunity for someone.

            var winningNext = gameState.FindWinningMove(gameState.CurrentTurn);

            if (winningNext is not null)
            {
                // A winning move for the desired player is more appealing the earlier it can
                // be made.  Likewise, the earlier an opponent wins, the more important it is
                // to avoid that branch.  Adjust the score based on the current depth in the
                // search tree to capture that.

                var score = winningNext.Value.Player == desiredWinner
                    ? baseScore + (maxDepth - currentDepth)
                    : -(baseScore - (maxDepth - currentDepth));

                _ = scoredMoves.TryAdd(move, score);
                return score;
            }

            // If the maximum depth has been reached, no meaningful score can be calculated.

            if (currentDepth >= maxDepth)
            {
                _ = scoredMoves.TryAdd(move, 0);
                return 0;
            }

            // Attempt to honor cancellation before performing recursive evaluations.

            cancellationToken.ThrowIfCancellationRequested();

            // Because the game state is mutable, a copy of the current player's tokens must be made
            // to avoid the loop iteration being impacted by state changes made by the recursive calls
            // when evaluating moves.

            var playerTokens = (Span<byte>)stackalloc byte[gameState.CurrentPlayerTokens.Count];
            var index = 0;

            foreach (var token in gameState.CurrentPlayerTokens)
            {
                playerTokens[index++] = token;
            }

            // Evaluate the possible follow-up moves from this position.

            var board = gameState.Board.AsSpan();
            var shouldContinueSearching = true;

            for (index = 0; index < board.Length; ++index)
            {
                if (board[index] == GameState.EmptyBoardSpaceValue)
                {
                    foreach (var token in playerTokens)
                    {
                        var nextMove = new Move(gameState.CurrentTurn, index, token);

                        int scoreFromNextMove = ScoreMove(
                            nextMove,
                            gameState,
                            desiredWinner,
                            currentDepth + 1,
                            maxDepth,
                            currentBestScoreForDesiredPlayer,
                            currentWorstScoreForOpponent,
                            baseScore,
                            scoredMoves,
                            cancellationToken);

                        // Update the best score assessment, based on the scoring and which player
                        // made the move.

                        if (isDesiredPlayersTurn)
                        {
                            // The desired winner made the move; track the highest known score.

                            bestScoreFromThisPosition = Math.Max(bestScoreFromThisPosition, scoreFromNextMove);
                            currentBestScoreForDesiredPlayer = Math.Max(currentBestScoreForDesiredPlayer, bestScoreFromThisPosition);
                        }
                        else
                        {
                            // The opponent made the move; track the worst known score for the desired player, as this will be
                            // the opponent's objective.

                            bestScoreFromThisPosition = Math.Min(bestScoreFromThisPosition, scoreFromNextMove);
                            currentWorstScoreForOpponent = Math.Min(currentWorstScoreForOpponent, bestScoreFromThisPosition);
                        }

                        // Pruning decision: Does continuing to evaluate turns in this state impact the outcome?

                        // If the desired player's guaranteed score >= what the opponent would accept,  then it is
                        // assumed that the opponent will have made a blocking move to avoid this state and it is
                        // highly unlikely that this particular game state would occur. As a result, there is no
                        // need to continue evaluating further moves from this position.

                        if (currentBestScoreForDesiredPlayer >= currentWorstScoreForOpponent)
                        {
                            shouldContinueSearching = false;
                            break;
                        }
                    }
                }

                // If the pruning condition was met, there's no need to continue
                // evaluating further moves.

                if (!shouldContinueSearching)
                {
                    break;
                }
            }
        }
        finally
        {
            // Restore the game state to its previous state after evaluating the move.

            gameState.UndoMove(move);
        }

        _ = scoredMoves.TryAdd(move, bestScoreFromThisPosition);
        return bestScoreFromThisPosition;
    }

    /// <summary>
    ///   Calculates the maximum look ahead value based on the current game state.
    /// </summary>
    ///
    /// <param name="gameState">The game state to consider.</param>
    /// <param name="difficulty">The desired difficulty level of the bot player.</param>
    ///
    /// <returns>The maximum number of moves to look ahead for the specificed <paramref name="difficulty"/>.</returns>
    ///
    private static int CalculateMaxLookAhead(GameState gameState,
                                             Difficulty difficulty)
    {
        var totalTokens = gameState.GetPlayerTokens(PlayerToken.Odd).Count + gameState.GetPlayerTokens(PlayerToken.Even).Count;

        // Account for any tokens that may already be on the board.

        for (var index = 0; index < gameState.Board.Length; ++index)
        {
            if (gameState.Board[index] != GameState.EmptyBoardSpaceValue)
            {
                ++totalTokens;
            }
        }

        var maxGameLength = Math.Min(gameState.Board.Length, totalTokens);

        return difficulty switch
        {
            Difficulty.Easy => Math.Min(2, maxGameLength),
            Difficulty.Medium => Math.Min(maxGameLength, Math.Max(3, maxGameLength / 3)),
            Difficulty.Hard => Math.Min(maxGameLength, Math.Max(5, (maxGameLength * 2) / 3)),
            Difficulty.Perfect => maxGameLength,
            _ => throw new ArgumentOutOfRangeException(nameof(difficulty), "Invalid difficulty level specified.")
        };
    }
}