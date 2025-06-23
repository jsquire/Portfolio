using NUnit.Framework;
using Squire.NumTic.Game;

namespace Squire.NumTic.Tests;

/// <summary>
///   Tests for the <see cref="GameState"/> class.
/// </summary>
///
[TestFixture]
[Category("Game")]
public class GameStateTests
{
    /// <summary>
    ///   Verifies that GetCurrentPlayerTokens returns the correct token set for the current player.
    /// </summary>
    ///
    /// <param name="currentTurn">The current turn to test.</param>
    /// <param name="expectedTokenCount">The expected number of tokens for that player.</param>
    ///
    [Test]
    [TestCase(PlayerToken.Odd, 5)]
    [TestCase(PlayerToken.Even, 4)]
    public void GetCurrentPlayerTokensReturnsCorrectTokenSet(PlayerToken currentTurn, int expectedTokenCount)
    {
        var gameState = GameState.CreateDefault() with { CurrentTurn = currentTurn };
        var tokens = gameState.CurrentPlayerTokens;

        Assert.That(tokens, Is.Not.Null, $"Token set for {currentTurn} should not be null");
        Assert.That(tokens.Count, Is.EqualTo(expectedTokenCount), $"Token set for {currentTurn} should contain {expectedTokenCount} tokens");
    }

    /// <summary>
    ///   Verifies that CurrentPlayerTokens returns consistent results.
    /// </summary>
    ///
    [Test]
    public void CurrentPlayerTokensReturnsConsistentResults()
    {
        var gameState = GameState.CreateDefault();
        var tokensFirst = gameState.CurrentPlayerTokens;
        var tokensSecond = gameState.CurrentPlayerTokens;

        Assert.That(tokensFirst, Is.SameAs(tokensSecond), "CurrentPlayerTokens should return the same reference on multiple calls");
    }

    /// <summary>
    ///   Verifies that GetPlayerTokens returns the correct token set for the specified player.
    /// </summary>
    ///
    /// <param name="player">The player to get tokens for.</param>
    /// <param name="expectedTokenCount">The expected number of tokens for that player.</param>
    ///
    [Test]
    [TestCase(PlayerToken.Odd, 5)]
    [TestCase(PlayerToken.Even, 4)]
    public void GetPlayerTokensReturnsCorrectTokenSet(PlayerToken player, int expectedTokenCount)
    {
        var gameState = GameState.CreateDefault();
        var tokens = gameState.GetPlayerTokens(player);

        Assert.That(tokens, Is.Not.Null, $"Token set for {player} should not be null");
        Assert.That(tokens.Count, Is.EqualTo(expectedTokenCount), $"Token set for {player} should contain {expectedTokenCount} tokens");
    }

    /// <summary>
    ///   Verifies that GetPlayerTokens returns different references for different players.
    /// </summary>
    ///
    [Test]
    public void GetPlayerTokensReturnsDifferentReferencesForDifferentPlayers()
    {
        var gameState = GameState.CreateDefault();
        var oddTokens = gameState.GetPlayerTokens(PlayerToken.Odd);
        var evenTokens = gameState.GetPlayerTokens(PlayerToken.Even);

        Assert.That(oddTokens, Is.Not.SameAs(evenTokens), "Odd and even token sets should be different references");
        Assert.That(oddTokens.Intersect(evenTokens), Is.Empty, "Odd and even token sets should not share any tokens");
    }

    /// <summary>
    ///   Verifies that GetPlayerTokens works correctly when called multiple times for the same player.
    /// </summary>
    ///
    [Test]
    public void GetPlayerTokensIsConsistentAcrossMultipleCalls()
    {
        var gameState = GameState.CreateDefault();

        // Multiple calls should return the same reference.

        var tokens1 = gameState.GetPlayerTokens(PlayerToken.Odd);
        var tokens2 = gameState.GetPlayerTokens(PlayerToken.Odd);
        var tokens3 = gameState.GetPlayerTokens(PlayerToken.Even);
        var tokens4 = gameState.GetPlayerTokens(PlayerToken.Even);

        Assert.That(tokens1, Is.SameAs(tokens2), "Multiple calls for odd tokens should return the same reference");
        Assert.That(tokens3, Is.SameAs(tokens4), "Multiple calls for even tokens should return the same reference");
    }

    /// <summary>
    ///   Verifies that GameState can be created with custom values and maintains those values.
    /// </summary>
    ///
    [Test]
    public void GameStateWithCustomValuesRetainsCorrectState()
    {
        var customBoard = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        var customOddTokens = new HashSet<byte> { 1, 5, 9 };
        var customEvenTokens = new HashSet<byte> { 2, 6 };
        var customTokens = new HashSet<byte>[] { customOddTokens, customEvenTokens };
        var gameState = new GameState(PlayerToken.Even, customBoard, 15, customTokens);

        Assert.That(gameState.CurrentTurn, Is.EqualTo(PlayerToken.Even), "Current turn should be PlayerToken.Even");
        Assert.That(gameState.Board, Is.SameAs(customBoard), "Board should be the same reference");
        Assert.That(gameState.CurrentPlayerTokens, Is.SameAs(customEvenTokens), "Current player tokens should be custom even tokens");
        Assert.That(gameState.GetPlayerTokens(PlayerToken.Even), Is.SameAs(customEvenTokens), "Even player tokens should be custom even tokens");
        Assert.That(gameState.GetPlayerTokens(PlayerToken.Odd), Is.SameAs(customOddTokens), "Odd player tokens should be custom odd tokens");
    }

    /// <summary>
    ///   Verifies that GameState record equality works correctly for the same instance.
    /// </summary>
    ///
    [Test]
    public void GameStateEqualityWorksForSameInstance()
    {
        var gameState = GameState.CreateDefault();
        var sameGameState = gameState;

        Assert.That(sameGameState, Is.EqualTo(gameState), "A game state should be equal to itself");
        Assert.That(sameGameState.GetHashCode(), Is.EqualTo(gameState.GetHashCode()), "Hash codes should be consistent for the same instance");
    }

    /// <summary>
    ///   Verifies that GameState 'with' expressions work correctly for creating modified copies.
    /// </summary>
    ///
    [Test]
    public void GameStateWithExpressionCreatesCorrectModifiedCopy()
    {
        var originalState = GameState.CreateDefault();
        var modifiedState = originalState with { CurrentTurn = PlayerToken.Even };

        Assert.That(modifiedState.CurrentTurn, Is.EqualTo(PlayerToken.Even), "Modified state should have PlayerToken.Even as current turn");
        Assert.That(originalState.CurrentTurn, Is.EqualTo(PlayerToken.Odd), "Original state should remain unchanged");
        Assert.That(modifiedState.Board, Is.SameAs(originalState.Board), "Board reference should be shared");
        Assert.That(modifiedState.GetPlayerTokens(PlayerToken.Odd), Is.SameAs(originalState.GetPlayerTokens(PlayerToken.Odd)), "Odd tokens reference should be shared");
        Assert.That(modifiedState.GetPlayerTokens(PlayerToken.Even), Is.SameAs(originalState.GetPlayerTokens(PlayerToken.Even)), "Even tokens reference should be shared");
    }

    /// <summary>
    ///   Verifies that GameState handles edge cases with empty token sets gracefully.
    /// </summary>
    ///
    [Test]
    public void GameStateHandlesEmptyTokenSetsGracefully()
    {
        var emptyTokens = new HashSet<byte>[] { new HashSet<byte>(), new HashSet<byte>() };
        var gameState = new GameState(PlayerToken.Odd, new int[9], 15, emptyTokens);
        var currentTokens = gameState.CurrentPlayerTokens;
        var oddTokens = gameState.GetPlayerTokens(PlayerToken.Odd);
        var evenTokens = gameState.GetPlayerTokens(PlayerToken.Even);

        Assert.That(currentTokens.Count, Is.EqualTo(0), "Current player tokens should be empty");
        Assert.That(oddTokens.Count, Is.EqualTo(0), "Odd tokens should be empty");
        Assert.That(evenTokens.Count, Is.EqualTo(0), "Even tokens should be empty");
    }

    /// <summary>
    ///   Verifies that GameState works correctly with modified token sets.
    /// </summary>
    ///
    [Test]
    public void GameStateWorksCorrectlyWithModifiedTokenSets()
    {
        var gameState = GameState.CreateDefault();
        var originalOddCount = gameState.GetPlayerTokens(PlayerToken.Odd).Count;

        // Modify the token set through the returned reference.

        gameState.GetPlayerTokens(PlayerToken.Odd).Remove(1);

        var newOddCount = gameState.GetPlayerTokens(PlayerToken.Odd).Count;

        Assert.That(newOddCount, Is.EqualTo(originalOddCount - 1), "Token count should decrease after removal");
        Assert.That(gameState.GetPlayerTokens(PlayerToken.Odd).Contains(1), Is.False, "Token 1 should no longer be available");
    }

    /// <summary>
    ///   Verifies that GameState maintains referential integrity after token modifications.
    /// </summary>
    ///
    [Test]
    public void GameStateMaintainsReferentialIntegrityAfterTokenModifications()
    {
        var gameState = GameState.CreateDefault();
        var initialOddTokens = gameState.GetPlayerTokens(PlayerToken.Odd);

        // Modify tokens and verify references remain consistent.

        initialOddTokens.Add(11);
        var retrievedOddTokens = gameState.GetPlayerTokens(PlayerToken.Odd);

        Assert.That(retrievedOddTokens, Is.SameAs(initialOddTokens), "Token set reference should remain the same");
        Assert.That(retrievedOddTokens.Contains(11), Is.True, "Modified token set should contain the added token");
        Assert.That(gameState.CurrentPlayerTokens.Contains(11), Is.True, "Current player tokens should reflect the modification");
    }

    /// <summary>
    ///   Verifies that AlternatePlayerTurn works correctly with all valid PlayerToken values using TestCase parameters.
    /// </summary>
    ///
    /// <param name="inputToken">The input token to start with.</param>
    /// <param name="expectedToken">The expected token after alternation.</param>
    ///
    [Test]
    [TestCase(PlayerToken.Odd, PlayerToken.Even)]
    [TestCase(PlayerToken.Even, PlayerToken.Odd)]
    public void AlternatePlayerTurnWithValidPlayersReturnsCorrectOpposite(PlayerToken inputToken,
                                                                          PlayerToken expectedToken)
    {
        var gameState = GameState.CreateDefault() with { CurrentTurn = inputToken };
        gameState.AlternatePlayerTurn();

        Assert.That(gameState.CurrentTurn, Is.EqualTo(expectedToken), $"PlayerToken.{inputToken} should alternate to PlayerToken.{expectedToken}");
    }

    /// <summary>
    ///   Verifies that AlternatePlayerTurn is symmetric - alternating twice returns the original player.
    /// </summary>
    ///
    /// <param name="originalToken">The original token to test.</param>
    ///
    [Test]
    [TestCase(PlayerToken.Odd)]
    [TestCase(PlayerToken.Even)]
    public void AlternatePlayerTurnIsSymmetricReturnsOriginalAfterTwoAlternations(PlayerToken originalToken)
    {
        var gameState = GameState.CreateDefault() with { CurrentTurn = originalToken };

        gameState.AlternatePlayerTurn();
        gameState.AlternatePlayerTurn();

        Assert.That(gameState.CurrentTurn, Is.EqualTo(originalToken), $"Alternating PlayerToken.{originalToken} twice should return the original token");
    }

    /// <summary>
    ///   Verifies that AlternatePlayerTurn works correctly in a round-trip scenario with multiple alternations.
    /// </summary>
    ///
    [Test]
    public void AlternatePlayerTurnMultipleRoundTripsWorksCorrectly()
    {
        var startingToken = PlayerToken.Odd;
        var gameState = GameState.CreateDefault() with { CurrentTurn = startingToken };

        // Perform multiple round trips.

        for (var index = 0; index < 10; ++index)
        {
            gameState.AlternatePlayerTurn();
            gameState.AlternatePlayerTurn();

            Assert.That(gameState.CurrentTurn, Is.EqualTo(startingToken), $"After round trip {index + 1}, token should be back to the starting value");
        }
    }

    /// <summary>
    ///   Verifies that AlternatePlayerTurn produces the expected sequence when called repeatedly.
    /// </summary>
    ///
    [Test]
    public void AlternatePlayerTurnProducesCorrectSequenceWhenCalledRepeatedly()
    {
        var gameState = GameState.CreateDefault() with { CurrentTurn = PlayerToken.Odd };
        var expectedSequence = new[] { PlayerToken.Even, PlayerToken.Odd, PlayerToken.Even, PlayerToken.Odd, PlayerToken.Even };

        for (var index = 0; index < expectedSequence.Length; ++index)
        {
            gameState.AlternatePlayerTurn();
            Assert.That(gameState.CurrentTurn, Is.EqualTo(expectedSequence[index]), $"Step {index + 1}: Expected {expectedSequence[index]}, but got {gameState.CurrentTurn}");
        }
    }

    /// <summary>
    ///   Verifies that AssertValidBoardPosition works correctly with valid positions.
    /// </summary>
    ///
    /// <param name="row">The row to test.</param>
    /// <param name="column">The column to test.</param>
    ///
    [Test]
    [TestCase(1, 1)]
    [TestCase(1, 3)]
    [TestCase(3, 1)]
    [TestCase(3, 3)]
    [TestCase(2, 2)]
    public void AssertValidBoardPositionAcceptsValidPositions(int row, int column)
    {
        var gameState = GameState.CreateDefault();

        Assert.That(() => gameState.AssertValidBoardPosition(row, column), Throws.Nothing,
            $"Position ({row}, {column}) should be valid for a 3x3 board");
    }

    /// <summary>
    ///   Verifies that AssertValidBoardPosition throws ArgumentOutOfRangeException for invalid row positions.
    /// </summary>
    ///
    /// <param name="row">The invalid row to test.</param>
    ///
    [Test]
    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(4)]
    [TestCase(10)]
    public void AssertValidBoardPositionThrowsForInvalidRow(int row)
    {
        var gameState = GameState.CreateDefault();

        Assert.That(() => gameState.AssertValidBoardPosition(row, 1),
            Throws.InstanceOf<ArgumentOutOfRangeException>()
                .With.Property("ParamName").EqualTo("row"),
            $"Position ({row}, 1) should throw ArgumentOutOfRangeException for invalid row");
    }

    /// <summary>
    ///   Verifies that AssertValidBoardPosition throws ArgumentOutOfRangeException for invalid column positions.
    /// </summary>
    ///
    /// <param name="column">The invalid column to test.</param>
    ///
    [Test]
    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(4)]
    [TestCase(10)]
    public void AssertValidBoardPositionThrowsForInvalidColumn(int column)
    {
        var gameState = GameState.CreateDefault();

        Assert.That(() => gameState.AssertValidBoardPosition(1, column),
            Throws.InstanceOf<ArgumentOutOfRangeException>()
                .With.Property("ParamName").EqualTo("column"),
            $"Position (1, {column}) should throw ArgumentOutOfRangeException for invalid column");
    }

    /// <summary>
    ///   Verifies that GetBoardToken and SetBoardToken work correctly together.
    /// </summary>
    ///
    [Test]
    public void GetAndSetBoardTokenWorkCorrectly()
    {
        var gameState = GameState.CreateDefault();

        // Set tokens at various positions and verify they can be retrieved correctly.

        gameState.SetBoardToken(1, 1, 5);
        gameState.SetBoardToken(2, 3, 7);
        gameState.SetBoardToken(3, 2, 9);

        Assert.That(gameState.GetBoardToken(1, 1), Is.EqualTo(5), "Token at (1,1) should be 5");
        Assert.That(gameState.GetBoardToken(2, 3), Is.EqualTo(7), "Token at (2,3) should be 7");
        Assert.That(gameState.GetBoardToken(3, 2), Is.EqualTo(9), "Token at (3,2) should be 9");

        // Verify other positions remain 0 (default).

        Assert.That(gameState.GetBoardToken(1, 2), Is.EqualTo(0), "Unset positions should remain 0");
        Assert.That(gameState.GetBoardToken(3, 3), Is.EqualTo(0), "Unset positions should remain 0");
    }

    /// <summary>
    ///   Verifies that SetBoardToken correctly overwrites existing values.
    /// </summary>
    ///
    [Test]
    public void SetBoardTokenOverwritesExistingValues()
    {
        var gameState = GameState.CreateDefault();

        // Set initial value.

        gameState.SetBoardToken(2, 2, 3);
        Assert.That(gameState.GetBoardToken(2, 2), Is.EqualTo(3), "Initial value should be set");

        // Overwrite with new value.

        gameState.SetBoardToken(2, 2, 8);
        Assert.That(gameState.GetBoardToken(2, 2), Is.EqualTo(8), "Value should be overwritten");
    }

    /// <summary>
    ///   Verifies that GetBoardPositionFromIndex works correctly for valid indices.
    /// </summary>
    ///
    /// <param name="index">The array index to convert.</param>
    /// <param name="expectedRow">The expected row (1-based).</param>
    /// <param name="expectedColumn">The expected column (1-based).</param>
    ///
    [Test]
    [TestCase(0, 1, 1)]
    [TestCase(1, 1, 2)]
    [TestCase(2, 1, 3)]
    [TestCase(3, 2, 1)]
    [TestCase(4, 2, 2)]
    [TestCase(5, 2, 3)]
    [TestCase(6, 3, 1)]
    [TestCase(7, 3, 2)]
    [TestCase(8, 3, 3)]
    public void GetBoardPositionFromIndexReturnsCorrectPosition(int index, int expectedRow, int expectedColumn)
    {
        var gameState = GameState.CreateDefault();
        var (row, column) = gameState.GetBoardPositionFromIndex(index);

        Assert.That(row, Is.EqualTo(expectedRow), $"Row for index {index} should be {expectedRow}");
        Assert.That(column, Is.EqualTo(expectedColumn), $"Column for index {index} should be {expectedColumn}");
    }

    /// <summary>
    ///   Verifies that GetBoardPositionFromIndex throws ArgumentOutOfRangeException for invalid indices.
    /// </summary>
    ///
    /// <param name="invalidIndex">The invalid index to test.</param>
    ///
    [Test]
    [TestCase(-1)]
    [TestCase(9)]
    [TestCase(10)]
    [TestCase(100)]
    public void GetBoardPositionFromIndexThrowsForInvalidIndex(int invalidIndex)
    {
        var gameState = GameState.CreateDefault();

        Assert.That(() => gameState.GetBoardPositionFromIndex(invalidIndex),
            Throws.InstanceOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("index"),
            $"Index {invalidIndex} should throw ArgumentOutOfRangeException for invalid index");
    }

    /// <summary>
    ///   Verifies that board position methods work correctly with different board sizes.
    /// </summary>
    ///
    [Test]
    public void BoardPositionMethodsWorkWithDifferentBoardSizes()
    {
        // Test with 4x4 board.

        var largerGameState = new GameState(
            PlayerToken.Odd,
            new int[16],
            30,
            [
                new HashSet<byte> { 1, 3, 5, 7, 9, 11, 13, 15 },
                new HashSet<byte> { 2, 4, 6, 8, 10, 12, 14, 16 }
            ]);

        // Test corner positions for 4x4 board.

        Assert.That(() => largerGameState.AssertValidBoardPosition(1, 1), Throws.Nothing, "Position (1,1) should be valid for 4x4 board");
        Assert.That(() => largerGameState.AssertValidBoardPosition(4, 4), Throws.Nothing, "Position (4,4) should be valid for 4x4 board");

        // Test that invalid positions throw for 4x4 board.

        Assert.That(() => largerGameState.AssertValidBoardPosition(5, 1),
            Throws.InstanceOf<ArgumentOutOfRangeException>(),
            "Position (5,1) should be invalid for 4x4 board");

        Assert.That(() => largerGameState.AssertValidBoardPosition(1, 5),
            Throws.InstanceOf<ArgumentOutOfRangeException>(),
            "Position (1,5) should be invalid for 4x4 board");

        // Test position conversion for 4x4 board.

        var (row, column) = largerGameState.GetBoardPositionFromIndex(15); // Last position

        Assert.That(row, Is.EqualTo(4), "Last index should convert to row 4");
        Assert.That(column, Is.EqualTo(4), "Last index should convert to column 4");

        // Test set/get for 4x4 board.

        largerGameState.SetBoardToken(3, 4, 42);
        Assert.That(largerGameState.GetBoardToken(3, 4), Is.EqualTo(42), "Token should be set and retrieved correctly on 4x4 board");
    }

    /// <summary>
    ///   Verifies that GameState constructor throws InvalidOperationException for non-square boards.
    /// </summary>
    ///
    /// <param name="boardSize">The size of the non-square board to test.</param>
    ///
    [Test]
    [TestCase(2)]   // Not a perfect square
    [TestCase(3)]   // Not a perfect square
    [TestCase(5)]   // Not a perfect square
    [TestCase(6)]   // Not a perfect square
    [TestCase(7)]   // Not a perfect square
    [TestCase(8)]   // Not a perfect square
    [TestCase(10)]  // Not a perfect square
    [TestCase(15)]  // Not a perfect square
    public void GameStateConstructorThrowsInvalidOperationExceptionForNonSquareBoard(int boardSize)
    {
        Assert.That(() => new GameState(
            PlayerToken.Odd,
            new int[boardSize],
            15,
            [
                new HashSet<byte> { 1, 3, 5 },
                new HashSet<byte> { 2, 4, 6 }
            ]),
        Throws.InstanceOf<InvalidOperationException>(),
            $"GameState creation with {boardSize}-element board should throw InvalidOperationException");
    }

    /// <summary>
    ///   Verifies that GetWinner returns null for a new game.
    /// </summary>
    ///
    [Test]
    public void GetWinnerReturnsNullForNewGame()
    {
        var gameState = GameState.CreateDefault();
        var winner = gameState.ScanForWinner();

        Assert.That(winner, Is.Null, "New game should not have a winner");
    }

    /// <summary>
    ///   Verifies that GetWinner returns the correct winner when there is a winning combination.
    /// </summary>
    ///
    [Test]
    public void GetWinnerReturnsCorrectWinnerForWinningCombination()
    {
        // Create a board with a winning row (1 + 5 + 9 = 15).

        var board = new int[]
        {
            1, 5, 9,
            0, 0, 0,
            0, 0, 0
        };

        var gameState = GameState.CreateDefault() with { Board = board, CurrentTurn = PlayerToken.Even };
        var winner = gameState.ScanForWinner();

        Assert.That(winner, Is.EqualTo(PlayerToken.Even), "Game should return PlayerToken.Even as winner when top row sums to 15");
    }

    /// <summary>
    ///   Verifies that GetWinner works correctly for different board sizes.
    /// </summary>
    ///
    /// <param name="boardSize">The size of the board to test (must be a perfect square).</param>
    /// <param name="expectedResult">The expected result for the GetWinner call.</param>
    ///
    [Test]
    [TestCase(1, null)]
    [TestCase(4, null)]
    [TestCase(9, null)]
    [TestCase(16, null)]
    public void GetWinnerWorksForDifferentBoardSizes(int boardSize, PlayerToken? expectedResult)
    {
        var gameState = new GameState(PlayerToken.Odd, new int[boardSize], 15, [new HashSet<byte> { 1, 3, 5 }, new HashSet<byte> { 2, 4, 6 }]);
        var winner = gameState.ScanForWinner();

        Assert.That(winner, Is.EqualTo(expectedResult), $"GetWinner should return {expectedResult} for {boardSize}-element board");
    }

    /// <summary>
    ///   Verifies that GetWinner identifies a diagonal victory correctly.
    /// </summary>
    ///
    [Test]
    public void GetWinnerIdentifiesDiagonalVictory()
    {
        var board = new int[]
        {
            1, 0, 0,  // Main diagonal: 1 + 5 + 9 = 15.
            0, 5, 0,
            0, 0, 9
        };

        var gameState = GameState.CreateDefault() with { Board = board };
        var winner = gameState.ScanForWinner();

        Assert.That(winner, Is.EqualTo(PlayerToken.Odd), "Game should identify diagonal victory");
    }

    /// <summary>
    ///   Verifies that GetWinner identifies a column victory correctly.
    /// </summary>
    ///
    [Test]
    public void GetWinnerIdentifiesColumnVictory()
    {
        var board = new int[]
        {
            1, 0, 0,  // First column: 1 + 5 + 9 = 15.
            5, 0, 0,
            9, 0, 0
        };

        var gameState = GameState.CreateDefault() with { Board = board };
        var winner = gameState.ScanForWinner();

        Assert.That(winner, Is.EqualTo(PlayerToken.Odd), "Game should identify column victory");
    }

    /// <summary>
    ///   Verifies that GetWinner returns null when there is no winner.
    /// </summary>
    ///
    [Test]
    public void GetWinnerReturnsNullWhenNoWinner()
    {
        var gameState = GameState.CreateDefault();
        var winner = gameState.ScanForWinner();

        Assert.That(winner, Is.Null, "Game with no winning combination should return null");
    }

    /// <summary>
    ///   Verifies that GetWinner returns null for a partial game with no winning combination.
    /// </summary>
    ///
    [Test]
    public void GetWinnerReturnsNullForPartialGameWithNoWinner()
    {
        var board = new int[]
        {
            1, 2, 0,  // Partial game, no winning combination.
            3, 0, 0,
            0, 0, 0
        };

        var gameState = GameState.CreateDefault() with { Board = board };
        var winner = gameState.ScanForWinner();

        Assert.That(winner, Is.Null, "Partial game with no winner should return null");
    }

    /// <summary>
    ///   Verifies that GetWinner works correctly when multiple combinations could win simultaneously.
    /// </summary>
    ///
    [Test]
    public void GetWinnerReturnsCorrectWinnerWhenMultipleCombinationsWin()
    {
        var gameState = new GameState(
            PlayerToken.Odd,
            [5, 4, 6, 1, 5, 4, 3, 2, 7], // Multiple winning combinations
            15,
            [
                new HashSet<byte> { 1, 3, 5, 7, 9 },
                new HashSet<byte> { 2, 4, 6, 8 }
            ]);

        var winner = gameState.ScanForWinner();

        Assert.That(winner, Is.EqualTo(PlayerToken.Odd), "Should return the current turn player when multiple combinations win");
    }

    /// <summary>
    ///   Verifies that GetWinner works correctly with edge case of 1x1 board.
    /// </summary>
    ///
    [Test]
    public void GetWinnerWorksWithOneByOneBoard()
    {
        var gameState = new GameState(
            PlayerToken.Odd,
            [15], // Single cell with winning total
            15,
            [
                new HashSet<byte> { 1, 3, 5, 7, 9 },
                new HashSet<byte> { 2, 4, 6, 8 }
            ]);

        var winner = gameState.ScanForWinner();

        Assert.That(winner, Is.EqualTo(PlayerToken.Odd), "1x1 board with winning total should return current player");
    }

    /// <summary>
    ///   Verifies that GetWinner returns null for 1x1 board without winning total.
    /// </summary>
    ///
    [Test]
    public void GetWinnerReturnsNullForOneByOneBoardWithoutWin()
    {
        var gameState = new GameState(
            PlayerToken.Odd,
           [5], // Single cell without winning total
            15,
            [
                new HashSet<byte> { 1, 3, 5, 7, 9 },
                new HashSet<byte> { 2, 4, 6, 8 }
            ]);

        var winner = gameState.ScanForWinner();

        Assert.That(winner, Is.Null, "1x1 board without winning total should return null");
    }

    /// <summary>
    ///   Verifies that GetWinner works correctly with 4x4 board.
    /// </summary>
    ///
    [Test]
    public void GetWinnerWorksWithFourByFourBoard()
    {
        var gameState = new GameState(
            PlayerToken.Even,
           [2, 4, 6, 8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0], // Top row wins with even numbers
            20, // 2+4+6+8 = 20
            [
                new HashSet<byte> { 1, 3, 5, 7, 9 },
                new HashSet<byte> { 2, 4, 6, 8 }
            ]);

        var winner = gameState.ScanForWinner();
        Assert.That(winner, Is.EqualTo(PlayerToken.Even), "4x4 board should correctly identify winner");
    }

    /// <summary>
    ///   Verifies that GetWinner identifies anti-diagonal victory correctly.
    /// </summary>
    ///
    [Test]
    public void GetWinnerIdentifiesAntiDiagonalVictory()
    {
        var gameState = new GameState(
            PlayerToken.Odd,
            [0, 0, 3, 0, 5, 0, 7, 0, 0], // Anti-diagonal: 3+5+7=15
            15,
            [
                new HashSet<byte> { 1, 3, 5, 7, 9 },
                new HashSet<byte> { 2, 4, 6, 8 }
            ]);

        var winner = gameState.ScanForWinner();
        Assert.That(winner, Is.EqualTo(PlayerToken.Odd), "Should identify anti-diagonal victory");
    }

    /// <summary>
    ///   Verifies that GetWinner identifies top row victory correctly.
    /// </summary>
    ///
    [Test]
    public void GetWinnerIdentifiesTopRowVictory()
    {
        var gameState = new GameState(
            PlayerToken.Odd,
            [1, 5, 9, 0, 0, 0, 0, 0, 0], // Top row: 1+5+9=15
            15,
            [
                new HashSet<byte> { 1, 3, 5, 7, 9 },
                new HashSet<byte> { 2, 4, 6, 8 }
            ]);

        var winner = gameState.ScanForWinner();
        Assert.That(winner, Is.EqualTo(PlayerToken.Odd), "Should identify top row victory");
    }

    /// <summary>
    ///   Verifies that GetWinner identifies main diagonal victory with detailed verification.
    /// </summary>
    ///
    [Test]
    public void GetWinnerIdentifiesMainDiagonalVictoryDetailed()
    {
        var gameState = new GameState(
            PlayerToken.Odd,
            [1, 0, 0, 0, 5, 0, 0, 0, 9], // Main diagonal: 1+5+9=15
            15,
            [
                new HashSet<byte> { 1, 3, 5, 7, 9 },
                new HashSet<byte> { 2, 4, 6, 8 }
            ]);

        var winner = gameState.ScanForWinner();
        Assert.That(winner, Is.EqualTo(PlayerToken.Odd), "Should identify main diagonal victory");
    }

    /// <summary>
    ///   Verifies that GetWinner identifies middle column victory with detailed verification.
    /// </summary>
    ///
    [Test]
    public void GetWinnerIdentifiesMiddleColumnVictoryDetailed()
    {
        var gameState = new GameState(
            PlayerToken.Odd,
            [0, 1, 0, 0, 5, 0, 0, 9, 0], // Middle column: 1+5+9=15
            15,
            [
                new HashSet<byte> { 1, 3, 5, 7, 9 },
                new HashSet<byte> { 2, 4, 6, 8 }
            ]);

        var winner = gameState.ScanForWinner();
        Assert.That(winner, Is.EqualTo(PlayerToken.Odd), "Should identify middle column victory");
    }

    /// <summary>
    ///   Verifies that GetWinner handles boundary case where sum is close to but not equal to WinningTotal.
    /// </summary>
    ///
    [Test]
    public void GetWinnerReturnNullWhenSumIsCloseButNotEqualToWinningTotal()
    {
        var board = new int[]
        {
            1, 2, 0,  // Top row: 1 + 2 + 11 = 14 (one less than 15)
            0, 0, 0,
            0, 0, 11
        };

        var gameState = GameState.CreateDefault() with { Board = board };
        var winner = gameState.ScanForWinner();

        Assert.That(winner, Is.Null, "Board with sum close to but not equal to WinningTotal should return null");
    }

    /// <summary>
    ///   Verifies that ApplyMove successfully applies a valid move and updates game state correctly.
    /// </summary>
    ///
    [Test]
    public void ApplyMoveSuccessfullyAppliesValidMove()
    {
        var gameState = GameState.CreateDefault();
        var initialTokenCount = gameState.CurrentPlayerTokens.Count;
        var move = new Move(PlayerToken.Odd, 0, 1, null);

        gameState.ApplyMove(move);

        Assert.That(gameState.Board[0], Is.EqualTo(1), "Board position should contain the placed token");
        Assert.That(gameState.CurrentPlayerTokens.Count, Is.EqualTo(initialTokenCount - 1), "Current player should have one fewer token");
        Assert.That(gameState.CurrentPlayerTokens.Contains(1), Is.False, "Used token should be removed from current player's tokens");
        Assert.That(gameState.CurrentTurn, Is.EqualTo(PlayerToken.Even), "Turn should alternate to next player");
        Assert.That(gameState.Winner, Is.Null, "No winner should be detected for a single move");
    }

    /// <summary>
    ///   Verifies that ApplyMove throws ArgumentNullException when move is null.
    /// </summary>
    ///
    [Test]
    public void ApplyMoveThrowsArgumentNullExceptionWhenMoveIsNull()
    {
        var gameState = GameState.CreateDefault();

        Assert.That(() => gameState.ApplyMove(null!),
            Throws.InstanceOf<ArgumentNullException>().With.Property("ParamName").EqualTo("move"),
            "ApplyMove should throw ArgumentNullException for null move");
    }

    /// <summary>
    ///   Verifies that ApplyMove throws InvalidOperationException when token is not available for current player.
    /// </summary>
    ///
    [Test]
    public void ApplyMoveThrowsInvalidOperationExceptionWhenTokenNotAvailable()
    {
        var gameState = GameState.CreateDefault();
        var invalidMove = new Move(PlayerToken.Odd, 0, 2, null); // Token 2 belongs to even player

        Assert.That(() => gameState.ApplyMove(invalidMove),
            Throws.InstanceOf<InvalidOperationException>()
                .With.Message.Contains("The token 2 is not available for the current player"),
            "ApplyMove should throw InvalidOperationException for unavailable token");
    }

    /// <summary>
    ///   Verifies that ApplyMove throws ArgumentOutOfRangeException when position index is out of bounds.
    /// </summary>
    ///
    /// <param name="invalidIndex">The invalid position index to test.</param>
    ///
    [Test]
    [TestCase(-1)]
    [TestCase(9)]
    [TestCase(10)]
    [TestCase(100)]
    public void ApplyMoveThrowsArgumentOutOfRangeExceptionForInvalidPosition(int invalidIndex)
    {
        var gameState = GameState.CreateDefault();
        var invalidMove = new Move(PlayerToken.Odd, invalidIndex, 1, null);

        Assert.That(() => gameState.ApplyMove(invalidMove),
            Throws.InstanceOf<ArgumentOutOfRangeException>()
                .With.Property("ParamName").EqualTo("PositionIndex"),
            $"ApplyMove should throw ArgumentOutOfRangeException for position index {invalidIndex}");
    }

    /// <summary>
    ///   Verifies that ApplyMove throws InvalidOperationException when position is already occupied.
    /// </summary>
    ///
    [Test]
    public void ApplyMoveThrowsInvalidOperationExceptionWhenPositionOccupied()
    {
        var gameState = GameState.CreateDefault();
        gameState.SetBoardToken(1, 1, 5); // Occupy position (1,1) which is index 0

        var move = new Move(PlayerToken.Odd, 0, 1, null);

        Assert.That(() => gameState.ApplyMove(move),
            Throws.InstanceOf<InvalidOperationException>()
                .With.Message.Contains("The position at row 1, column 1 is already occupied"),
            "ApplyMove should throw InvalidOperationException for occupied position");
    }

    /// <summary>
    ///   Verifies that ApplyMove correctly detects a winning move and returns the winner.
    /// </summary>
    ///
    [Test]
    public void ApplyMoveDetectsWinningMove()
    {
        var gameState = GameState.CreateDefault();

        // Set up a winning scenario: place 1 and 5 in top row, then place 9 to complete the win
        gameState.SetBoardToken(1, 1, 1); // Position (1,1) = index 0
        gameState.SetBoardToken(1, 2, 5); // Position (1,2) = index 1

        var winningMove = new Move(PlayerToken.Odd, 2, 9, null); // Position (1,3) = index 2
        gameState.ApplyMove(winningMove);

        Assert.That(gameState.Winner, Is.EqualTo(PlayerToken.Odd), "ApplyMove should detect winning move and set the winner");
        Assert.That(gameState.Board[2], Is.EqualTo(9), "Winning token should be placed on the board");
    }

    /// <summary>
    ///   Verifies that ApplyMove works correctly with different board positions.
    /// </summary>
    ///
    /// <param name="positionIndex">The board position index to test.</param>
    /// <param name="expectedRow">The expected row for position validation.</param>
    /// <param name="expectedColumn">The expected column for position validation.</param>
    ///
    [Test]
    [TestCase(0, 1, 1)]
    [TestCase(4, 2, 2)]
    [TestCase(8, 3, 3)]
    public void ApplyMoveWorksWithDifferentBoardPositions(int positionIndex, int expectedRow, int expectedColumn)
    {
        var gameState = GameState.CreateDefault();
        var move = new Move(PlayerToken.Odd, positionIndex, 1, null);

        gameState.ApplyMove(move);

        Assert.That(gameState.Board[positionIndex], Is.EqualTo(1), $"Token should be placed at position index {positionIndex}");
        Assert.That(gameState.GetBoardToken(expectedRow, expectedColumn), Is.EqualTo(1),
            $"Token should be accessible at row {expectedRow}, column {expectedColumn}");
        Assert.That(gameState.Winner, Is.Null, "Single move should not result in a win");
    }

    /// <summary>
    ///   Verifies that ApplyMove alternates turns correctly between players.
    /// </summary>
    ///
    [Test]
    public void ApplyMoveAlternatesTurnsCorrectly()
    {
        var gameState = GameState.CreateDefault();
        Assert.That(gameState.CurrentTurn, Is.EqualTo(PlayerToken.Odd), "Game should start with Odd player");

        // Apply odd player move
        var oddMove = new Move(PlayerToken.Odd, 0, 1, null);
        gameState.ApplyMove(oddMove);
        Assert.That(gameState.CurrentTurn, Is.EqualTo(PlayerToken.Even), "Turn should alternate to Even player");

        // Apply even player move
        var evenMove = new Move(PlayerToken.Even, 1, 2, null);
        gameState.ApplyMove(evenMove);
        Assert.That(gameState.CurrentTurn, Is.EqualTo(PlayerToken.Odd), "Turn should alternate back to Odd player");
    }

    /// <summary>
    ///   Verifies that ApplyMove correctly removes tokens from player's available tokens.
    /// </summary>
    ///
    [Test]
    public void ApplyMoveRemovesTokensFromPlayerTokens()
    {
        var gameState = GameState.CreateDefault();
        var oddTokens = gameState.GetPlayerTokens(PlayerToken.Odd);
        var evenTokens = gameState.GetPlayerTokens(PlayerToken.Even);

        var initialOddCount = oddTokens.Count;
        var initialEvenCount = evenTokens.Count;

        // Apply odd player move
        var oddMove = new Move(PlayerToken.Odd, 0, 1, null);
        gameState.ApplyMove(oddMove);

        Assert.That(oddTokens.Count, Is.EqualTo(initialOddCount - 1), "Odd player should have one fewer token");
        Assert.That(oddTokens.Contains(1), Is.False, "Token 1 should be removed from odd player tokens");
        Assert.That(evenTokens.Count, Is.EqualTo(initialEvenCount), "Even player tokens should remain unchanged");

        // Apply even player move
        var evenMove = new Move(PlayerToken.Even, 1, 2, null);
        gameState.ApplyMove(evenMove);

        Assert.That(evenTokens.Count, Is.EqualTo(initialEvenCount - 1), "Even player should have one fewer token");
        Assert.That(evenTokens.Contains(2), Is.False, "Token 2 should be removed from even player tokens");
    }

    /// <summary>
    ///   Verifies that ApplyMove works correctly with larger board sizes.
    /// </summary>
    ///
    [Test]
    public void ApplyMoveWorksWithLargerBoardSizes()
    {
        var largerGameState = new GameState(
            PlayerToken.Odd,
            new int[16],
            30,
            [
                new HashSet<byte> { 1, 3, 5, 7, 9, 11, 13, 15 },
                new HashSet<byte> { 2, 4, 6, 8, 10, 12, 14, 16 }
            ]);

        var move = new Move(PlayerToken.Odd, 15, 1, null); // Last position on 4x4 board
        largerGameState.ApplyMove(move);

        Assert.That(largerGameState.Board[15], Is.EqualTo(1), "Token should be placed at last position of 4x4 board");
        Assert.That(largerGameState.CurrentTurn, Is.EqualTo(PlayerToken.Even), "Turn should alternate after move");
        Assert.That(largerGameState.Winner, Is.Null, "Single move on larger board should not result in win");
    }

    /// <summary>
    ///   Verifies that ApplyMove correctly handles edge case with last available token.
    /// </summary>
    ///
    [Test]
    public void ApplyMoveHandlesLastAvailableToken()
    {
        var gameState = GameState.CreateDefault();
        var oddTokens = gameState.GetPlayerTokens(PlayerToken.Odd);

        // Remove all but one token from odd player
        var tokensToRemove = oddTokens.Where(t => t != 1).ToList();
        foreach (var token in tokensToRemove)
        {
            oddTokens.Remove(token);
        }

        Assert.That(oddTokens.Count, Is.EqualTo(1), "Odd player should have exactly one token remaining");
        Assert.That(oddTokens.Contains(1), Is.True, "Odd player should have token 1 remaining");

        var move = new Move(PlayerToken.Odd, 0, 1, null);
        gameState.ApplyMove(move);

        Assert.That(oddTokens.Count, Is.EqualTo(0), "Odd player should have no tokens remaining after move");
        Assert.That(gameState.Board[0], Is.EqualTo(1), "Last token should be placed on board");
        Assert.That(gameState.Winner, Is.Null, "Single move should not result in win");
    }

    /// <summary>
    ///   Verifies that ApplyMove validates token ownership correctly for different players.
    /// </summary>
    ///
    /// <param name="currentPlayer">The current player making the move.</param>
    /// <param name="validToken">A valid token for the current player.</param>
    /// <param name="invalidToken">An invalid token belonging to the other player.</param>
    ///
    [Test]
    [TestCase(PlayerToken.Odd, (byte)1, (byte)2)]
    [TestCase(PlayerToken.Even, (byte)2, (byte)1)]
    public void ApplyMoveValidatesTokenOwnershipCorrectly(PlayerToken currentPlayer, byte validToken, byte invalidToken)
    {
        var gameState = GameState.CreateDefault() with { CurrentTurn = currentPlayer };

        // Valid move should succeed
        var validMove = new Move(currentPlayer, 0, validToken, null);
        Assert.That(() => gameState.ApplyMove(validMove), Throws.Nothing,
            $"Valid move with token {validToken} should succeed for {currentPlayer}");

        // Reset for invalid move test
        gameState = GameState.CreateDefault() with { CurrentTurn = currentPlayer };

        // Invalid move should throw
        var invalidMove = new Move(currentPlayer, 0, invalidToken, null);
        Assert.That(() => gameState.ApplyMove(invalidMove),
            Throws.InstanceOf<InvalidOperationException>(),
            $"Invalid move with token {invalidToken} should fail for {currentPlayer}");
    }

    /// <summary>
    ///   Verifies that ApplyMove correctly handles complete game scenario with winner detection.
    /// </summary>
    ///
    [Test]
    public void ApplyMoveHandlesCompleteGameScenario()
    {
        var gameState = GameState.CreateDefault();

        // Play a sequence of moves leading to a win
        // Odd player: positions 0, 1 with tokens 1, 5
        var move1 = new Move(PlayerToken.Odd, 0, 1, null);
        gameState.ApplyMove(move1);
        Assert.That(gameState.Winner, Is.Null, "First move should not result in win");

        var move2 = new Move(PlayerToken.Even, 3, 2, null);
        gameState.ApplyMove(move2);
        Assert.That(gameState.Winner, Is.Null, "Second move should not result in win");

        var move3 = new Move(PlayerToken.Odd, 1, 5, null);
        gameState.ApplyMove(move3);
        Assert.That(gameState.Winner, Is.Null, "Third move should not result in win");

        var move4 = new Move(PlayerToken.Even, 4, 4, null);
        gameState.ApplyMove(move4);
        Assert.That(gameState.Winner, Is.Null, "Fourth move should not result in win");

        // Winning move: complete top row with 1 + 5 + 9 = 15
        var winningMove = new Move(PlayerToken.Odd, 2, 9, null);
        gameState.ApplyMove(winningMove);

        Assert.That(gameState.Winner, Is.EqualTo(PlayerToken.Odd), "Final move should result in Odd player winning");
        Assert.That(gameState.Board[0], Is.EqualTo(1), "Position 0 should contain token 1");
        Assert.That(gameState.Board[1], Is.EqualTo(5), "Position 1 should contain token 5");
        Assert.That(gameState.Board[2], Is.EqualTo(9), "Position 2 should contain token 9");
    }

    /// <summary>
    ///   Verifies that ApplyMove correctly handles edge case with zero position index.
    /// </summary>
    ///
    [Test]
    public void ApplyMoveHandlesZeroPositionIndex()
    {
        var gameState = GameState.CreateDefault();
        var move = new Move(PlayerToken.Odd, 0, 1, null);

        gameState.ApplyMove(move);

        Assert.That(gameState.Board[0], Is.EqualTo(1), "Token should be placed at position index 0");
        Assert.That(gameState.Winner, Is.Null, "Single move should not result in win");
        Assert.That(gameState.CurrentTurn, Is.EqualTo(PlayerToken.Even), "Turn should alternate after move");
    }

    /// <summary>
    ///   Verifies that ApplyMove correctly handles different token values for odd players.
    /// </summary>
    ///
    /// <param name="token">The odd token to test.</param>
    ///
    [Test]
    [TestCase((byte)1)]
    [TestCase((byte)3)]
    [TestCase((byte)5)]
    [TestCase((byte)7)]
    [TestCase((byte)9)]
    public void ApplyMoveHandlesDifferentOddTokens(byte token)
    {
        var gameState = GameState.CreateDefault();
        var move = new Move(PlayerToken.Odd, 0, token, null);

        gameState.ApplyMove(move);

        Assert.That(gameState.Board[0], Is.EqualTo(token), $"Token {token} should be placed on the board");
        Assert.That(gameState.CurrentPlayerTokens.Contains(token), Is.False, $"Token {token} should be removed from current player tokens");
        Assert.That(gameState.Winner, Is.Null, "Single token placement should not result in win");
    }

    /// <summary>
    ///   Verifies that ApplyMove correctly handles different token values for even players.
    /// </summary>
    ///
    /// <param name="token">The even token to test.</param>
    ///
    [Test]
    [TestCase((byte)2)]
    [TestCase((byte)4)]
    [TestCase((byte)6)]
    [TestCase((byte)8)]
    public void ApplyMoveHandlesDifferentEvenTokens(byte token)
    {
        var gameState = GameState.CreateDefault() with { CurrentTurn = PlayerToken.Even };
        var move = new Move(PlayerToken.Even, 0, token, null);

        gameState.ApplyMove(move);

        Assert.That(gameState.Board[0], Is.EqualTo(token), $"Token {token} should be placed on the board");
        Assert.That(gameState.CurrentPlayerTokens.Contains(token), Is.False, $"Token {token} should be removed from current player tokens");
        Assert.That(gameState.Winner, Is.Null, "Single token placement should not result in win");
    }

    /// <summary>
    ///   Verifies that ApplyMove correctly detects diagonal wins.
    /// </summary>
    ///
    [Test]
    public void ApplyMoveDetectsDiagonalWin()
    {
        var gameState = GameState.CreateDefault();

        // Set up diagonal win: 1 + 5 + 9 = 15
        gameState.SetBoardToken(1, 1, 1); // Position (1,1) = index 0
        gameState.SetBoardToken(2, 2, 5); // Position (2,2) = index 4

        // Complete diagonal with token 9
        var winningMove = new Move(PlayerToken.Odd, 8, 9, null); // Position (3,3) = index 8
        gameState.ApplyMove(winningMove);

        Assert.That(gameState.Winner, Is.EqualTo(PlayerToken.Odd), "Diagonal win should be detected");
        Assert.That(gameState.Board[8], Is.EqualTo(9), "Winning token should be placed");
    }

    /// <summary>
    ///   Verifies that ApplyMove correctly detects column wins.
    /// </summary>
    ///
    [Test]
    public void ApplyMoveDetectsColumnWin()
    {
        var gameState = GameState.CreateDefault();

        // Set up column win: 1 + 5 + 9 = 15 (first column)
        gameState.SetBoardToken(1, 1, 1); // Position (1,1) = index 0
        gameState.SetBoardToken(2, 1, 5); // Position (2,1) = index 3

        // Complete column with token 9
        var winningMove = new Move(PlayerToken.Odd, 6, 9, null); // Position (3,1) = index 6
        gameState.ApplyMove(winningMove);

        Assert.That(gameState.Winner, Is.EqualTo(PlayerToken.Odd), "Column win should be detected");
        Assert.That(gameState.Board[6], Is.EqualTo(9), "Winning token should be placed");
    }

    /// <summary>
    ///   Verifies that ApplyMove throws InvalidOperationException for token already used by same player.
    /// </summary>
    ///
    [Test]
    public void ApplyMoveThrowsInvalidOperationExceptionForAlreadyUsedToken()
    {
        var gameState = GameState.CreateDefault();

        // Use token 1 first
        var firstMove = new Move(PlayerToken.Odd, 0, 1, null);
        gameState.ApplyMove(firstMove);

        // Reset turn back to Odd to test reusing token
        gameState.AlternatePlayerTurn();

        // Try to use token 1 again (should fail)
        var invalidMove = new Move(PlayerToken.Odd, 1, 1, null);

        Assert.That(() => gameState.ApplyMove(invalidMove),
            Throws.InstanceOf<InvalidOperationException>()
                .With.Message.Contains("The token 1 is not available for the current player"),
            "ApplyMove should throw InvalidOperationException for already used token");
    }

    /// <summary>
    ///   Verifies that ApplyMove correctly handles moves that don't result in immediate wins.
    /// </summary>
    ///
    [Test]
    public void ApplyMoveHandlesNonWinningMovesCorrectly()
    {
        var gameState = GameState.CreateDefault();

        // Make several moves that don't create wins
        var moves = new[]
        {
            new Move(PlayerToken.Odd, 0, 1, null),   // (1,1) = 1
            new Move(PlayerToken.Even, 4, 2, null),  // (2,2) = 2
            new Move(PlayerToken.Odd, 8, 3, null),   // (3,3) = 3
            new Move(PlayerToken.Even, 1, 4, null)   // (1,2) = 4
        };

        foreach (var move in moves)
        {
            gameState.ApplyMove(move);
            Assert.That(gameState.Winner, Is.Null, $"Move with token {move.Token} should not result in win");
        }

        // Verify final game state
        Assert.That(gameState.Board[0], Is.EqualTo(1), "Position 0 should contain token 1");
        Assert.That(gameState.Board[4], Is.EqualTo(2), "Position 4 should contain token 2");
        Assert.That(gameState.Board[8], Is.EqualTo(3), "Position 8 should contain token 3");
        Assert.That(gameState.Board[1], Is.EqualTo(4), "Position 1 should contain token 4");
        Assert.That(gameState.CurrentTurn, Is.EqualTo(PlayerToken.Odd), "Turn should be back to Odd player");
    }

    /// <summary>
    ///   Verifies that ApplyMove correctly validates Move.PositionIndex parameter name in exception.
    /// </summary>
    ///
    [Test]
    public void ApplyMoveValidatesParameterNameInPositionIndexException()
    {
        var gameState = GameState.CreateDefault();
        var invalidMove = new Move(PlayerToken.Odd, -1, 1, null);

        Assert.That(() => gameState.ApplyMove(invalidMove),
            Throws.InstanceOf<ArgumentOutOfRangeException>()
                .With.Property("ParamName").EqualTo("PositionIndex"),
            "ApplyMove should throw ArgumentOutOfRangeException with correct parameter name for invalid PositionIndex");
    }
}
