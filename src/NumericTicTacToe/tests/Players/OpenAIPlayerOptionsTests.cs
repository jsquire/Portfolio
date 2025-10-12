using NUnit.Framework;
using Squire.NumTic.Players;

namespace Squire.NumTic.Tests;

/// <summary>
///   The suite of tests for the <see cref="OpenAIPlayerOptions"/> class.
/// </summary>
///
[TestFixture]
[Category(TestCategory.Players)]
public class OpenAIPlayerOptionsTests
{
    /// <summary>
    ///   Verifies functionality of the default constructor.
    /// </summary>
    ///
    [Test]
    public void ConstructorInitializesWithDefaults()
    {
        var options = new OpenAIPlayerOptions();

        Assert.That(Enum.IsDefined(options.Difficulty), Is.True, "Difficulty should be initialized to a valid enum value");
        Assert.That(options.ModelName, Is.Not.Null.And.Not.Empty, "ModelName should be initialized to a non-empty string");
        Assert.That(options.MaxMoveRetries, Is.GreaterThanOrEqualTo(0), "MaxMoveRetries should be initialized to a non-negative value");
    }

    /// <summary>
    ///   Verifies functionality of the ModelName property.
    /// </summary>
    ///
    [TestCase(null)]
    [TestCase("")]
    public void ModelNameWithInvalidValueThrows(string? invalidValue)
    {
        var options = new OpenAIPlayerOptions();

        Assert.That(() => options.ModelName = invalidValue!,
            Throws.Exception);
    }

    /// <summary>
    ///   Verifies functionality of the MaxMoveRetries property.
    /// </summary>
    ///
    [TestCase(-1)]
    [TestCase(-100)]
    [TestCase(int.MinValue)]
    public void MaxMoveRetriesWithNegativeValueThrows(int negativeValue)
    {
        var options = new OpenAIPlayerOptions();

        Assert.That(() => options.MaxMoveRetries = negativeValue,
            Throws.InstanceOf<ArgumentOutOfRangeException>()
                .With.Property("ParamName").EqualTo("MaxMoveRetries"));
    }

    /// <summary>
    ///   Verifies functionality of the MaxMoveRetries property.
    /// </summary>
    ///
    [Test]
    public void MaxMoveRetriesAcceptsZero()
    {
        var options = new OpenAIPlayerOptions { MaxMoveRetries = 0 };
        Assert.That(options.MaxMoveRetries, Is.EqualTo(0));
    }

    /// <summary>
    ///   Verifies functionality of the Clone method.
    /// </summary>
    ///
    [Test]
    public void CloneCreatesIndependentInstanceWithPreservedValues()
    {
        var original = new OpenAIPlayerOptions
        {
            Difficulty = Difficulty.Medium,
            ModelName = "custom-model"
        };

        var cloned = original.Clone();

        // Verify it's a new instance with same values.

        Assert.That(cloned, Is.Not.SameAs(original), "Clone should create a new instance");
        Assert.That(cloned.Difficulty, Is.EqualTo(original.Difficulty), "Clone should preserve Difficulty value");
        Assert.That(cloned.ModelName, Is.EqualTo(original.ModelName), "Clone should preserve ModelName value");

        // Modify cloned instance to verify independence.

        cloned.Difficulty = Difficulty.Hard;
        cloned.ModelName = "different-model";

        Assert.That(original.Difficulty, Is.EqualTo(Difficulty.Medium), "Modifying clone should not affect original Difficulty");
        Assert.That(original.ModelName, Is.EqualTo("custom-model"), "Modifying clone should not affect original ModelName");
    }
}
