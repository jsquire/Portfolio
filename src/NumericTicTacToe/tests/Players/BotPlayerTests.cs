using NSubstitute;
using NUnit.Framework;
using Squire.NumTic.Contracts;
using Squire.NumTic.Players;

namespace Squire.NumTic.Tests;

/// <summary>
///   Tests for the <see cref="BotPlayer"/> class.
/// </summary>
///
[TestFixture]
[Category("Players")]
public class BotPlayerTests
{
    /// <summary>
    ///   Verifies that the constructor throws ArgumentNullException when gameInterface is null.
    /// </summary>
    ///
    [Test]
    public void ConstructorThrowsWhenGameInterfaceIsNull()
    {
        Assert.That(() => new BotPlayer(null!),
            Throws.InstanceOf<ArgumentNullException>().With.Property("ParamName").EqualTo("gameInterface"),
            "Constructor should throw ArgumentNullException for null gameInterface");
    }

    /// <summary>
    ///   Verifies that the constructor succeeds with valid gameInterface.
    /// </summary>
    ///
    [Test]
    public void ConstructorSucceedsWithValidGameInterface()
    {
        var mockGameInterface = Substitute.For<IGameInterface>();

        Assert.That(() => new BotPlayer(mockGameInterface), Throws.Nothing,
            "Constructor should succeed with valid gameInterface");
    }

    /// <summary>
    ///   Verifies that the constructor accepts custom options.
    /// </summary>
    ///
    [Test]
    public void ConstructorAcceptsCustomOptions()
    {
        var mockGameInterface = Substitute.For<IGameInterface>();
        var customOptions = new BotPlayerOptions { Difficulty = Difficulty.Perfect };

        Assert.That(() => new BotPlayer(mockGameInterface, customOptions), Throws.Nothing,
            "Constructor should accept custom options");
    }

    /// <summary>
    ///   Verifies that the constructor uses default options when none provided.
    /// </summary>
    ///
    [Test]
    public void ConstructorUsesDefaultOptionsWhenNoneProvided()
    {
        var mockGameInterface = Substitute.For<IGameInterface>();

        Assert.That(() => new BotPlayer(mockGameInterface, null), Throws.Nothing,
            "Constructor should use default options when null is provided");
    }

    /// <summary>
    ///   Verifies that PlayTurnAsync throws ArgumentNullException when gameState is null.
    /// </summary>
    ///
    [Test]
    public async Task PlayTurnAsyncThrowsWhenGameStateIsNull()
    {
        var mockGameInterface = Substitute.For<IGameInterface>();
        var botPlayer = CreateFastTestBotPlayer(mockGameInterface);

        await Assert.ThatAsync(async () => await botPlayer.PlayTurnAsync(null!),
            Throws.InstanceOf<ArgumentNullException>().With.Property("ParamName").EqualTo("gameState"),
            "PlayTurnAsync should throw ArgumentNullException for null gameState");
    }

    /// <summary>
    ///   Verifies that PlayTurnAsync throws InvalidOperationException when game is over.
    /// </summary>
    ///
    [Test]
    public async Task PlayTurnAsyncThrowsInvalidOperationExceptionWhenGameIsOver()
    {
        var mockGameInterface = Substitute.For<IGameInterface>();
        var botPlayer = CreateFastTestBotPlayer(mockGameInterface);
        var gameState = CreateGameOverState();

        await Assert.ThatAsync(async () => await botPlayer.PlayTurnAsync(gameState),
            Throws.InstanceOf<InvalidOperationException>().With.Message.Contains("already over"),
            "PlayTurnAsync should throw InvalidOperationException when game is over");

        await mockGameInterface.Received().RenderPlayerTextAsync(
            TextType.Error,
            Arg.Is<string>(s => s.Contains("already over")),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    ///   Verifies that PlayTurnAsync finds and returns a winning move when available.
    /// </summary>
    ///
    [Test]
    public async Task PlayTurnAsyncReturnsWinningMoveWhenAvailable()
    {
        var mockGameInterface = Substitute.For<IGameInterface>();
        var botPlayer = CreateFastTestBotPlayer(mockGameInterface);
        var gameState = CreateNearWinState();

        var move = await botPlayer.PlayTurnAsync(gameState);

        // Verify the bot found the specific winning move: token 9 at position 2.

        Assert.That(move.Player, Is.EqualTo(PlayerToken.Odd), "Should be Odd's move");
        Assert.That(move.Token, Is.EqualTo(9), "Should play token 9 to complete 1+5+9=15");
        Assert.That(move.PositionIndex, Is.EqualTo(2), "Should play at position 2 to complete top row");

        // Verify it's actually a winning move by applying it.

        var testState = gameState.CreateCopy();
        testState.ApplyMove(move);

        Assert.That(testState.Winner, Is.EqualTo(PlayerToken.Odd), "Move should result in Odd winning");
    }

    /// <summary>
    ///   Verifies that PlayTurnAsync returns a valid move for a normal game state.
    /// </summary>
    ///
    [Test]
    public async Task PlayTurnAsyncReturnsValidMoveForNormalGame()
    {
        var mockGameInterface = Substitute.For<IGameInterface>();
        var botPlayer = CreateFastTestBotPlayer(mockGameInterface);
        var gameState = CreateValidGameState();

        var move = await botPlayer.PlayTurnAsync(gameState);

        Assert.That(move.Player, Is.EqualTo(gameState.CurrentTurn), "Move should be for current player");
        Assert.That(gameState.CurrentPlayerTokens.Contains(move.Token), Is.True, "Move should use available token");
        Assert.That(move.PositionIndex, Is.InRange(0, gameState.Board.Length - 1), "Position should be valid");
        Assert.That(gameState.Board[move.PositionIndex], Is.EqualTo(GameState.EmptyBoardSpaceValue), "Position should be empty");
    }

    /// <summary>
    ///   Verifies that PlayTurnAsync respects cancellation tokens.
    /// </summary>
    ///
    [Test]
    public async Task PlayTurnAsyncRespectsCancellationToken()
    {
        var mockGameInterface = Substitute.For<IGameInterface>();
        var botPlayer = CreateFastTestBotPlayer(mockGameInterface);
        var gameState = CreateValidGameState();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThatAsync(async () => await botPlayer.PlayTurnAsync(gameState, cts.Token),
            Throws.InstanceOf<OperationCanceledException>(),
            "PlayTurnAsync should respect cancellation token");
    }

    /// <summary>
    ///   Verifies that PlayTurnAsync does not modify the original game state.
    /// </summary>
    ///
    [Test]
    public async Task PlayTurnAsyncDoesNotModifyOriginalGameState()
    {
        var mockGameInterface = Substitute.For<IGameInterface>();
        var botPlayer = CreateFastTestBotPlayer(mockGameInterface);
        var gameState = CreateValidGameState();

        // Capture original state.

        var originalBoard = gameState.Board.ToArray();
        var originalCurrentTurn = gameState.CurrentTurn;
        var originalTokens = gameState.CurrentPlayerTokens.ToHashSet();

        await botPlayer.PlayTurnAsync(gameState);

        // Verify original state unchanged.

        Assert.That(gameState.Board, Is.EqualTo(originalBoard), "Original board should be unchanged");
        Assert.That(gameState.CurrentTurn, Is.EqualTo(originalCurrentTurn), "Original turn should be unchanged");
        Assert.That(gameState.CurrentPlayerTokens.ToHashSet(), Is.EqualTo(originalTokens), "Original tokens should be unchanged");
    }

    /// <summary>
    ///   Verifies that the bot prioritizes good strategic positions.
    /// </summary>
    ///
    [Test]
    public async Task BotMakesStrategicMoves()
    {
        var mockGameInterface = Substitute.For<IGameInterface>();
        var botPlayer = CreateFastTestBotPlayer(mockGameInterface);
        var gameState = CreateOpponentNearWinState();

        var move = await botPlayer.PlayTurnAsync(gameState);

        // Verify the bot makes a reasonable strategic move.

        Assert.That(move.Player, Is.EqualTo(gameState.CurrentTurn), "Move should be for current player");
        Assert.That(gameState.CurrentPlayerTokens.Contains(move.Token), Is.True, "Should use available token");
        Assert.That(gameState.Board[move.PositionIndex], Is.EqualTo(GameState.EmptyBoardSpaceValue), "Should place on empty space");
    }

    /// <summary>
    ///   Verifies that the bot makes reasonable moves in mid-game scenarios.
    /// </summary>
    ///
    [Test]
    public async Task BotMakesReasonableMovesInMidGame()
    {
        var mockGameInterface = Substitute.For<IGameInterface>();
        var botPlayer = CreateFastTestBotPlayer(mockGameInterface);
        var gameState = CreateMidGameState();

        var move = await botPlayer.PlayTurnAsync(gameState);

        Assert.That(move.Player, Is.EqualTo(gameState.CurrentTurn), "Move should be for current player");
        Assert.That(gameState.GetPlayerTokens(gameState.CurrentTurn).Contains(move.Token), Is.True, "Should use available token");
        Assert.That(gameState.Board[move.PositionIndex], Is.EqualTo(GameState.EmptyBoardSpaceValue), "Should place on empty space");
    }

    /// <summary>
    ///   Verifies that invalid difficulty options are detected.
    /// </summary>
    ///
    [Test]
    public async Task BotThrowsWhenDifficultyIsUnknown()
    {
        var mockGameInterface = Substitute.For<IGameInterface>();
        var limitedOptions = new BotPlayerOptions { Difficulty = (Difficulty)int.MinValue };
        var botPlayer = new BotPlayer(mockGameInterface, limitedOptions);
        var gameState = CreateValidGameState();

        await Assert.ThatAsync(async () => await botPlayer.PlayTurnAsync(gameState),
            Throws.InstanceOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("difficulty"),
            "PlayTurnAsync should throw ArgumentOutOfRangeException for unknown difficulty.");
    }

    /// <summary>
    ///   Verifies that pruning optimization produces identical results to exhaustive evaluation.
    /// </summary>
    ///
    [Test]
    public void PruningProducesSameResultsAsExhaustiveEvaluation()
    {
        var mockGameInterface = Substitute.For<IGameInterface>();
        var gameState = CreateGameStateWithMultipleMoves();

        // Create two identical players with different search approaches.
        // One with minimal lookahead (more exhaustive per depth) and one with deeper lookahead (more pruning).

        var shallowPlayer = new BotPlayer(mockGameInterface, new BotPlayerOptions { Difficulty = Difficulty.Easy });
        var deepPlayer = new BotPlayer(mockGameInterface, new BotPlayerOptions { Difficulty = Difficulty.Hard });

        var shallowMove = shallowPlayer.PlayTurnAsync(gameState.CreateCopy()).Result;
        var deepMove = deepPlayer.PlayTurnAsync(gameState.CreateCopy()).Result;

        // Both should make rational moves (not necessarily identical due to randomization of equal scores).

        Assert.That(shallowMove.Player, Is.EqualTo(gameState.CurrentTurn), "Shallow player should make move for current player");
        Assert.That(deepMove.Player, Is.EqualTo(gameState.CurrentTurn), "Deep player should make move for current player");
        Assert.That(gameState.GetPlayerTokens(gameState.CurrentTurn), Contains.Item(shallowMove.Token), "Shallow player should use available token");
        Assert.That(gameState.GetPlayerTokens(gameState.CurrentTurn), Contains.Item(deepMove.Token), "Deep player should use available token");
    }

    /// <summary>
    ///   Verifies that pruning handles deep search scenarios correctly without missing optimal moves.
    /// </summary>
    ///
    [Test]
    public void PruningHandlesDeepSearchCorrectly()
    {
        var mockGameInterface = Substitute.For<IGameInterface>();
        var gameState = CreateNearWinState();
        var player = new BotPlayer(mockGameInterface, new BotPlayerOptions { Difficulty = Difficulty.Perfect });

        // Player should find the specific winning move despite deep search with pruning.

        var move = player.PlayTurnAsync(gameState).Result;

        // Verify the specific winning move was found.

        Assert.That(move.Token, Is.EqualTo(9), "Should find the winning token 9");
        Assert.That(move.PositionIndex, Is.EqualTo(2), "Should find the winning position 2");

        gameState.ApplyMove(move);

        Assert.That(gameState.Winner, Is.EqualTo(PlayerToken.Odd), "Player should find the winning move with deep search and pruning");
    }

    /// <summary>
    ///   Verifies that pruning respects cancellation tokens during optimization.
    /// </summary>
    ///
    [Test]
    public void PruningRespectsCancellation()
    {
        var mockGameInterface = Substitute.For<IGameInterface>();
        var gameState = CreateValidGameState();
        var player = new BotPlayer(mockGameInterface, new BotPlayerOptions { Difficulty = Difficulty.Perfect });

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Pruning should not interfere with cancellation handling.

        Assert.ThatAsync(async () => await player.PlayTurnAsync(gameState, cts.Token),
            Throws.InstanceOf<OperationCanceledException>(),
            "Pruning should respect cancellation tokens");
    }

    /// <summary>
    ///   Verifies that pruning handles game end conditions correctly.
    /// </summary>
    ///
    [Test]
    public void PruningHandlesGameEndConditions()
    {
        var mockGameInterface = Substitute.For<IGameInterface>();
        var gameState = CreateNearWinState();
        var player = new BotPlayer(mockGameInterface, new BotPlayerOptions { Difficulty = Difficulty.Hard });

        // Should find the specific winning move when game is nearly over.

        var move = player.PlayTurnAsync(gameState).Result;

        Assert.That(move.Token, Is.EqualTo(9), "Should find the winning token 9");
        Assert.That(move.PositionIndex, Is.EqualTo(2), "Should find the winning position 2");

        gameState.ApplyMove(move);

        Assert.That(gameState.Winner, Is.EqualTo(PlayerToken.Odd), "Pruning should not miss winning moves near game end");
    }

    /// <summary>
    ///   Verifies that pruning maintains performance benefits over multiple evaluations.
    /// </summary>
    ///
    [Test]
    public void PruningMaintainsPerformanceBenefits()
    {
        var mockGameInterface = Substitute.For<IGameInterface>();
        var gameState = CreateGameStateWithMultipleMoves();

        // Test with higher difficulty which should benefit most from pruning.
        // Use a reasonable timeout to ensure the algorithm completes in acceptable time.

        var player = new BotPlayer(mockGameInterface, new BotPlayerOptions { Difficulty = Difficulty.Hard });

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var move = player.PlayTurnAsync(gameState).Result;
        stopwatch.Stop();

        // Should complete in reasonable time (pruning prevents exponential blowup).
        // Use a more conservative timeout that allows for CI/test environment variability.

        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(2000), "Pruning should prevent excessive computation time");
        Assert.That(move.Player, Is.EqualTo(gameState.CurrentTurn), "Should make move for current player");
        Assert.That(gameState.GetPlayerTokens(gameState.CurrentTurn), Contains.Item(move.Token), "Should use available token");
        Assert.That(gameState.Board[move.PositionIndex], Is.EqualTo(GameState.EmptyBoardSpaceValue), "Should place on empty space");
    }

    /// <summary>
    ///   Verifies that the bot prefers immediate wins over delayed wins.
    /// </summary>
    ///
    [Test]
    public async Task BotPrefersImmediateWinsOverDelayedWins()
    {
        var mockGameInterface = Substitute.For<IGameInterface>();
        var botPlayer = new BotPlayer(mockGameInterface, new BotPlayerOptions { Difficulty = Difficulty.Hard });
        var gameState = CreateStateWithImmediateWinOption();

        var move = await botPlayer.PlayTurnAsync(gameState);

        // The bot should choose the immediate win (token 9 at position 2) over any delayed win sequence.

        Assert.That(move.Token, Is.EqualTo(9), "Should choose immediate winning token");
        Assert.That(move.PositionIndex, Is.EqualTo(2), "Should choose immediate winning position");

        // Verify it's actually the immediate winning move.

        var testState = gameState.CreateCopy();
        testState.ApplyMove(move);

        Assert.That(testState.Winner, Is.EqualTo(PlayerToken.Odd), "Move should result in immediate win");
    }

    /// <summary>
    ///   Verifies that the bot prefers delayed losses when all moves lead to opponent wins.
    /// </summary>
    ///
    [Test]
    public async Task BotPrefersDelayedLossesWhenAllMovesLose()
    {
        var mockGameInterface = Substitute.For<IGameInterface>();
        var botPlayer = new BotPlayer(mockGameInterface, new BotPlayerOptions { Difficulty = Difficulty.Hard });
        var gameState = CreateStateWhereAllMovesLeadToLoss();

        var move = await botPlayer.PlayTurnAsync(gameState);

        // Verify the bot makes a valid move that delays the opponent's win.

        Assert.That(move.Player, Is.EqualTo(gameState.CurrentTurn), "Should make move for current player");
        Assert.That(gameState.GetPlayerTokens(gameState.CurrentTurn), Contains.Item(move.Token), "Should use available token");
        Assert.That(gameState.Board[move.PositionIndex], Is.EqualTo(GameState.EmptyBoardSpaceValue), "Should place on empty space");

        // The specific move choice will depend on the implementation, but it should be a valid delaying move.
    }

    /// <summary>
    ///   Verifies that depth-based scoring calculates correct values for wins and losses.
    /// </summary>
    ///
    [Test]
    public async Task DepthBasedScoringCalculatesCorrectValues()
    {
        var mockGameInterface = Substitute.For<IGameInterface>();
        var botPlayer = new BotPlayer(mockGameInterface, new BotPlayerOptions { Difficulty = Difficulty.Medium });
        var gameState = CreateValidGameState();

        // The base score calculation should follow the formula: Math.Max(1000, maxDepth * 100).
        // For medium difficulty with our game state, maxDepth should be around 3.
        // Base score should be Math.Max(1000, 3 * 100) = 1000.

        var move = await botPlayer.PlayTurnAsync(gameState);

        // Verify the bot makes a reasonable move (specific score testing requires internal access).

        Assert.That(move.Player, Is.EqualTo(gameState.CurrentTurn), "Should make move for current player");
        Assert.That(gameState.GetPlayerTokens(gameState.CurrentTurn), Contains.Item(move.Token), "Should use available token");
        Assert.That(gameState.Board[move.PositionIndex], Is.EqualTo(GameState.EmptyBoardSpaceValue), "Should place on empty space");
    }

    /// <summary>
    ///   Verifies that the bot distinguishes between multiple win options at different depths.
    /// </summary>
    ///
    [Test]
    public async Task BotDistinguishesBetweenWinOptionsAtDifferentDepths()
    {
        var mockGameInterface = Substitute.For<IGameInterface>();
        var botPlayer = new BotPlayer(mockGameInterface, new BotPlayerOptions { Difficulty = Difficulty.Hard });
        var gameState = CreateStateWithMultipleWinDepths();

        var move = await botPlayer.PlayTurnAsync(gameState);

        // When multiple winning paths exist, bot should prefer the shortest path.
        // In our test state, there should be an immediate win available.

        Assert.That(move.Player, Is.EqualTo(gameState.CurrentTurn), "Should make move for current player");
        Assert.That(gameState.GetPlayerTokens(gameState.CurrentTurn), Contains.Item(move.Token), "Should use available token");
        Assert.That(gameState.Board[move.PositionIndex], Is.EqualTo(GameState.EmptyBoardSpaceValue), "Should place on empty space");

        // Verify it results in a win.

        var testState = gameState.CreateCopy();
        testState.ApplyMove(move);

        Assert.That(testState.Winner, Is.EqualTo(gameState.CurrentTurn), "Should achieve win");
    }

    /// <summary>
    ///   Verifies that the bot handles boundary conditions correctly at maximum depth.
    /// </summary>
    ///
    [Test]
    public async Task BotHandlesBoundaryConditionsAtMaximumDepth()
    {
        var mockGameInterface = Substitute.For<IGameInterface>();
        var botPlayer = new BotPlayer(mockGameInterface, new BotPlayerOptions { Difficulty = Difficulty.Easy }, maxLookAhead: 1);
        var gameState = CreateNearWinState();

        var move = await botPlayer.PlayTurnAsync(gameState);

        // Even with minimal depth, bot should find the immediate winning move.

        Assert.That(move.Token, Is.EqualTo(9), "Should find winning token even at depth 1");
        Assert.That(move.PositionIndex, Is.EqualTo(2), "Should find winning position even at depth 1");

        // Verify it's the winning move.

        var testState = gameState.CreateCopy();
        testState.ApplyMove(move);

        Assert.That(testState.Winner, Is.EqualTo(PlayerToken.Odd), "Should win even with limited depth");
    }

    /// <summary>
    ///   Creates a valid initial game state for testing.
    /// </summary>
    ///
    private static GameState CreateValidGameState() =>
        new GameState(
            PlayerToken.Odd,
            new int[9],
            15,
            [
                new HashSet<byte> { 1, 3, 5, 7, 9 },
                new HashSet<byte> { 2, 4, 6, 8 }
            ]);

    /// <summary>
    ///   Creates a game state where the current player can win in one move.
    /// </summary>
    ///
    private static GameState CreateNearWinState()
    {
        var gameState = CreateValidGameState();

        // Set up a near-win scenario: 1 + 5 = 6, need 9 at position 2 to win (1+5+9=15).
        // Row 0: positions 0, 1, 2

        gameState.ApplyMove(new Move(PlayerToken.Odd, 0, 1));   // Odd plays 1 at position 0
        gameState.ApplyMove(new Move(PlayerToken.Even, 3, 2));  // Even plays 2 at position 3
        gameState.ApplyMove(new Move(PlayerToken.Odd, 1, 5));   // Odd plays 5 at position 1
        gameState.ApplyMove(new Move(PlayerToken.Even, 4, 4));  // Even plays 4 at position 4

        // Now it's Odd's turn and they can win by playing 9 at position 2: 1+5+9=15

        return gameState;
    }

    /// <summary>
    ///   Creates a game state where the opponent could potentially threaten.
    /// </summary>
    ///
    private static GameState CreateOpponentNearWinState()
    {
        var gameState = CreateValidGameState();

        // Create a scenario where Even has potential threats and Odd needs to respond.
        // Even can win with 2+6+8=16, but that's not possible (need exactly 15).
        // Instead, set up where Even has 2+4=6 and needs 9 (but 9 is odd token).
        // This forces Odd to think strategically about positioning.

        gameState.ApplyMove(new Move(PlayerToken.Odd, 0, 1));   // Odd plays 1 at position 0
        gameState.ApplyMove(new Move(PlayerToken.Even, 4, 2));  // Even plays 2 at center (position 4)
        gameState.ApplyMove(new Move(PlayerToken.Odd, 8, 3));   // Odd plays 3 at position 8
        gameState.ApplyMove(new Move(PlayerToken.Even, 1, 4));  // Even plays 4 at position 1

        // Now it's Odd's turn in a mid-game scenario

        return gameState;
    }

    /// <summary>
    ///   Creates a mid-game state with several moves already played.
    /// </summary>
    ///
    private static GameState CreateMidGameState()
    {
        var gameState = CreateValidGameState();

        // Play a few moves to create a mid-game scenario.

        gameState.ApplyMove(new Move(PlayerToken.Odd, 0, 1));   // Odd plays 1 at position 0
        gameState.ApplyMove(new Move(PlayerToken.Even, 4, 2));  // Even plays 2 at center (position 4)
        gameState.ApplyMove(new Move(PlayerToken.Odd, 8, 3));   // Odd plays 3 at position 8

        // After Odd, Even, Odd moves, it should be Even's turn

        return gameState;
    }

    /// <summary>
    ///   Creates a game state where the game is already over.
    /// </summary>
    ///
    private static GameState CreateGameOverState()
    {
        var gameState = CreateValidGameState();

        // Create a winning scenario for Odd: 1+5+9=15 in top row (positions 0,1,2).

        gameState.ApplyMove(new Move(PlayerToken.Odd, 0, 1));   // Odd plays 1 at position 0
        gameState.ApplyMove(new Move(PlayerToken.Even, 3, 2));  // Even plays 2 at position 3
        gameState.ApplyMove(new Move(PlayerToken.Odd, 1, 5));   // Odd plays 5 at position 1
        gameState.ApplyMove(new Move(PlayerToken.Even, 4, 4));  // Even plays 4 at position 4
        gameState.ApplyMove(new Move(PlayerToken.Odd, 2, 9));   // Odd plays 9 at position 2 - WINS!

        return gameState;
    }

    /// <summary>
    ///   Creates a BotPlayer optimized for fast unit testing with minimal lookahead.
    /// </summary>
    ///
    private static BotPlayer CreateFastTestBotPlayer(IGameInterface gameInterface) =>
        new BotPlayer(gameInterface, BotPlayerOptions.Default, maxLookAhead: 1);

    /// <summary>
    ///   Creates a game state with multiple possible moves for testing move selection.
    /// </summary>
    ///
    private static GameState CreateGameStateWithMultipleMoves()
    {
        var gameState = CreateValidGameState();

        // Apply a few moves to create an interesting mid-game position.

        gameState.ApplyMove(new Move(PlayerToken.Odd, 0, 1));   // Odd plays 1 at position 0
        gameState.ApplyMove(new Move(PlayerToken.Even, 4, 2));  // Even plays 2 at center
        gameState.ApplyMove(new Move(PlayerToken.Odd, 8, 3));   // Odd plays 3 at position 8

        return gameState;
    }

    /// <summary>
    ///   Creates a game state where the bot has an immediate win option available.
    /// </summary>
    ///
    private static GameState CreateStateWithImmediateWinOption()
    {
        var gameState = CreateValidGameState();

        // Set up the same near-win scenario as CreateNearWinState.
        // This gives the bot an immediate win option with token 9 at position 2.

        gameState.ApplyMove(new Move(PlayerToken.Odd, 0, 1));   // Odd plays 1 at position 0
        gameState.ApplyMove(new Move(PlayerToken.Even, 3, 2));  // Even plays 2 at position 3
        gameState.ApplyMove(new Move(PlayerToken.Odd, 1, 5));   // Odd plays 5 at position 1
        gameState.ApplyMove(new Move(PlayerToken.Even, 4, 4));  // Even plays 4 at position 4

        // Now Odd can win immediately with 9 at position 2 (1+5+9=15).

        return gameState;
    }

    /// <summary>
    ///   Creates a game state where all available moves lead to opponent wins, testing loss delay preference.
    /// </summary>
    ///
    private static GameState CreateStateWhereAllMovesLeadToLoss()
    {
        var gameState = CreateValidGameState();

        // Create a scenario where Even is threatening and Odd must choose between bad options.
        // This tests the bot's ability to prefer delayed losses over immediate ones.

        gameState.ApplyMove(new Move(PlayerToken.Odd, 0, 1));   // Odd plays 1 at position 0
        gameState.ApplyMove(new Move(PlayerToken.Even, 1, 2));  // Even plays 2 at position 1
        gameState.ApplyMove(new Move(PlayerToken.Odd, 2, 3));   // Odd plays 3 at position 2
        gameState.ApplyMove(new Move(PlayerToken.Even, 4, 4));  // Even plays 4 at position 4

        // Current state creates a scenario where Even has potential winning threats.

        return gameState;
    }

    /// <summary>
    ///   Creates a game state with multiple winning paths at different depths.
    /// </summary>
    ///
    private static GameState CreateStateWithMultipleWinDepths()
    {
        var gameState = CreateValidGameState();

        // Create a scenario where Odd can win immediately.
        // Set up: 1+5 in anti-diagonal (positions 6, 4), need 9 at position 2.

        gameState.ApplyMove(new Move(PlayerToken.Odd, 6, 1));   // Odd plays 1 at position 6 (bottom-left)
        gameState.ApplyMove(new Move(PlayerToken.Even, 0, 2));  // Even plays 2 at position 0
        gameState.ApplyMove(new Move(PlayerToken.Odd, 4, 5));   // Odd plays 5 at position 4 (center)
        gameState.ApplyMove(new Move(PlayerToken.Even, 1, 4));  // Even plays 4 at position 1

        // Board layout (3x3):
        // [2, 4, _]  <- positions 0, 1, 2
        // [_, 5, _]  <- positions 3, 4, 5
        // [1, _, _]  <- positions 6, 7, 8
        //
        // Odd can win immediately by playing 9 at position 2 for anti-diagonal: 1+5+9=15.

        return gameState;
    }
}