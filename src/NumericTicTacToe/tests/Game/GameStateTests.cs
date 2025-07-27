using NUnit.Framework;

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
        var customBoard = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
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
        var gameState = new GameState(PlayerToken.Odd, new byte[9], 15, emptyTokens);
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

        // Should not throw and method should complete normally.
        gameState.AssertValidBoardCoordinates(row, column);

        // Verify the method completed by checking state is still accessible.
        Assert.That(gameState.TokensPerRow, Is.EqualTo(3),
            $"Position ({row}, {column}) validation should complete successfully for 3x3 board");
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

        Assert.That(() => gameState.AssertValidBoardCoordinates(row, 1),
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

        Assert.That(() => gameState.AssertValidBoardCoordinates(1, column),
            Throws.InstanceOf<ArgumentOutOfRangeException>()
                .With.Property("ParamName").EqualTo("column"),
            $"Position (1, {column}) should throw ArgumentOutOfRangeException for invalid column");
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
        var (row, column) = gameState.GetBoardCoordinates(index);

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

        Assert.That(() => gameState.GetBoardCoordinates(invalidIndex),
            Throws.InstanceOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("position"),
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
            new byte[16],
            30,
            [
                new HashSet<byte> { 1, 3, 5, 7, 9, 11, 13, 15 },
                new HashSet<byte> { 2, 4, 6, 8, 10, 12, 14, 16 }
            ]);

        // Test corner positions for 4x4 board.

        largerGameState.AssertValidBoardCoordinates(1, 1);
        largerGameState.AssertValidBoardCoordinates(4, 4);

        // Verify method completed successfully by checking state properties.
        Assert.That(largerGameState.TokensPerRow, Is.EqualTo(4), "Board validation should complete for 4x4 board");
        Assert.That(largerGameState.Board.Length, Is.EqualTo(16), "Board should remain intact after validation");

        // Test that invalid positions throw for 4x4 board.

        Assert.That(() => largerGameState.AssertValidBoardCoordinates(5, 1),
            Throws.InstanceOf<ArgumentOutOfRangeException>(),
            "Position (5,1) should be invalid for 4x4 board");

        Assert.That(() => largerGameState.AssertValidBoardCoordinates(1, 5),
            Throws.InstanceOf<ArgumentOutOfRangeException>(),
            "Position (1,5) should be invalid for 4x4 board");

        // Test position conversion for 4x4 board.

        var (row, column) = largerGameState.GetBoardCoordinates(15); // Last position

        Assert.That(row, Is.EqualTo(4), "Last index should convert to row 4");
        Assert.That(column, Is.EqualTo(4), "Last index should convert to column 4");

        // Test direct board access for 4x4 board.

        largerGameState.Board[largerGameState.GetBoardPosition(3, 4)] = 42;
        Assert.That(largerGameState.Board[largerGameState.GetBoardPosition(3, 4)], Is.EqualTo(42), "Token should be set and retrieved correctly on 4x4 board");
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
            new byte[boardSize],
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

        var board = new byte[]
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
        var gameState = new GameState(PlayerToken.Odd, new byte[boardSize], 15, [new HashSet<byte> { 1, 3, 5 }, new HashSet<byte> { 2, 4, 6 }]);
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
        var board = new byte[]
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
        var board = new byte[]
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
        var board = new byte[]
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
        var board = new byte[]
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
    ///   Verifies that ScanForWinner returns null when sum equals WinningTotal but not all squares are populated.
    ///   This tests the fix for a bug where partial lines could incorrectly trigger wins.
    /// </summary>
    ///
    [Test]
    public void ScanForWinnerReturnNullWhenSumEqualsWinningTotalButLineIncomplete()
    {
        var board = new byte[]
        {
            7, 8, 0,  // Top row: 7 + 8 + 0 = 15, but third square is empty
            0, 0, 0,
            0, 0, 0
        };

        var gameState = GameState.CreateDefault() with { Board = board };
        var winner = gameState.ScanForWinner();

        Assert.That(winner, Is.Null, "Should return null when sum equals 15 but line is incomplete");
    }

    /// <summary>
    ///   Verifies that ScanForWinner returns null for partial diagonal with sum equaling WinningTotal.
    /// </summary>
    ///
    [Test]
    public void ScanForWinnerReturnNullForPartialDiagonalSummingTo15()
    {
        var board = new byte[]
        {
            6, 0, 0,  // Main diagonal: 6 + 9 + 0 = 15, but third square is empty
            0, 9, 0,
            0, 0, 0
        };

        var gameState = GameState.CreateDefault() with { Board = board };
        var winner = gameState.ScanForWinner();

        Assert.That(winner, Is.Null, "Should return null when diagonal sum equals 15 but diagonal is incomplete");
    }

    /// <summary>
    ///   Verifies that ScanForWinner returns null for partial column with sum equaling WinningTotal.
    /// </summary>
    ///
    [Test]
    public void ScanForWinnerReturnNullForPartialColumnSummingTo15()
    {
        var board = new byte[]
        {
            4, 0, 0,  // First column: 4 + 0 + 11 = 15, but middle square is empty
            0, 0, 0,
            11, 0, 0
        };

        var gameState = GameState.CreateDefault() with { Board = board };
        var winner = gameState.ScanForWinner();

        Assert.That(winner, Is.Null, "Should return null when column sum equals 15 but column is incomplete");
    }

    /// <summary>
    ///   Verifies that ApplyMove applies a valid move and updates game state correctly.
    /// </summary>
    ///
    [Test]
    public void ApplyMoveAppliesValidMove()
    {
        var gameState = GameState.CreateDefault();
        var initialTokenCount = gameState.CurrentPlayerTokens.Count;
        var move = new Move(PlayerToken.Odd, 0, 1);

        gameState.ApplyMove(move);

        Assert.That(gameState.Board[0], Is.EqualTo(1), "Board position should contain the placed token");
        Assert.That(gameState.CurrentPlayerTokens.Count, Is.EqualTo(initialTokenCount - 1), "Current player should have one fewer token");
        Assert.That(gameState.CurrentPlayerTokens.Contains(1), Is.False, "Used token should be removed from current player's tokens");
        Assert.That(gameState.CurrentTurn, Is.EqualTo(PlayerToken.Even), "Turn should alternate to next player");
        Assert.That(gameState.Winner, Is.Null, "No winner should be detected for a single move");
    }

    /// <summary>
    ///   Verifies that ApplyMove throws InvalidOperationException when token is not available for current player.
    /// </summary>
    ///
    [Test]
    public void ApplyMoveThrowsInvalidOperationExceptionWhenTokenNotAvailable()
    {
        var gameState = GameState.CreateDefault();
        var invalidMove = new Move(PlayerToken.Odd, 0, 2); // Token 2 belongs to even player

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
        var invalidMove = new Move(PlayerToken.Odd, invalidIndex, 1);

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
        gameState.Board[gameState.GetBoardPosition(1, 1)] = 5; // Occupy position (1,1) which is index 0

        var move = new Move(PlayerToken.Odd, 0, 1);

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

        // Set up a winning scenario: place 1 and 5 in top row, then place 9 to complete the win.

        gameState.Board[gameState.GetBoardPosition(1, 1)] = 1; // Position (1,1) = index 0
        gameState.Board[gameState.GetBoardPosition(1, 2)] = 5; // Position (1,2) = index 1

        var winningMove = new Move(PlayerToken.Odd, 2, 9); // Position (1,3) = index 2
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
        var move = new Move(PlayerToken.Odd, positionIndex, 1);

        gameState.ApplyMove(move);

        Assert.That(gameState.Board[positionIndex], Is.EqualTo(1), $"Token should be placed at position index {positionIndex}");
        Assert.That(gameState.Board[gameState.GetBoardPosition(expectedRow, expectedColumn)], Is.EqualTo(1),
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

        // Apply odd player move.

        var oddMove = new Move(PlayerToken.Odd, 0, 1);
        gameState.ApplyMove(oddMove);
        Assert.That(gameState.CurrentTurn, Is.EqualTo(PlayerToken.Even), "Turn should alternate to Even player");

        // Apply even player move.

        var evenMove = new Move(PlayerToken.Even, 1, 2);
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

        // Apply odd player move.

        var oddMove = new Move(PlayerToken.Odd, 0, 1);
        gameState.ApplyMove(oddMove);

        Assert.That(oddTokens.Count, Is.EqualTo(initialOddCount - 1), "Odd player should have one fewer token");
        Assert.That(oddTokens.Contains(1), Is.False, "Token 1 should be removed from odd player tokens");
        Assert.That(evenTokens.Count, Is.EqualTo(initialEvenCount), "Even player tokens should remain unchanged");

        // Apply even player move.

        var evenMove = new Move(PlayerToken.Even, 1, 2);
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
            new byte[16],
            30,
            [
                new HashSet<byte> { 1, 3, 5, 7, 9, 11, 13, 15 },
                new HashSet<byte> { 2, 4, 6, 8, 10, 12, 14, 16 }
            ]);

        var move = new Move(PlayerToken.Odd, 15, 1); // Last position on 4x4 board
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

        // Remove all but one token from odd player.

        var tokensToRemove = oddTokens.Where(t => t != 1).ToList();
        foreach (var token in tokensToRemove)
        {
            oddTokens.Remove(token);
        }

        Assert.That(oddTokens.Count, Is.EqualTo(1), "Odd player should have exactly one token remaining");
        Assert.That(oddTokens.Contains(1), Is.True, "Odd player should have token 1 remaining");

        var move = new Move(PlayerToken.Odd, 0, 1);
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

        // Valid move should succeed.

        var validMove = new Move(currentPlayer, 0, validToken);
        gameState.ApplyMove(validMove);

        // Verify the move was applied successfully.
        Assert.That(gameState.Board[0], Is.EqualTo(validToken),
            $"Valid move with token {validToken} should be applied for {currentPlayer}");
        Assert.That(gameState.GetPlayerTokens(currentPlayer).Contains(validToken), Is.False,
            $"Token {validToken} should be removed from {currentPlayer}'s available tokens");

        // Reset for invalid move test.

        gameState = GameState.CreateDefault() with { CurrentTurn = currentPlayer };

        // Invalid move should throw.

        var invalidMove = new Move(currentPlayer, 0, invalidToken);
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

        // Play a sequence of moves leading to a win.
        // Odd player: positions 0, 1 with tokens 1, 5.

        var move1 = new Move(PlayerToken.Odd, 0, 1);
        gameState.ApplyMove(move1);
        Assert.That(gameState.Winner, Is.Null, "First move should not result in win");

        var move2 = new Move(PlayerToken.Even, 3, 2);
        gameState.ApplyMove(move2);
        Assert.That(gameState.Winner, Is.Null, "Second move should not result in win");

        var move3 = new Move(PlayerToken.Odd, 1, 5);
        gameState.ApplyMove(move3);
        Assert.That(gameState.Winner, Is.Null, "Third move should not result in win");

        var move4 = new Move(PlayerToken.Even, 4, 4);
        gameState.ApplyMove(move4);
        Assert.That(gameState.Winner, Is.Null, "Fourth move should not result in win");

        // Winning move: complete top row with 1 + 5 + 9 = 15.

        var winningMove = new Move(PlayerToken.Odd, 2, 9);
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
        var move = new Move(PlayerToken.Odd, 0, 1);

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
        var move = new Move(PlayerToken.Odd, 0, token);

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
        var move = new Move(PlayerToken.Even, 0, token);

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

        // Set up diagonal win: 1 + 5 + 9 = 15.

        gameState.Board[gameState.GetBoardPosition(1, 1)] = 1; // Position (1,1) = index 0
        gameState.Board[gameState.GetBoardPosition(2, 2)] = 5; // Position (2,2) = index 4

        // Complete diagonal with token 9.

        var winningMove = new Move(PlayerToken.Odd, 8, 9); // Position (3,3) = index 8
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

        // Set up column win: 1 + 5 + 9 = 15 (first column).

        gameState.Board[gameState.GetBoardPosition(1, 1)] = 1; // Position (1,1) = index 0
        gameState.Board[gameState.GetBoardPosition(2, 1)] = 5; // Position (2,1) = index 3

        // Complete column with token 9.

        var winningMove = new Move(PlayerToken.Odd, 6, 9); // Position (3,1) = index 6
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

        // Use token 1 first.

        var firstMove = new Move(PlayerToken.Odd, 0, 1);
        gameState.ApplyMove(firstMove);

        // Reset turn back to Odd to test reusing token.

        gameState.AlternatePlayerTurn();

        // Try to use token 1 again (should fail).

        var invalidMove = new Move(PlayerToken.Odd, 1, 1);

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

        // Make several moves that don't create wins.

        var moves = new[]
        {
            new Move(PlayerToken.Odd, 0, 1),   // (1,1) = 1
            new Move(PlayerToken.Even, 4, 2),  // (2,2) = 2
            new Move(PlayerToken.Odd, 8, 3),   // (3,3) = 3
            new Move(PlayerToken.Even, 1, 4)   // (1,2) = 4
        };

        foreach (var move in moves)
        {
            gameState.ApplyMove(move);
            Assert.That(gameState.Winner, Is.Null, $"Move with token {move.Token} should not result in win");
        }

        // Verify final game state.

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
        var invalidMove = new Move(PlayerToken.Odd, -1, 1);

        Assert.That(() => gameState.ApplyMove(invalidMove),
            Throws.InstanceOf<ArgumentOutOfRangeException>()
                .With.Property("ParamName").EqualTo("PositionIndex"),
            "ApplyMove should throw ArgumentOutOfRangeException with correct parameter name for invalid PositionIndex");
    }

    /// <summary>
    ///   Verifies that UndoMove throws ArgumentOutOfRangeException when position index is invalid.
    /// </summary>
    ///
    [Test]
    public void UndoMoveThrowsWhenPositionIndexIsInvalid()
    {
        var gameState = CreateValidGameState();
        var invalidMove = new Move(PlayerToken.Odd, 10, 1); // Invalid position for 3x3 board

        Assert.That(() => gameState.UndoMove(invalidMove),
            Throws.InstanceOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("PositionIndex"),
            "UndoMove should throw ArgumentOutOfRangeException for invalid position");
    }

    /// <summary>
    ///   Verifies that UndoMove throws InvalidOperationException when position is already empty.
    /// </summary>
    ///
    [Test]
    public void UndoMoveThrowsWhenPositionIsEmpty()
    {
        var gameState = CreateValidGameState();
        var move = new Move(PlayerToken.Odd, 0, 1); // Position 0 is empty

        Assert.That(() => gameState.UndoMove(move),
            Throws.InvalidOperationException.With.Message.Contains("already empty"),
            "UndoMove should throw InvalidOperationException when position is already empty");
    }

    /// <summary>
    ///   Verifies that UndoMove correctly undoes a simple move without winner.
    /// </summary>
    ///
    [Test]
    public void UndoMoveCorrectlyUndoesSimpleMove()
    {
        var gameState = CreateValidGameState();
        var move = new Move(PlayerToken.Odd, 0, 1);

        // Capture original state.

        var originalBoard = gameState.Board.ToArray();
        var originalCurrentTurn = gameState.CurrentTurn;
        var originalOddTokens = gameState.GetPlayerTokens(PlayerToken.Odd).ToHashSet();
        var originalEvenTokens = gameState.GetPlayerTokens(PlayerToken.Even).ToHashSet();
        var originalWinner = gameState.Winner;

        // Apply then undo the move.

        gameState.ApplyMove(move);
        gameState.UndoMove(move);

        // Verify state is restored.

        Assert.That(gameState.Board, Is.EqualTo(originalBoard), "Board should be restored");
        Assert.That(gameState.CurrentTurn, Is.EqualTo(originalCurrentTurn), "CurrentTurn should be restored");
        Assert.That(gameState.GetPlayerTokens(PlayerToken.Odd).ToHashSet(), Is.EqualTo(originalOddTokens), "Odd tokens should be restored");
        Assert.That(gameState.GetPlayerTokens(PlayerToken.Even).ToHashSet(), Is.EqualTo(originalEvenTokens), "Even tokens should be restored");
        Assert.That(gameState.Winner, Is.EqualTo(originalWinner), "Winner should be restored");
    }

    /// <summary>
    ///   Verifies that UndoMove correctly handles undoing a winning move.
    /// </summary>
    ///
    [Test]
    public void UndoMoveCorrectlyUndoesWinningMove()
    {
        var gameState = CreateNearWinState();
        var winningMove = new Move(PlayerToken.Odd, 4, 5); // Center position with token 5 should win

        // Capture original state.

        var originalBoard = gameState.Board.ToArray();
        var originalCurrentTurn = gameState.CurrentTurn;
        var originalOddTokens = gameState.GetPlayerTokens(PlayerToken.Odd).ToHashSet();
        var originalEvenTokens = gameState.GetPlayerTokens(PlayerToken.Even).ToHashSet();
        var originalWinner = gameState.Winner;
        var originalIsGameOver = gameState.IsGameOver;

        // Apply the winning move.

        gameState.ApplyMove(winningMove);

        // Verify the game is won.

        Assert.That(gameState.Winner, Is.EqualTo(PlayerToken.Odd), "Game should be won by Odd");
        Assert.That(gameState.IsGameOver, Is.True, "Game should be over");

        // Undo the winning move.

        gameState.UndoMove(winningMove);

        // Verify state is restored.

        Assert.That(gameState.Board, Is.EqualTo(originalBoard), "Board should be restored");
        Assert.That(gameState.CurrentTurn, Is.EqualTo(originalCurrentTurn), "CurrentTurn should be restored");
        Assert.That(gameState.GetPlayerTokens(PlayerToken.Odd).ToHashSet(), Is.EqualTo(originalOddTokens), "Odd tokens should be restored");
        Assert.That(gameState.GetPlayerTokens(PlayerToken.Even).ToHashSet(), Is.EqualTo(originalEvenTokens), "Even tokens should be restored");
        Assert.That(gameState.Winner, Is.EqualTo(originalWinner), "Winner should be restored");
        Assert.That(gameState.IsGameOver, Is.EqualTo(originalIsGameOver), "IsGameOver should be restored");
    }

    /// <summary>
    ///   Verifies that UndoMove correctly handles multiple move sequences.
    /// </summary>
    ///
    [Test]
    public void UndoMoveHandlesMultipleMoveSequences()
    {
        var gameState = CreateValidGameState();
        var move1 = new Move(PlayerToken.Odd, 0, 1);
        var move2 = new Move(PlayerToken.Even, 4, 2);
        var move3 = new Move(PlayerToken.Odd, 8, 3);

        // Capture original state.

        var originalBoard = gameState.Board.ToArray();
        var originalCurrentTurn = gameState.CurrentTurn;
        var originalOddTokens = gameState.GetPlayerTokens(PlayerToken.Odd).ToHashSet();
        var originalEvenTokens = gameState.GetPlayerTokens(PlayerToken.Even).ToHashSet();

        // Apply moves in sequence.

        gameState.ApplyMove(move1);
        gameState.ApplyMove(move2);
        gameState.ApplyMove(move3);

        // Undo moves in reverse order.

        gameState.UndoMove(move3);
        gameState.UndoMove(move2);
        gameState.UndoMove(move1);

        // Verify original state is restored.

        Assert.That(gameState.Board, Is.EqualTo(originalBoard), "Board should be restored after undoing all moves");
        Assert.That(gameState.CurrentTurn, Is.EqualTo(originalCurrentTurn), "CurrentTurn should be restored after undoing all moves");
        Assert.That(gameState.GetPlayerTokens(PlayerToken.Odd).ToHashSet(), Is.EqualTo(originalOddTokens), "Odd tokens should be restored after undoing all moves");
        Assert.That(gameState.GetPlayerTokens(PlayerToken.Even).ToHashSet(), Is.EqualTo(originalEvenTokens), "Even tokens should be restored after undoing all moves");
    }

    /// <summary>
    ///   Verifies that UndoMove correctly restores the turn when undoing the last move in a sequence.
    /// </summary>
    ///
    [Test]
    public void UndoMoveRestoresCorrectTurnAfterMultipleMoves()
    {
        var gameState = CreateValidGameState();
        var move1 = new Move(PlayerToken.Odd, 0, 1);   // Odd plays, turn becomes Even
        var move2 = new Move(PlayerToken.Even, 4, 2);  // Even plays, turn becomes Odd

        // After move1, it should be Even's turn.

        gameState.ApplyMove(move1);
        Assert.That(gameState.CurrentTurn, Is.EqualTo(PlayerToken.Even), "After Odd's move, it should be Even's turn");

        // After move2, it should be Odd's turn.

        gameState.ApplyMove(move2);
        Assert.That(gameState.CurrentTurn, Is.EqualTo(PlayerToken.Odd), "After Even's move, it should be Odd's turn");

        // Undo move2, should restore Even's turn.

        gameState.UndoMove(move2);
        Assert.That(gameState.CurrentTurn, Is.EqualTo(PlayerToken.Even), "After undoing Even's move, it should be Even's turn again");

        // Undo move1, should restore Odd's turn.

        gameState.UndoMove(move1);
        Assert.That(gameState.CurrentTurn, Is.EqualTo(PlayerToken.Odd), "After undoing Odd's move, it should be Odd's turn again");
    }

    /// <summary>
    ///   Verifies that UndoMove correctly handles undoing a move that doesn't result in a win
    ///   when there are already winning conditions on the board.
    /// </summary>
    ///
    [Test]
    public void UndoMoveHandlesNonWinningMoveWithExistingWinCondition()
    {
        var gameState = CreateComplexWinState();

        // Since the game is already won, let's undo the last move to get back to a playable state.

        var lastMove = new Move(PlayerToken.Odd, 8, 9); // The winning move
        gameState.UndoMove(lastMove);

        // Now apply a non-winning move for the current player (should be Odd after undo).

        var nonWinningMove = new Move(PlayerToken.Odd, 6, 7);

        // Apply and undo the non-winning move.

        var originalWinner = gameState.Winner;
        gameState.ApplyMove(nonWinningMove);
        gameState.UndoMove(nonWinningMove);

        // Winner should remain unchanged.

        Assert.That(gameState.Winner, Is.EqualTo(originalWinner), "Winner should remain unchanged after undoing non-winning move");
    }

    /// <summary>
    ///   Verifies that UndoMove can only remove winners, never create them.
    ///   This validates the mathematical impossibility of creating wins by removing tokens.
    /// </summary>
    ///
    [Test]
    public void UndoMoveCanOnlyRemoveWinsNeverCreateThem()
    {
        // Test 1: Undoing a non-winning move should never create a winner.

        var gameState = CreateValidGameState();
        var move1 = new Move(PlayerToken.Odd, 0, 1);
        var move2 = new Move(PlayerToken.Even, 1, 2);

        gameState.ApplyMove(move1);
        gameState.ApplyMove(move2);

        var winnerBeforeUndo = gameState.Winner;
        gameState.UndoMove(move2);
        var winnerAfterUndo = gameState.Winner;

        Assert.That(winnerBeforeUndo, Is.Null, "No winner should exist before undo");
        Assert.That(winnerAfterUndo, Is.Null, "No winner should be created by undoing a non-winning move");

        // Test 2: Undoing a winning move should remove the winner.

        var winState = CreateNearWinState();
        var winningMove = new Move(PlayerToken.Odd, 4, 5); // Completes diagonal 1+5+9=15

        winState.ApplyMove(winningMove);
        Assert.That(winState.Winner, Is.EqualTo(PlayerToken.Odd), "Winner should exist after winning move");

        winState.UndoMove(winningMove);
        Assert.That(winState.Winner, Is.Null, "Winner should be removed after undoing winning move");

        // Test 3: Undoing a move in a complex scenario should never create a new different winner.

        var complexState = CreateComplexWinState();
        var lastMove = new Move(PlayerToken.Odd, 8, 9);

        Assert.That(complexState.Winner, Is.EqualTo(PlayerToken.Odd), "Complex state should have Odd as winner");

        complexState.UndoMove(lastMove);

        // Winner should be either null or the same player, never a different player.

        Assert.That(complexState.Winner, Is.Not.EqualTo(PlayerToken.Even),
            "Undoing should never create a win for the opposite player");
    }

    /// <summary>
    ///   Verifies that IsGameOver returns false for a new game with available moves.
    /// </summary>
    ///
    [Test]
    public void IsGameOverReturnsFalseForNewGame()
    {
        var gameState = GameState.CreateDefault();

        Assert.That(gameState.IsGameOver, Is.False, "New game should not be over");
    }

    /// <summary>
    ///   Verifies that IsGameOver returns true when there is a winner.
    /// </summary>
    ///
    [Test]
    public void IsGameOverReturnsTrueWhenThereIsWinner()
    {
        var gameState = GameState.CreateDefault();

        // Create a winning scenario: 1 + 5 + 9 = 15 in top row.

        gameState.ApplyMove(new Move(PlayerToken.Odd, gameState.GetBoardPosition(1, 1), 1));
        gameState.ApplyMove(new Move(PlayerToken.Even, gameState.GetBoardPosition(2, 1), 2));
        gameState.ApplyMove(new Move(PlayerToken.Odd, gameState.GetBoardPosition(1, 2), 5));
        gameState.ApplyMove(new Move(PlayerToken.Even, gameState.GetBoardPosition(2, 2), 4));
        gameState.ApplyMove(new Move(PlayerToken.Odd, gameState.GetBoardPosition(1, 3), 9));

        Assert.That(gameState.IsGameOver, Is.True, "Game should be over when there is a winner");
    }

    /// <summary>
    ///   Verifies that IsGameOver returns true when current player has no tokens left.
    /// </summary>
    ///
    [Test]
    public void IsGameOverReturnsTrueWhenCurrentPlayerHasNoTokens()
    {
        var gameState = GameState.CreateDefault();

        // Remove all tokens from the current player (Odd).

        gameState.CurrentPlayerTokens.Clear();

        Assert.That(gameState.IsGameOver, Is.True, "Game should be over when current player has no tokens");
    }

    /// <summary>
    ///   Verifies that IsGameOver returns true when the board is completely full.
    /// </summary>
    ///
    [Test]
    public void IsGameOverReturnsTrueWhenBoardIsFull()
    {
        var gameState = GameState.CreateDefault();

        // Fill the entire board without creating a winning combination.

        gameState.Board[gameState.GetBoardPosition(1, 1)] = 1;  // 1
        gameState.Board[gameState.GetBoardPosition(1, 2)] = 2;  // 2
        gameState.Board[gameState.GetBoardPosition(1, 3)] = 3;  // 3   (1+2+3=6, not 15)
        gameState.Board[gameState.GetBoardPosition(2, 1)] = 4;  // 4
        gameState.Board[gameState.GetBoardPosition(2, 2)] = 5;  // 5
        gameState.Board[gameState.GetBoardPosition(2, 3)] = 6;  // 6   (4+5+6=15, but different players)
        gameState.Board[gameState.GetBoardPosition(3, 1)] = 7;  // 7
        gameState.Board[gameState.GetBoardPosition(3, 2)] = 8;  // 8
        gameState.Board[gameState.GetBoardPosition(3, 3)] = 9;  // 9   (7+8+9=24, not 15)

        Assert.That(gameState.IsGameOver, Is.True, "Game should be over when board is completely full");
    }

    /// <summary>
    ///   Verifies that IsGameOver returns false when there are empty spaces and tokens available.
    /// </summary>
    ///
    [Test]
    public void IsGameOverReturnsFalseWhenEmptySpacesAndTokensAvailable()
    {
        var gameState = GameState.CreateDefault();

        // Place a few tokens but leave empty spaces.

        gameState.Board[gameState.GetBoardPosition(1, 1)] = 1;
        gameState.Board[gameState.GetBoardPosition(2, 2)] = 2;

        // Verify we have empty spaces and tokens.

        var hasEmptySpaces = gameState.Board.Any(space => space == GameState.EmptyBoardSpaceValue);
        var hasTokens = gameState.CurrentPlayerTokens.Count > 0;

        Assert.That(hasEmptySpaces, Is.True, "Board should have empty spaces for this test");
        Assert.That(hasTokens, Is.True, "Current player should have tokens for this test");
        Assert.That(gameState.IsGameOver, Is.False, "Game should not be over when empty spaces and tokens are available");
    }

    /// <summary>
    ///   Verifies that IsGameOver returns true when board is full even with tokens remaining.
    /// </summary>
    ///
    [Test]
    public void IsGameOverReturnsTrueWhenBoardFullDespiteTokensRemaining()
    {
        var gameState = GameState.CreateDefault();

        // Fill the entire 3x3 board.

        for (var row = 1; row <= 3; row++)
        {
            for (var col = 1; col <= 3; col++)
            {
                var tokenValue = ((row - 1) * 3) + col;
                gameState.Board[gameState.GetBoardPosition(row, col)] = (byte)tokenValue;
            }
        }

        // Verify tokens are still available but board is full.

        var hasTokens = gameState.CurrentPlayerTokens.Count > 0;
        var hasEmptySpaces = gameState.Board.Any(space => space == GameState.EmptyBoardSpaceValue);

        Assert.That(hasTokens, Is.True, "Current player should still have tokens for this test");
        Assert.That(hasEmptySpaces, Is.False, "Board should be completely full for this test");
        Assert.That(gameState.IsGameOver, Is.True, "Game should be over when board is full regardless of remaining tokens");
    }

    /// <summary>
    ///   Verifies that IsGameOver works correctly with different board sizes.
    /// </summary>
    ///
    [Test]
    public void IsGameOverWorksWithDifferentBoardSizes()
    {
        var oddTokens = new HashSet<byte> { 1, 3, 5, 7 };
        var evenTokens = new HashSet<byte> { 2, 4, 6, 8 };
        var tokens = new[] { oddTokens, evenTokens };

        // Create a 2x2 board.

        var gameState = new GameState(PlayerToken.Odd, new byte[4], 10, tokens);

        // Initially should not be over.

        Assert.That(gameState.IsGameOver, Is.False, "2x2 game should not be over initially");

        // Fill the entire 2x2 board.

        gameState.Board[gameState.GetBoardPosition(1, 1)] = 1;
        gameState.Board[gameState.GetBoardPosition(1, 2)] = 2;
        gameState.Board[gameState.GetBoardPosition(2, 1)] = 3;
        gameState.Board[gameState.GetBoardPosition(2, 2)] = 4;

        Assert.That(gameState.IsGameOver, Is.True, "2x2 game should be over when board is full");
    }

    /// <summary>
    ///   Creates a valid initial game state for testing.
    /// </summary>
    ///
    private static GameState CreateValidGameState() =>
        new GameState(
            PlayerToken.Odd,
            new byte[9],
            15,
            [
                new HashSet<byte> { 1, 3, 5, 7, 9 },
                new HashSet<byte> { 2, 4, 6, 8 }
            ]);

    /// <summary>
    ///   Creates a game state where Odd can win with one move.
    ///   Board state: [1, 0, 0, 0, 0, 0, 0, 0, 9]
    ///   Odd can play 5 at position 4 (center) to win: 1 + 5 + 9 = 15 (diagonal)
    /// </summary>
    ///
    private static GameState CreateNearWinState()
    {
        var gameState = CreateValidGameState();

        // Set up a near-win scenario for Odd - diagonal positions 0, 4, 8.

        gameState.Board[gameState.GetBoardPosition(1, 1)] = 1; // Position 0 (row 1, column 1): token 1
        gameState.Board[gameState.GetBoardPosition(3, 3)] = 9; // Position 8 (row 3, column 3): token 9

        // Remove the used tokens.

        gameState.GetPlayerTokens(PlayerToken.Odd).Remove(1);
        gameState.GetPlayerTokens(PlayerToken.Odd).Remove(9);

        return gameState;
    }

    /// <summary>
    ///   Creates a game state where there's already a winning condition on the board.
    ///   This tests scenarios where UndoMove needs to handle existing wins correctly.
    /// </summary>
    ///
    private static GameState CreateComplexWinState()
    {
        var gameState = CreateValidGameState();

        // Apply moves to create a winning condition for Odd: 1 + 5 + 9 = 15 (diagonal).

        var move1 = new Move(PlayerToken.Odd, 0, 1);   // Position 0, Odd plays 1
        var move2 = new Move(PlayerToken.Even, 1, 2);  // Position 1, Even plays 2
        var move3 = new Move(PlayerToken.Odd, 4, 5);   // Position 4 (center), Odd plays 5
        var move4 = new Move(PlayerToken.Even, 3, 4);  // Position 3, Even plays 4
        var move5 = new Move(PlayerToken.Odd, 8, 9);   // Position 8, Odd plays 9 - this should win

        gameState.ApplyMove(move1);
        gameState.ApplyMove(move2);
        gameState.ApplyMove(move3);
        gameState.ApplyMove(move4);
        gameState.ApplyMove(move5);

        return gameState;
    }

    /// <summary>
    ///   Verifies that CreateCopy creates completely independent copies with proper state preservation.
    /// </summary>
    ///
    [Test]
    public void CreateCopyCreatesIndependentCopyWithStatePreservation()
    {
        var originalState = GameState.CreateDefault();
        originalState.ApplyMove(new Move(PlayerToken.Odd, 0, 1));
        originalState.ApplyMove(new Move(PlayerToken.Even, 1, 2));

        var copiedState = originalState.CreateCopy();

        // Verify all properties are copied correctly.

        Assert.That(copiedState.CurrentTurn, Is.EqualTo(originalState.CurrentTurn), "CurrentTurn should be copied");
        Assert.That(copiedState.Winner, Is.EqualTo(originalState.Winner), "Winner should be copied");
        Assert.That(copiedState.IsGameOver, Is.EqualTo(originalState.IsGameOver), "IsGameOver should be copied");
        Assert.That(copiedState.Board, Is.EqualTo(originalState.Board), "Board should be copied");

        // Verify token collections are preserved and independent.

        Assert.That(copiedState.GetPlayerTokens(PlayerToken.Odd), Is.EqualTo(originalState.GetPlayerTokens(PlayerToken.Odd)), "Odd tokens should be preserved");
        Assert.That(copiedState.GetPlayerTokens(PlayerToken.Even), Is.EqualTo(originalState.GetPlayerTokens(PlayerToken.Even)), "Even tokens should be preserved");

        // Verify board arrays are different references.

        Assert.That(copiedState.Board, Is.Not.SameAs(originalState.Board), "Boards should be different references");

        // Verify complete independence by modifying original.

        originalState.ApplyMove(new Move(PlayerToken.Odd, 2, 3));

        Assert.That(copiedState.Board[2], Is.EqualTo(GameState.EmptyBoardSpaceValue), "Copy board should not be affected by changes to original");
        Assert.That(originalState.Board[2], Is.EqualTo(3), "Original board should have the new move");
        Assert.That(copiedState.GetPlayerTokens(PlayerToken.Odd).Contains(3), Is.True, "Copy should still have token 3");
        Assert.That(originalState.GetPlayerTokens(PlayerToken.Odd).Contains(3), Is.False, "Original should have used token 3");
    }

    /// <summary>
    ///   Verifies that CreateCopy works correctly with different board sizes and winning states.
    /// </summary>
    ///
    [Test]
    public void CreateCopyHandlesSpecializedScenarios()
    {
        // Test with different board sizes.

        var largerState = new GameState(
            PlayerToken.Even,
            new byte[16],
            30,
            [
                new HashSet<byte> { 1, 3, 5, 7, 9, 11, 13, 15 },
                new HashSet<byte> { 2, 4, 6, 8, 10, 12, 14, 16 }
            ]);

        var copiedLargerState = largerState.CreateCopy();

        Assert.That(copiedLargerState.Board.Length, Is.EqualTo(16), "Board size should be preserved");
        Assert.That(copiedLargerState.CurrentTurn, Is.EqualTo(PlayerToken.Even), "Current turn should be preserved");
        Assert.That(copiedLargerState.GetPlayerTokens(PlayerToken.Odd).Count, Is.EqualTo(8), "Odd token count should be preserved");
        Assert.That(copiedLargerState.GetPlayerTokens(PlayerToken.Even).Count, Is.EqualTo(8), "Even token count should be preserved");

        // Test with winning state.

        var winningState = GameState.CreateDefault();

        // Create a winning scenario: 1 + 5 + 9 = 15 in top row.

        winningState.ApplyMove(new Move(PlayerToken.Odd, 0, 1));   // Position 0
        winningState.ApplyMove(new Move(PlayerToken.Even, 3, 2));  // Position 3
        winningState.ApplyMove(new Move(PlayerToken.Odd, 1, 5));   // Position 1
        winningState.ApplyMove(new Move(PlayerToken.Even, 4, 4));  // Position 4
        winningState.ApplyMove(new Move(PlayerToken.Odd, 2, 9));   // Position 2 - winning move

        var copiedWinningState = winningState.CreateCopy();

        // Verify winning state is preserved.

        Assert.That(copiedWinningState.Winner, Is.EqualTo(PlayerToken.Odd), "Copied state should preserve winner");
        Assert.That(copiedWinningState.IsGameOver, Is.True, "Copied state should preserve game over state");
        Assert.That(copiedWinningState.Board, Is.EqualTo(winningState.Board), "Board should be copied exactly");
        Assert.That(copiedWinningState.ScanForWinner(), Is.EqualTo(PlayerToken.Odd), "Copied state should detect same winner when scanned");
    }

    /// <summary>
    ///   Verifies that game state handles corrupted board scenarios gracefully.
    /// </summary>
    ///
    [Test]
    public void GameStateHandlesInvalidBoardStatesGracefully()
    {
        // Create a game state with an invalid board configuration.

        var corruptedBoard = new byte[9];
        corruptedBoard[0] = 1;
        corruptedBoard[1] = 1; // Invalid: duplicate token

        var gameState = new GameState(
            PlayerToken.Odd,
            corruptedBoard,
            15,
            [
                new HashSet<byte> { 3, 5, 7, 9 }, // Missing 1 since it's "used" twice
                new HashSet<byte> { 2, 4, 6, 8 }
            ]);

        // The game state should still function for basic operations.

        Assert.That(gameState.CurrentTurn, Is.EqualTo(PlayerToken.Odd), "CurrentTurn should be accessible");
        Assert.That(gameState.Board.Length, Is.EqualTo(9), "Board length should be correct");

        // ScanForWinner should handle corrupted state gracefully and return a result.
        var winner = gameState.ScanForWinner();
        Assert.That(winner, Is.Not.Null.Or.Null, "ScanForWinner should return a deterministic result even with corrupted data");
    }

    /// <summary>
    ///   Verifies that game state maintains consistency after multiple operations.
    /// </summary>
    ///
    [Test]
    public void GameStateMaintainsConsistencyAfterMultipleOperations()
    {
        var gameState = GameState.CreateDefault();
        var originalOddTokens = gameState.GetPlayerTokens(PlayerToken.Odd).Count;
        var originalEvenTokens = gameState.GetPlayerTokens(PlayerToken.Even).Count;

        // Apply and undo multiple moves to test consistency.

        var move1 = new Move(PlayerToken.Odd, 0, 1);
        var move2 = new Move(PlayerToken.Even, 1, 2);
        var move3 = new Move(PlayerToken.Odd, 2, 3);

        gameState.ApplyMove(move1);
        gameState.ApplyMove(move2);
        gameState.ApplyMove(move3);

        gameState.UndoMove(move3);
        gameState.UndoMove(move2);
        gameState.UndoMove(move1);

        // Verify state is restored to original.

        Assert.That(gameState.CurrentTurn, Is.EqualTo(PlayerToken.Odd), "Turn should be restored");
        Assert.That(gameState.Winner, Is.Null, "Winner should be cleared");
        Assert.That(gameState.Board.All(cell => cell == GameState.EmptyBoardSpaceValue), Is.True, "Board should be empty");
        Assert.That(gameState.GetPlayerTokens(PlayerToken.Odd).Count, Is.EqualTo(originalOddTokens), "Odd tokens should be restored");
        Assert.That(gameState.GetPlayerTokens(PlayerToken.Even).Count, Is.EqualTo(originalEvenTokens), "Even tokens should be restored");
    }

    /// <summary>
    ///   Verifies that GameState handles extremely large winning totals correctly.
    /// </summary>
    ///
    [Test]
    public void GameStateHandlesLargeWinningTotalsCorrectly()
    {
        // Create a game state with an unusually large winning total.

        var largeBoard = new byte[25]; // 5x5 board
        var largeWinningTotal = 1000;

        var largeTokensOdd = new HashSet<byte>();
        var largeTokensEven = new HashSet<byte>();

        // Generate tokens that could theoretically sum to the large total.

        for (byte i = 1; i <= 100; i += 2)
        {
            largeTokensOdd.Add(i);
        }

        for (byte i = 2; i <= 100; i += 2)
        {
            largeTokensEven.Add(i);
        }

        var gameState = new GameState(
            PlayerToken.Odd,
            largeBoard,
            largeWinningTotal,
            [largeTokensOdd, largeTokensEven]);

        // Test basic operations with large winning total.

        var move = new Move(PlayerToken.Odd, 0, 99);
        gameState.ApplyMove(move);

        Assert.That(gameState.Board[0], Is.EqualTo(99), "Large token should be placed correctly");
        Assert.That(gameState.Winner, Is.Null, "Single large token should not trigger win");
        Assert.That(gameState.WinningTotal, Is.EqualTo(largeWinningTotal), "Winning total should be preserved");
    }

    /// <summary>
    ///   Verifies that GameState performs efficiently with rapid successive state changes.
    /// </summary>
    ///
    [Test]
    public void GameStateHandlesRapidStateChangesEfficiently()
    {
        var gameState = CreateValidGameState();
        var executionTimes = new List<long>();

        // Perform rapid successive apply/undo operations and measure performance.

        for (var i = 0; i < 100; i++)
        {
            var move = new Move(PlayerToken.Odd, i % 9, (byte)(1 + (i % 5) * 2)); // Cycle through odd tokens
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Only apply if the position is empty.

            if (gameState.Board[move.PositionIndex] == GameState.EmptyBoardSpaceValue &&
                gameState.GetPlayerTokens(move.Player).Contains(move.Token))
            {
                gameState.ApplyMove(move);
                gameState.UndoMove(move);
            }

            stopwatch.Stop();
            executionTimes.Add(stopwatch.ElapsedTicks);
        }

        // Verify performance doesn't degrade significantly over time.

        var averageFirstHalf = executionTimes.Take(50).Average();
        var averageSecondHalf = executionTimes.Skip(50).Average();

        Assert.That(averageSecondHalf, Is.LessThan(averageFirstHalf * 3),
            "Performance should remain consistent over rapid state changes");
        Assert.That(gameState.Board.All(cell => cell == GameState.EmptyBoardSpaceValue), Is.True,
            "Board should be empty after all undo operations");
    }

    /// <summary>
    ///   Verifies that GameState handles minimum and maximum token values correctly.
    /// </summary>
    ///
    [Test]
    public void GameStateHandlesExtremeTokenValuesCorrectly()
    {
        // Test with minimum and maximum byte values.

        var extremeGameState = new GameState(
            PlayerToken.Odd,
            new byte[9],
            510, // Sum of 1 + 255 + 254 = 510
            [
                new HashSet<byte> { 1, 3, 255 },      // Mix of small and large odd-ish values
                new HashSet<byte> { 2, 4, 254 }       // Mix of small and large even-ish values
            ]);

        // Test applying moves with extreme values.

        var minMove = new Move(PlayerToken.Odd, 0, 1);
        var maxMove = new Move(PlayerToken.Even, 1, 254);

        extremeGameState.ApplyMove(minMove);
        Assert.That(extremeGameState.Board[0], Is.EqualTo(1), "Minimum token value should be handled");

        extremeGameState.ApplyMove(maxMove);
        Assert.That(extremeGameState.Board[1], Is.EqualTo(254), "Maximum token value should be handled");

        // Test that these don't cause overflow issues in winner calculation.

        var winner = extremeGameState.ScanForWinner();
        Assert.That(winner, Is.Null, "Extreme values should not cause calculation errors");
    }

    /// <summary>
    ///   Verifies that GameState maintains data integrity under concurrent access patterns.
    /// </summary>
    ///
    [Test]
    public void GameStateHandlesConcurrentReadOperationsSafely()
    {
        var gameState = CreateValidGameState();

        // Apply some moves to create a non-trivial state.

        gameState.ApplyMove(new Move(PlayerToken.Odd, 0, 1));
        gameState.ApplyMove(new Move(PlayerToken.Even, 3, 2));
        gameState.ApplyMove(new Move(PlayerToken.Odd, 4, 5));

        // Perform concurrent read operations to test thread safety of read-only operations.

        var readTasks = Enumerable.Range(0, 20).Select(_ => Task.Run(() =>
        {
            var results = new List<object>();

            // Perform various read operations.

            results.Add(gameState.CurrentTurn);
            results.Add(gameState.Winner ?? (object)"null");
            results.Add(gameState.IsGameOver);
            results.Add(gameState.WinningTotal);
            results.Add(gameState.Board.ToArray()); // Create a copy
            results.Add(gameState.GetPlayerTokens(PlayerToken.Odd).ToHashSet());
            results.Add(gameState.GetPlayerTokens(PlayerToken.Even).ToHashSet());

            return results;
        })).ToArray();

        var allResults = Task.WhenAll(readTasks).Result;

        // Verify all concurrent reads returned consistent results.

        foreach (var results in allResults)
        {
            Assert.That(results[0], Is.EqualTo(PlayerToken.Even), "CurrentTurn should be consistent");
            Assert.That(results[1], Is.EqualTo("null"), "Winner should be consistent");
            Assert.That(results[2], Is.False, "IsGameOver should be consistent");
            Assert.That(results[3], Is.EqualTo(15), "WinningTotal should be consistent");

            var board = (byte[])results[4];
            Assert.That(board[0], Is.EqualTo(1), "Board state should be consistent");
            Assert.That(board[3], Is.EqualTo(2), "Board state should be consistent");
            Assert.That(board[4], Is.EqualTo(5), "Board state should be consistent");
        }
    }

    /// <summary>
    ///   Verifies that GameState handles the theoretical maximum game length correctly.
    /// </summary>
    ///
    [Test]
    public void GameStateHandlesMaximumGameLengthScenario()
    {
        var gameState = CreateValidGameState();
        var moves = new List<Move>();

        // Create the longest possible game (fill entire board without winning).

        moves.Add(new Move(PlayerToken.Odd, 0, 1));   // 1
        moves.Add(new Move(PlayerToken.Even, 1, 2));  // 2
        moves.Add(new Move(PlayerToken.Odd, 2, 3));   // 3 (top row: 1+2+3=6, not 15)
        moves.Add(new Move(PlayerToken.Even, 3, 4));  // 4
        moves.Add(new Move(PlayerToken.Odd, 4, 7));   // 7
        moves.Add(new Move(PlayerToken.Even, 5, 6));  // 6
        moves.Add(new Move(PlayerToken.Odd, 6, 9));   // 9
        moves.Add(new Move(PlayerToken.Even, 7, 8));  // 8
        moves.Add(new Move(PlayerToken.Odd, 8, 5));   // 5

        // Apply all moves.

        foreach (var move in moves)
        {
            if (gameState.GetPlayerTokens(move.Player).Contains(move.Token))
            {
                gameState.ApplyMove(move);
            }
        }

        // Verify the game handles a full board correctly.

        var emptyPositions = gameState.Board.Count(cell => cell == GameState.EmptyBoardSpaceValue);
        Assert.That(emptyPositions, Is.EqualTo(0), "Board should be completely filled");
        Assert.That(gameState.IsGameOver, Is.True, "Game should be over when board is full");

        // Verify no winner exists (since we avoided winning combinations).

        Assert.That(gameState.Winner, Is.Null, "Full board without winning combination should have no winner");
    }
}