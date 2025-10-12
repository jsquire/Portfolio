using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace Squire.NumTic.Tests;

/// <summary>
///   Marks methods as live tests that have external network dependencies.
/// </summary>
///
/// <remarks>
///   Methods with this attribute will:
///   <list type="bullet">
///     <item>
///       <description>Be recognized as test methods (no need for a separate [Test] attribute).</description>
///     </item>
///     <item>
///       <description>Be skipped during normal test runs unless TestEnvironment.RunLiveTestsByDefault is <c>true</c>./description>
///     </item>
///     <item>
///       <description>Run when explicitly filtered by category or test name. (Example: <c>dotnet test --filter TestCategory=Live</c>).</description>
///     </item>
///     <item>
///       <description>Run when explicitly selected in IDE test runners</description>
///     </item>
///   </list>
/// </remarks>
///
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
internal class LiveTestAttribute : TestAttribute, IApplyToTest
{
    /// <summary>The reason to report when the test is skipped during a run.</summary>
    private const string Reason = "Test that requires live network resources. Must be run explicitly or by enabling `RunLiveTestsByDefault` in the settings.";

    /// <summary>
    ///   Applies the Test, Live category, and conditionally Explicit behavior.
    /// </summary>
    ///
    /// <param name="test">The test to modify.</param>
    ///
    /// <remarks>
    ///   This implementation shadows the base ApplyToTest method,
    ///   as it is not marked as virtual.
    /// </remarks>
    ///
    public new void ApplyToTest(Test test)
    {
        // Apply the base TestAttribute behavior to identify the decorated method
        // as a test for the runner.

        base.ApplyToTest(test);

        // Apply Explicit behavior unless Live tests should always run.

        if (!TestEnvironment.RunLiveTestsByDefault)
        {
            // Remove all existing categories to ensure Live is the only category.
            // This prevents tests from being included in other category filters.

            if (test.Properties.TryGet(PropertyNames.Category, out var categories))
            {
                categories.Clear();
            }

            test.RunState = RunState.Explicit;
            test.Properties.Set(PropertyNames.SkipReason, Reason);
        }

        // Add the Live category so it can be filtered.

        test.Properties.Add(PropertyNames.Category, TestCategory.Live);
    }
}