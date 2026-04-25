using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text.Json;
using Azure.Identity;
using NSubstitute;
using NUnit.Framework;
using OpenAI;
using OpenAI.Responses;
using Squire.NumTic.AI;
using Squire.NumTic.Contracts;
using Squire.NumTic.Players;

namespace Squire.NumTic.Tests;

// OpenAI features are mostly still in an experimental state and require opt-in by disabling warnings.

#pragma warning disable OPENAI001
#pragma warning disable SCME0001

/// <summary>
///   The suite of live tests for the <see cref="OpenAIPlayer"/> class.
/// </summary>
///
[TestFixture]
[Category(TestCategory.Live)]
[Category(TestCategory.Players)]
public class OpenAIPlayerLiveTests
{
    /// <summary>The default set of player options for live testing.</summary>
    private static readonly OpenAIPlayerOptions DefaultPlayerOptions = new ()
    {
         ModelName = TestEnvironment.AzureOpenAIModelName
            ?? throw new InvalidOperationException("Azure OpenAI model name is not configured in the test environment.")
    };

    /// <summary>
    ///   Verifies functionality of the PlayTurnAsync method with a live OpenAI client.
    /// </summary>
    ///
    [LiveTest]
    public async Task PlayTurnAsyncReturnsValidMoveForEmptyBoard()
    {
        var mockGameInterface = Substitute.For<IGameInterface>();
        var client = CreateClient();
        var gameState = GameState.CreateDefault();
        var player = new OpenAIPlayer(mockGameInterface, client, gameState, DefaultPlayerOptions);
        var move = await player.PlayTurnAsync(gameState);

        Assert.That(move.Player, Is.EqualTo(PlayerToken.Odd), "Move should be for Odd player");
        Assert.That(move.PositionIndex, Is.InRange(0, 8), "Position should be within valid board range");
        Assert.That(move.Token % 2, Is.EqualTo(1), "Token should be odd");
        Assert.That(move.Token, Is.InRange((byte)1, (byte)9), "Token should be in valid range");
        Assert.That(gameState.Board[move.PositionIndex], Is.EqualTo(GameState.EmptyBoardSpaceValue), "Selected position should be empty");
        Assert.That(gameState.CurrentPlayerTokens.Contains(move.Token), Is.True, "Token should be from player's available set");
    }

    /// <summary>
    ///   Verifies functionality of the PlayTurnAsync method with a live OpenAI client.
    /// </summary>
    ///
    [LiveTest]
    public async Task PlayTurnAsyncReturnsValidMoveForPartiallyFilledBoard()
    {
        var mockGameInterface = Substitute.For<IGameInterface>();
        var client = CreateClient();
        var gameState = GameState.CreateDefault();

        // Apply some moves to create a partially filled board.

        gameState.ApplyMove(new Move(PlayerToken.Odd, 0, 1));
        gameState.ApplyMove(new Move(PlayerToken.Even, 4, 2));
        gameState.ApplyMove(new Move(PlayerToken.Odd, 8, 3));

        var player = new OpenAIPlayer(mockGameInterface, client, gameState, DefaultPlayerOptions);
        var move = await player.PlayTurnAsync(gameState);

        Assert.That(move.Player, Is.EqualTo(PlayerToken.Even), "Move should be for Even player");
        Assert.That(move.PositionIndex, Is.InRange(0, 8), "Position should be within valid board range");
        Assert.That(gameState.Board[move.PositionIndex], Is.EqualTo(GameState.EmptyBoardSpaceValue), "Selected position should be empty");
        Assert.That(move.Token % 2, Is.EqualTo(0), "Token should be even");
        Assert.That(gameState.CurrentPlayerTokens.Contains(move.Token), Is.True, "Token should be from player's available set");
    }

    /// <summary>
    ///   Verifies functionality of the PlayTurnAsync method with a live OpenAI client.
    /// </summary>
    ///
    [LiveTest]
    public async Task PlayTurnAsyncReturnsValidMoveForNearlyFullBoard()
    {
        var mockGameInterface = Substitute.For<IGameInterface>();
        var client = CreateClient();
        var gameState = GameState.CreateDefault();

        // Fill most of the board, leaving only positions 7 and 8 empty.
        // The sequence is chosen to avoid creating a winning line (sum of 15).

        gameState.ApplyMove(new Move(PlayerToken.Odd, 0, 1));
        gameState.ApplyMove(new Move(PlayerToken.Even, 1, 2));
        gameState.ApplyMove(new Move(PlayerToken.Odd, 2, 9));
        gameState.ApplyMove(new Move(PlayerToken.Even, 3, 4));
        gameState.ApplyMove(new Move(PlayerToken.Odd, 4, 3));
        gameState.ApplyMove(new Move(PlayerToken.Even, 5, 6));
        gameState.ApplyMove(new Move(PlayerToken.Odd, 6, 5));

        var player = new OpenAIPlayer(mockGameInterface, client, gameState, DefaultPlayerOptions);
        var move = await player.PlayTurnAsync(gameState);

        Assert.That(move.Player, Is.EqualTo(PlayerToken.Even), "Move should be for Even player");
        Assert.That(move.PositionIndex, Is.AnyOf(7, 8), "Position should be one of the remaining empty positions");
        Assert.That(gameState.Board[move.PositionIndex], Is.EqualTo(GameState.EmptyBoardSpaceValue), "Selected position should be empty");
        Assert.That(move.Token, Is.EqualTo((byte)8), "Token should be 8 (only remaining even token)");
    }

    /// <summary>
    ///   Verifies that randomization frequency decreases as difficulty level increases.
    /// </summary>
    ///
    /// <remarks>
    ///   This test plays multiple games at each difficulty level and captures the actual
    ///   responses from the OpenAI model to count how often random moves are made.
    ///   The assertion validates that easier difficulties produce more random moves than
    ///   harder difficulties, with Perfect difficulty producing zero random moves.
    /// </remarks>
    ///
    [LiveTest]
    public async Task RandomizationFrequencyDecreasesWithHigherDifficulty()
    {
        var mockGameInterface = Substitute.For<IGameInterface>();
        var randomCountsByDifficulty = new Dictionary<Difficulty, int>();

        foreach (var difficulty in Enum.GetValues<Difficulty>())
        {
            var capturedResponses = new List<string>();
            var client = CreateCapturingClient(capturedResponses);
            var gameState = GameState.CreateDefault();

            var options = new OpenAIPlayerOptions
            {
                ModelName = DefaultPlayerOptions.ModelName,
                Difficulty = difficulty
            };

            // Create players for both sides with the same difficulty.

            var oddPlayer = new OpenAIPlayer(mockGameInterface, client, gameState, options);
            var evenPlayer = new OpenAIPlayer(mockGameInterface, client, gameState, options);

            // Play until the game ends.

            while (!gameState.IsGameOver)
            {
                var currentPlayer = gameState.CurrentTurn == PlayerToken.Odd ? oddPlayer : evenPlayer;
                var move = await currentPlayer.PlayTurnAsync(gameState);

                gameState.ApplyMove(move);
            }

            // Count random moves from captured responses.

            var randomCount = capturedResponses.Count(response =>
            {
                try
                {
                    using var doc = JsonDocument.Parse(response);

                    return doc.RootElement.TryGetProperty("random", out var randomProp)
                        && randomProp.GetBoolean();
                }
                catch
                {
                    return false;
                }
            });

            randomCountsByDifficulty[difficulty] = randomCount;
        }

        // Assert that randomization frequency decreases with higher difficulty.
        // Each difficulty level should have at least as many random moves as the next harder level.

        var difficulties = Enum.GetValues<Difficulty>();

        for (var index = 1; index < difficulties.Length; ++index)
        {
            var current = difficulties[index];
            var previous = difficulties[index - 1];

            Assert.That(randomCountsByDifficulty[previous], Is.GreaterThanOrEqualTo(randomCountsByDifficulty[current]),
                $"{previous} ({randomCountsByDifficulty[previous]}) should have at least as many random moves as {current} ({randomCountsByDifficulty[current]})");
        }

        // Perfect difficulty should have exactly zero random moves.

        Assert.That(randomCountsByDifficulty[Difficulty.Perfect], Is.EqualTo(0), "Perfect difficulty should never produce random moves");
    }

    /// <summary>
    ///   Verifies functionality of the PlayTurnAsync method with a live OpenAI client.
    /// </summary>
    ///
    [LiveTest]
    public void PlayTurnAsyncRespectsCancellationWhenAlreadyCancelled()
    {
        var mockGameInterface = Substitute.For<IGameInterface>();
        var client = CreateClient();
        var gameState = GameState.CreateDefault();
        var player = new OpenAIPlayer(mockGameInterface, client, gameState, DefaultPlayerOptions);

        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        Assert.That(async () => await player.PlayTurnAsync(gameState, cancellationSource.Token),
            Throws.InstanceOf<OperationCanceledException>(),
            "PlayTurnAsync should throw OperationCanceledException for already cancelled token");
    }

    /// <summary>
    ///   Verifies that the OpenAIPlayer can complete a full game across all difficulty levels.
    /// </summary>
    ///
    [LiveTest]
    public async Task FullGameCompletesSuccessfullyForAllDifficulties()
    {
        var mockGameInterface = Substitute.For<IGameInterface>();

        foreach (var difficulty in Enum.GetValues<Difficulty>())
        {
            var client = CreateClient();
            var gameState = GameState.CreateDefault();
            var options = new OpenAIPlayerOptions
            {
                ModelName = DefaultPlayerOptions.ModelName,
                Difficulty = difficulty
            };

            // OpenAI player plays as Odd, BotPlayer plays as Even.

            var openAIPlayer = new OpenAIPlayer(mockGameInterface, client, gameState, options);
            var botPlayer = new BotPlayer(mockGameInterface, new BotPlayerOptions { Difficulty = difficulty });

            // Play until the game ends.

            while (!gameState.IsGameOver)
            {
                var move = gameState.CurrentTurn switch
                {
                    PlayerToken.Odd => await openAIPlayer.PlayTurnAsync(gameState),
                    PlayerToken.Even => await botPlayer.PlayTurnAsync(gameState),
                    _ => throw new InvalidOperationException("Unknown player token.")
                };

                gameState.ApplyMove(move);
            }

            // Verify the game reached a terminal state.

            Assert.That(gameState.IsGameOver, Is.True, $"Game should reach terminal state for {difficulty} difficulty");
        }
    }

    /// <summary>
    ///   Creates a <see cref="ResponsesClient"/> for testing using <see cref="DefaultAzureCredential"/>
    ///   for authorization.
    /// </summary>
    ///
    /// <returns>The <see cref="ResponsesClient"/> instance for testing.</returns>
    ///
    /// <exception cref="InvalidOperationException">Occurs when the test environment is not configured with an Azure OpenAI endpoint.</exception>
    /// <exception cref="InvalidOperationException">Occurs when the test environment is not configured with an Azure OpenAI authorization scope.</exception>
    ///
    private static ResponsesClient CreateClient()
    {
        var endpoint = TestEnvironment.AzureOpenAIEndpoint ?? throw new InvalidOperationException("Azure OpenAI endpoint is not configured in the test environment.");

        // Visual Studio and Visual Studio Code credentials can be problematic in mixed identity cases,
        // so they're being excluded here to avoid potential issues.

        var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            ExcludeVisualStudioCredential = true,
            ExcludeVisualStudioCodeCredential = true
        });

        var authScope = TestEnvironment.AzureOpenAIAuthorizationScope ?? throw new InvalidOperationException("Azure OpenAI authorization scope is not configured in the test environment.");
        var policy = new BearerTokenPolicy(credential, authScope);

        var options = new OpenAIClientOptions
        {
            Endpoint = endpoint
        };

        return new ResponsesClient(policy, options);
    }

    /// <summary>
    ///   Creates a <see cref="ResponsesClient"/> that proxies to a real client but captures
    ///   all response text for inspection.
    /// </summary>
    ///
    /// <param name="capturedResponses">
    ///   The list that will be populated with captured response texts as calls are made.
    /// </param>
    ///
    /// <returns>The mock <see cref="ResponsesClient"/> instance that proxies to the real client.</returns>
    ///
    private static ResponsesClient CreateCapturingClient(List<string> capturedResponses)
    {
        var realClient = CreateClient();
        var mockClient = Substitute.For<ResponsesClient>();

        mockClient
            .CreateResponseAsync(
                Arg.Any<CreateResponseOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                var options = callInfo.ArgAt<CreateResponseOptions>(0);
                var cancellationToken = callInfo.ArgAt<CancellationToken>(1);

                var result = await realClient.CreateResponseAsync(options, cancellationToken);
                capturedResponses.Add(result.Value.GetOutputText());

                return result;
            });

        return mockClient;
    }
}
