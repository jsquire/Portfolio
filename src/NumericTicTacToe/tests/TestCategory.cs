namespace Squire.NumTic.Tests;

/// <summary>
///   The set of well-known test categories used for classifying
///   tests.
/// </summary>
///
internal static class TestCategory
{
    /// <summary>The Console category.</summary>
    public const string Console = "Console";

    /// <summary>The Game category.</summary>
    public const string Game = "Game";

    /// <summary>The model-context-protocol (MCP) category.</summary>
    public const string MCP = "MCP";

    /// <summary>The Players category.</summary>
    public const string Players = "Players";

    /// <summary>The test requires live network resources to execute.</summary>
    public const string Live = "Live";
}
