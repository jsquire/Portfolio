namespace Squire.Toolbox.Tests
{
    /// <summary>
    ///   The classification of a test or a suite of related tests.
    /// </summary>
    ///
    public enum Category
    {
        /// <summary>The category of the test isn't known or understood.</summary>
        Unknown = -1,

        /// <summary>The associated test is meant to verify a build and is safe to run in isolation; it has no external dependencies.</summary>
        BuildVerification = 0
    }
}
