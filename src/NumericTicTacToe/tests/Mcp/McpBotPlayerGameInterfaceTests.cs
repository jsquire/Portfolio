using NUnit.Framework;
using Squire.NumTic.Mcp;

namespace Squire.NumTic.Tests;

/// <summary>
///   The suite of tests for the <see cref="McpBotPlayerGameInterface"/> class.
/// </summary>
///
[TestFixture]
[Category(TestCategory.MCP)]
public class McpBotPlayerGameInterfaceTests
{
    /// <summary>
    ///   Verifies functionality of the RenderAsync method.
    /// </summary>
    ///
    [Test]
    public void RenderAsyncThrowsNotSupported()
    {
        var gameInterface = new McpBotPlayerGameInterface();
        var gameState = GameState.CreateDefault();

        Assert.That(async () => await gameInterface.RenderAsync(gameState),
            Throws.InstanceOf<NotSupportedException>()
                .With.Message.Contain("BotPlayer should never attempt to render in the MCP tool context"));
    }

    /// <summary>
    ///   Verifies functionality of the RenderPlayerTextAsync method.
    /// </summary>
    ///
    [Test]
    public void RenderPlayerTextAsyncConvertsErrorToInvalidOperation()
    {
        var gameInterface = new McpBotPlayerGameInterface();
        const string errorMessage = "Test error message";

        Assert.That(async () => await gameInterface.RenderPlayerTextAsync(TextType.Error, errorMessage),
            Throws.InstanceOf<InvalidOperationException>()
                .With.Message.Contain("BotPlayer error: Test error message"));
    }

    /// <summary>
    ///   Verifies functionality of the RenderPlayerTextAsync method.
    /// </summary>
    ///
    [Test]
    [TestCaseSource(nameof(GetNonErrorTextTypes))]
    public void RenderPlayerTextAsyncThrowsNotSupportedForNonError(TextType textType)
    {
        var gameInterface = new McpBotPlayerGameInterface();

        Assert.That(async () => await gameInterface.RenderPlayerTextAsync(textType, "test"),
            Throws.InstanceOf<NotSupportedException>()
                .With.Message.Contain("BotPlayer should never render non-error text in the MCP tool context"));
    }

    /// <summary>
    ///   Provides all non-error text types for testing.
    /// </summary>
    ///
    private static IEnumerable<TextType> GetNonErrorTextTypes()
    {
        return Enum.GetValues<TextType>().Where(t => t != TextType.Error);
    }

    /// <summary>
    ///   Verifies functionality of the RenderPlayerTextAsync method.
    /// </summary>
    ///
    [Test]
    public void RenderPlayerTextAsyncThrowsArgumentNullForNullText()
    {
        var gameInterface = new McpBotPlayerGameInterface();

        Assert.That(async () => await gameInterface.RenderPlayerTextAsync(TextType.Error, null!),
            Throws.ArgumentNullException.With.Property("ParamName").EqualTo("text"));
    }

    /// <summary>
    ///   Verifies functionality of the ReadPlayerResponseAsnyc method.
    /// </summary>
    ///
    [Test]
    public void ReadPlayerResponseAsyncThrowsNotSupported()
    {
        var gameInterface = new McpBotPlayerGameInterface();

        Assert.That(async () => await gameInterface.ReadPlayerResponseAsnyc(),
            Throws.InstanceOf<NotSupportedException>()
                .With.Message.Contain("BotPlayer should never request user input in the MCP tool context"));
    }

    /// <summary>
    ///   Verifies functionality of the RenderPlayerTextAsync method.
    /// </summary>
    ///
    [Test]
    public void RenderPlayerTextAsyncRespectsCancellationToken()
    {
        var gameInterface = new McpBotPlayerGameInterface();
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        Assert.That(async () => await gameInterface.RenderPlayerTextAsync(TextType.Error, "test", cancellationTokenSource.Token),
            Throws.InstanceOf<OperationCanceledException>());
    }
}