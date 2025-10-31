using NUnit.Framework;
using Squire.NumTic.Players;

namespace Squire.NumTic.Tests;

/// <summary>
///   The suite of tests for the <see cref="BotPlayerOptions"/> class.
/// </summary>
///
[TestFixture]
[Category(TestCategory.Players)]
public class BotPlayerOptionsTests
{
    /// <summary>
    ///   Verifies functionality of the Clone method.
    /// </summary>
    ///
    [Test]
    public void CloneCreatesNewInstanceWithSameValues()
    {
        var original = new BotPlayerOptions { Difficulty = Difficulty.Hard };

        var cloned = original.Clone();

        Assert.That(cloned, Is.Not.SameAs(original));
        Assert.That(cloned.Difficulty, Is.EqualTo(original.Difficulty));
    }

    /// <summary>
    ///   Verifies functionality of the Clone method.
    /// </summary>
    ///
    [Test]
    public void ClonePreservesDifficultyProperty()
    {
        var original = new BotPlayerOptions { Difficulty = Difficulty.Perfect };

        var cloned = original.Clone();

        Assert.That(cloned.Difficulty, Is.EqualTo(Difficulty.Perfect));
    }
}