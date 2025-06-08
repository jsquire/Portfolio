using NSubstitute;
using NUnit.Framework;
using Squire.NumTic.Game;
using Squire.NumTic.Game.Contracts;

namespace NumTic.Tests;

/// <summary>
///   Tests for the <see cref="Game"/> class.
/// </summary>
///
[TestFixture]
public class GameTests
{
    /// <summary>
    ///   Verifies that the default constructor creates a game with correct initial state.
    /// </summary>
    ///
    [Test]
    public void DefaultConstructorCreatesGameWithCorrectInitialState()
    {
        var oddPlayer = Substitute.For<IPlayer>();
        var evenPlayer = Substitute.For<IPlayer>();
        var renderer = Substitute.For<IRenderer>();

        Assert.That(() => new Game(oddPlayer, evenPlayer, renderer), Throws.Nothing);
    }

    /// <summary>
    ///   Verifies that the constructor with GameState parameter works correctly.
    /// </summary>
    ///
    [Test]
    public void ConstructorWithGameStateCreatesGameSuccessfully()
    {
        var gameState = new GameState(
            PlayerToken.Odd,
            new int[9],
            15,
            [
                new HashSet<byte> { 1, 3, 5, 7, 9 },
                new HashSet<byte> { 2, 4, 6, 8 }
            ]);

        var oddPlayer = Substitute.For<IPlayer>();
        var evenPlayer = Substitute.For<IPlayer>();
        var renderer = Substitute.For<IRenderer>();

        Assert.That(() => new Game(oddPlayer, evenPlayer, renderer, gameState), Throws.Nothing);
    }

    /// <summary>
    ///   Verifies that the constructor throws ArgumentNullException when oddPlayer is null.
    /// </summary>
    ///
    [Test]
    public void ConstructorThrowsArgumentNullExceptionWhenOddPlayerIsNull()
    {
        var evenPlayer = Substitute.For<IPlayer>();
        var renderer = Substitute.For<IRenderer>();
        var state = GameState.CreateDefault();

        Assert.That(() => new Game(null!, evenPlayer, renderer, state),
            Throws.InstanceOf<ArgumentNullException>().With.Property("ParamName").EqualTo("oddPlayer"),
            "Constructor should throw ArgumentNullException for null oddPlayer");
    }

    /// <summary>
    ///   Verifies that the constructor throws ArgumentNullException when evenPlayer is null.
    /// </summary>
    ///
    [Test]
    public void ConstructorThrowsArgumentNullExceptionWhenEvenPlayerIsNull()
    {
        var oddPlayer = Substitute.For<IPlayer>();
        var renderer = Substitute.For<IRenderer>();
        var state = GameState.CreateDefault();

        Assert.That(() => new Game(oddPlayer, null!, renderer, state),
            Throws.InstanceOf<ArgumentNullException>().With.Property("ParamName").EqualTo("evenPlayer"),
            "Constructor should throw ArgumentNullException for null evenPlayer");
    }

    /// <summary>
    ///   Verifies that the constructor throws ArgumentNullException when renderer is null.
    /// </summary>
    ///
    [Test]
    public void ConstructorThrowsArgumentNullExceptionWhenRendererIsNull()
    {
        var oddPlayer = Substitute.For<IPlayer>();
        var evenPlayer = Substitute.For<IPlayer>();
        var state = GameState.CreateDefault();

        Assert.That(() => new Game(oddPlayer, evenPlayer, null!, state),
            Throws.InstanceOf<ArgumentNullException>().With.Property("ParamName").EqualTo("gameRenderer"),
            "Constructor should throw ArgumentNullException for null renderer");
    }

    /// <summary>
    ///   Verifies that the constructor throws ArgumentNullException when state is null.
    /// </summary>
    ///
    [Test]
    public void ConstructorThrowsArgumentNullExceptionWhenStateIsNull()
    {
        var oddPlayer = Substitute.For<IPlayer>();
        var evenPlayer = Substitute.For<IPlayer>();
        var renderer = Substitute.For<IRenderer>();

        Assert.That(() => new Game(oddPlayer, evenPlayer, renderer, null!),
            Throws.InstanceOf<ArgumentNullException>().With.Property("ParamName").EqualTo("state"),
            "Constructor should throw ArgumentNullException for null state");
    }
}
