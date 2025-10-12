using System.ClientModel;
using System.ClientModel.Primitives;
using System.Reflection;
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
///   The suite of tests for the <see cref="OpenAIPlayer"/> class.
/// </summary>
///
[TestFixture]
[Category(TestCategory.Players)]
public class OpenAIPlayerTests
{
    /// <summary>
    ///   Verifies functionality of the OpenAIPlayer constructor.
    /// </summary>
    ///
    [Test]
    public void ConstructorWithNullGameInterfaceThrows()
    {
        var mockClient = Substitute.For<OpenAIClient>();
        var gameState = GameState.CreateDefault();

        Assert.That(() => new OpenAIPlayer(null!, mockClient, gameState),
            Throws.InstanceOf<ArgumentNullException>().With.Property("ParamName").EqualTo("gameInterface"),
            "Constructor should throw ArgumentNullException for null gameInterface");
    }

    /// <summary>
    ///   Verifies functionality of the OpenAIPlayer constructor.
    /// </summary>
    ///
    [Test]
    public void ConstructorWithNullOpenAIClientThrows()
    {
        var mockGameInterface = Substitute.For<IGameInterface>();
        var gameState = GameState.CreateDefault();

        Assert.That(() => new OpenAIPlayer(mockGameInterface, null!, gameState),
            Throws.InstanceOf<ArgumentNullException>().With.Property("ParamName").EqualTo("openAIClient"),
            "Constructor should throw ArgumentNullException for null openAIClient");
    }

    /// <summary>
    ///   Verifies functionality of the OpenAIPlayer constructor.
    /// </summary>
    ///
    [Test]
    public void ConstructorWithNullGameStateThrows()
    {
        var mockGameInterface = Substitute.For<IGameInterface>();
        var mockClient = Substitute.For<OpenAIClient>();

        Assert.That(() => new OpenAIPlayer(mockGameInterface, mockClient, null!),
            Throws.InstanceOf<ArgumentNullException>().With.Property("ParamName").EqualTo("gameState"),
            "Constructor should throw ArgumentNullException for null gameState");
    }

    /// <summary>
    ///   Verifies functionality of the OpenAIPlayer constructor.
    /// </summary>
    ///
    [Test]
    public void ConstructorClonesProvidedOptions()
    {
        var mockGameInterface = Substitute.For<IGameInterface>();
        var mockClient = CreateMockOpenAIClient();
        var gameState = GameState.CreateDefault();
        var originalOptions = new OpenAIPlayerOptions { Difficulty = Difficulty.Hard, ModelName = "gpt-4o" };
        var player = new OpenAIPlayer(mockGameInterface, mockClient, gameState, originalOptions);

        // Use reflection to access the private Options field.

        var optionsField = typeof(OpenAIPlayer).GetField("Options", BindingFlags.NonPublic | BindingFlags.Instance);
        var storedOptions = (OpenAIPlayerOptions)optionsField!.GetValue(player)!;

        Assert.That(storedOptions, Is.Not.SameAs(originalOptions), "Options should be cloned, not stored directly");
        Assert.That(storedOptions.Difficulty, Is.EqualTo(originalOptions.Difficulty), "Cloned options should preserve Difficulty");
        Assert.That(storedOptions.ModelName, Is.EqualTo(originalOptions.ModelName), "Cloned options should preserve ModelName");
    }

    /// <summary>
    ///   Verifies functionality of the OpenAIPlayer constructor.
    /// </summary>
    ///
    [Test]
    public void ConstructorUsesDefaultOptionsWhenNoneProvided()
    {
        var mockGameInterface = Substitute.For<IGameInterface>();
        var mockClient = CreateMockOpenAIClient();
        var gameState = GameState.CreateDefault();
        var player = new OpenAIPlayer(mockGameInterface, mockClient, gameState);

        // Use reflection to access the private Options field.

        var optionsField = typeof(OpenAIPlayer).GetField("Options", BindingFlags.NonPublic | BindingFlags.Instance);
        var storedOptions = (OpenAIPlayerOptions)optionsField!.GetValue(player)!;

        Assert.That(storedOptions.Difficulty, Is.EqualTo(Difficulty.Perfect), "Default options should have Perfect difficulty");
    }

    /// <summary>
    ///   Verifies functionality of the PlayTurnAsync method.
    /// </summary>
    ///
    [Test]
    public async Task PlayTurnAsyncWithNullGameStateThrows()
    {
        var mockGameInterface = Substitute.For<IGameInterface>();
        var mockClient = CreateMockOpenAIClient();
        var gameState = GameState.CreateDefault();
        var player = new OpenAIPlayer(mockGameInterface, mockClient, gameState);

        await Assert.ThatAsync(async () => await player.PlayTurnAsync(null!),
            Throws.InstanceOf<ArgumentNullException>().With.Property("ParamName").EqualTo("gameState"),
            "PlayTurnAsync should throw ArgumentNullException for null gameState");
    }

    /// <summary>
    ///   Verifies functionality of the PlayTurnAsync method.
    /// </summary>
    ///
    [Test]
    public async Task PlayTurnAsyncRespectsCancellation()
    {
        var mockGameInterface = Substitute.For<IGameInterface>();
        var mockClient = CreateMockOpenAIClient();
        var gameState = GameState.CreateDefault();
        var player = new OpenAIPlayer(mockGameInterface, mockClient, gameState);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThatAsync(async () => await player.PlayTurnAsync(gameState, cts.Token),
            Throws.InstanceOf<OperationCanceledException>(),
            "PlayTurnAsync should respect cancellation token");
    }

    /// <summary>
    ///   Verifies functionality of the PlayTurnAsync method.
    /// </summary>
    ///
    [Test]
    public async Task PlayTurnAsyncReturnsValidMoveForValidResponse()
    {
        var mockGameInterface = Substitute.For<IGameInterface>();
        var mockResponseClient = Substitute.For<OpenAIResponseClient>();
        var mockClient = CreateMockOpenAIClient(mockResponseClient);
        var gameState = GameState.CreateDefault();
        var player = new OpenAIPlayer(mockGameInterface, mockClient, gameState);

        // Set up expected values for the move.

        var expectedPosition = 0;
        var expectedToken = (byte)1;
        var expectedRandom = false;

        // Set up mock to return valid JSON response.

        var validJsonResponse = $$"""{"position": {{expectedPosition}}, "token": {{expectedToken}}, "random": {{expectedRandom.ToString().ToLowerInvariant()}}}""";
        var mockResponse = CreateMockResponse(validJsonResponse);

        mockResponseClient
            .CreateResponseAsync(
                Arg.Any<IEnumerable<ResponseItem>>(),
                Arg.Any<ResponseCreationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ClientResult.FromValue(mockResponse, Substitute.For<PipelineResponse>())));

        var move = await player.PlayTurnAsync(gameState);

        Assert.That(move.Player, Is.EqualTo(PlayerToken.Odd), "Move should be for Odd player");
        Assert.That(move.PositionIndex, Is.EqualTo(expectedPosition), "Move should be at expected position");
        Assert.That(move.Token, Is.EqualTo(expectedToken), "Move should use expected token");
    }

    /// <summary>
    ///   Verifies functionality of the PlayTurnAsync method.
    /// </summary>
    ///
    [Test]
    public async Task PlayTurnAsyncRetriesOnInvalidPosition()
    {
        var mockGameInterface = Substitute.For<IGameInterface>();
        var mockResponseClient = Substitute.For<OpenAIResponseClient>();
        var mockClient = CreateMockOpenAIClient(mockResponseClient);
        var gameState = GameState.CreateDefault();
        var player = new OpenAIPlayer(mockGameInterface, mockClient, gameState);
        var callCount = 0;

        // First response has invalid position, second response is valid.

        var invalidResponse = """{"position": 99, "token": 1, "random": false}""";
        var validResponse = """{"position": 0, "token": 1, "random": false}""";

        mockResponseClient
            .CreateResponseAsync(
                Arg.Any<IEnumerable<ResponseItem>>(),
                Arg.Any<ResponseCreationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                ++callCount;
                var response = callCount == 1
                    ? CreateMockResponse(invalidResponse)
                    : CreateMockResponse(validResponse);
                return Task.FromResult(ClientResult.FromValue(response, Substitute.For<PipelineResponse>()));
            });

        var move = await player.PlayTurnAsync(gameState);

        Assert.That(callCount, Is.EqualTo(2), "Should retry after invalid position");
        Assert.That(move.PositionIndex, Is.EqualTo(0), "Should return valid move after retry");
    }

    /// <summary>
    ///   Verifies functionality of the PlayTurnAsync method.
    /// </summary>
    ///
    [Test]
    public async Task PlayTurnAsyncRetriesOnOccupiedPosition()
    {
        var mockGameInterface = Substitute.For<IGameInterface>();
        var mockResponseClient = Substitute.For<OpenAIResponseClient>();
        var mockClient = CreateMockOpenAIClient(mockResponseClient);
        var gameState = GameState.CreateDefault();
        var player = new OpenAIPlayer(mockGameInterface, mockClient, gameState);
        var callCount = 0;

        // Occupy position 0.

        gameState.ApplyMove(new Move(PlayerToken.Odd, 0, 1));

        // First response tries occupied position, second response uses empty position.

        var occupiedResponse = """{"position": 0, "token": 2, "random": false}""";
        var validResponse = """{"position": 3, "token": 2, "random": false}""";

        mockResponseClient
            .CreateResponseAsync(
                Arg.Any<IEnumerable<ResponseItem>>(),
                Arg.Any<ResponseCreationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                ++callCount;

                var response = callCount == 1
                    ? CreateMockResponse(occupiedResponse)
                    : CreateMockResponse(validResponse);

                return Task.FromResult(ClientResult.FromValue(response, Substitute.For<PipelineResponse>()));
            });

        var move = await player.PlayTurnAsync(gameState);

        Assert.That(callCount, Is.EqualTo(2), "Should retry after attempting occupied position");
        Assert.That(move.PositionIndex, Is.EqualTo(3), "Should return valid move at empty position");
    }

    /// <summary>
    ///   Verifies functionality of the PlayTurnAsync method.
    /// </summary>
    ///
    [Test]
    public async Task PlayTurnAsyncRetriesOnUnavailableToken()
    {
        var mockGameInterface = Substitute.For<IGameInterface>();
        var mockResponseClient = Substitute.For<OpenAIResponseClient>();
        var mockClient = CreateMockOpenAIClient(mockResponseClient);
        var gameState = GameState.CreateDefault();
        var player = new OpenAIPlayer(mockGameInterface, mockClient, gameState);
        var callCount = 0;

        // Use token 1 so it's unavailable.

        gameState.ApplyMove(new Move(PlayerToken.Odd, 0, 1));

        // First response tries unavailable token, second response uses available token.

        var unavailableTokenResponse = """{"position": 3, "token": 1, "random": false}""";
        var validResponse = """{"position": 3, "token": 2, "random": false}""";

        mockResponseClient
            .CreateResponseAsync(
                Arg.Any<IEnumerable<ResponseItem>>(),
                Arg.Any<ResponseCreationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                ++callCount;

                var response = callCount == 1
                    ? CreateMockResponse(unavailableTokenResponse)
                    : CreateMockResponse(validResponse);

                return Task.FromResult(ClientResult.FromValue(response, Substitute.For<PipelineResponse>()));
            });

        var move = await player.PlayTurnAsync(gameState);

        Assert.That(callCount, Is.EqualTo(2), "Should retry after attempting unavailable token");
        Assert.That(move.Token, Is.EqualTo(2), "Should return valid move with available token");
    }

    /// <summary>
    ///   Verifies functionality of the PlayTurnAsync method.
    /// </summary>
    ///
    [Test]
    public async Task PlayTurnAsyncRetriesOnMalformedJson()
    {
        var mockGameInterface = Substitute.For<IGameInterface>();
        var mockResponseClient = Substitute.For<OpenAIResponseClient>();
        var mockClient = CreateMockOpenAIClient(mockResponseClient);
        var gameState = GameState.CreateDefault();
        var player = new OpenAIPlayer(mockGameInterface, mockClient, gameState);
        var callCount = 0;

        // First response is malformed JSON, second response is valid.

        var malformedResponse = """{"position": 0, "token": 1""";
        var validResponse = """{"position": 0, "token": 1, "random": false}""";

        mockResponseClient
            .CreateResponseAsync(
                Arg.Any<IEnumerable<ResponseItem>>(),
                Arg.Any<ResponseCreationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                ++callCount;

                var response = callCount == 1
                    ? CreateMockResponse(malformedResponse)
                    : CreateMockResponse(validResponse);

                return Task.FromResult(ClientResult.FromValue(response, Substitute.For<PipelineResponse>()));
            });

        var move = await player.PlayTurnAsync(gameState);

        Assert.That(callCount, Is.EqualTo(2), "Should retry after malformed JSON");
        Assert.That(move.PositionIndex, Is.EqualTo(0), "Should return valid move after retry");
    }

    /// <summary>
    ///   Verifies functionality of the PlayTurnAsync method.
    /// </summary>
    ///
    [Test]
    public async Task PlayTurnAsyncWithMaxRetriesExceededThrows()
    {
        var mockGameInterface = Substitute.For<IGameInterface>();
        var mockResponseClient = Substitute.For<OpenAIResponseClient>();
        var mockClient = CreateMockOpenAIClient(mockResponseClient);
        var gameState = GameState.CreateDefault();
        var options = new OpenAIPlayerOptions { MaxMoveRetries = 3 };
        var player = new OpenAIPlayer(mockGameInterface, mockClient, gameState, options);

        // Always return invalid response.

        var invalidResponse = """{"position": 99, "token": 1, "random": false}""";

        mockResponseClient
            .CreateResponseAsync(
                Arg.Any<IEnumerable<ResponseItem>>(),
                Arg.Any<ResponseCreationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ClientResult.FromValue(
                CreateMockResponse(invalidResponse),
                Substitute.For<PipelineResponse>())));

        await Assert.ThatAsync(async () => await player.PlayTurnAsync(gameState),
            Throws.InstanceOf<InvalidOperationException>().With.Message.Contains("Failed to get valid move after"),
            "Should throw InvalidOperationException after max retries");
    }

    /// <summary>
    ///   Verifies functionality of the PlayTurnAsync method.
    /// </summary>
    ///
    [Test]
    public async Task PlayTurnAsyncTracksRandomMoveCounter()
    {
        var mockGameInterface = Substitute.For<IGameInterface>();
        var mockResponseClient = Substitute.For<OpenAIResponseClient>();
        var mockClient = CreateMockOpenAIClient(mockResponseClient);
        var gameState = GameState.CreateDefault();
        var player = new OpenAIPlayer(mockGameInterface, mockClient, gameState);

        var capturedMessages = new List<string>();
        var callCount = 0;

        // Set up three valid moves alternating between players.
        // Odd tokens: 1,3,5,7,9  Even tokens: 2,4,6,8

        var strategicResponse1 = """{"position": 0, "token": 1, "random": false}""";  // Odd plays
        var randomResponse = """{"position": 1, "token": 2, "random": true}""";       // Even plays (random)
        var strategicResponse2 = """{"position": 2, "token": 3, "random": false}""";  // Odd plays

        mockResponseClient
            .CreateResponseAsync(
                Arg.Do<IEnumerable<ResponseItem>>(items =>
                {
                    // Capture the user message text from the request.

                    var userMessage = items.OfType<MessageResponseItem>().LastOrDefault();

                    if (userMessage != null)
                    {
                        var contentProp = userMessage.GetType().GetProperty("Content");
                        var content = contentProp?.GetValue(userMessage) as IReadOnlyList<ResponseContentPart>;
                        var text = content?.FirstOrDefault()?.Text;

                        if (text != null)
                        {
                            capturedMessages.Add(text);
                        }
                    }
                }),
                Arg.Any<ResponseCreationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                ++callCount;

                var json = callCount switch
                {
                    1 => strategicResponse1,
                    2 => randomResponse,
                    _ => strategicResponse2
                };

                return Task.FromResult(ClientResult.FromValue(
                    CreateMockResponse(json),
                    Substitute.For<PipelineResponse>()));
            });

        // First move - counter starts at 0.

        var move1 = await player.PlayTurnAsync(gameState);
        gameState.ApplyMove(move1);

        // Second move - counter should be 1, then reset to 0 after random move.

        var move2 = await player.PlayTurnAsync(gameState);
        gameState.ApplyMove(move2);

        // Third move - counter should be 0 (immediately after random move).

        var move3 = await player.PlayTurnAsync(gameState);
        gameState.ApplyMove(move3);

        // Verify the messages sent to the OpenAI client contain correct counter values.

        Assert.That(capturedMessages.Count, Is.EqualTo(3), "Should have captured 3 user messages");
        Assert.That(capturedMessages[0], Does.Contain("Moves since last random: 0"), "First move should show counter = 0");
        Assert.That(capturedMessages[1], Does.Contain("Moves since last random: 1"), "Second move should show counter = 1");
        Assert.That(capturedMessages[2], Does.Contain("Moves since last random: 0"), "Third move should show counter = 0 after random reset");
    }

    /// <summary>
    ///   Creates a mock OpenAI client for testing.
    /// </summary>
    ///
    private static OpenAIClient CreateMockOpenAIClient(OpenAIResponseClient? responseClient = null)
    {
        var mockClient = Substitute.For<OpenAIClient>();
        var mockResponseClient = responseClient ?? Substitute.For<OpenAIResponseClient>();

        mockClient.GetOpenAIResponseClient(Arg.Any<string>()).Returns(mockResponseClient);
        return mockClient;
    }

    /// <summary>
    ///   Creates a mock OpenAI response with the specified output text.
    /// </summary>
    ///
    private static OpenAIResponse CreateMockResponse(string outputText)
    {
        // Create a message item with output text content.

        var messageItem = ResponseItem.CreateAssistantMessageItem(outputText);
        var outputItems = new List<ResponseItem> { messageItem };

        return OpenAIResponsesModelFactory.OpenAIResponse(
            id: "test-response",
            status: ResponseStatus.Completed,
            outputItems: outputItems);
    }
}

#pragma warning restore SCME0001
#pragma warning restore OPENAI001
