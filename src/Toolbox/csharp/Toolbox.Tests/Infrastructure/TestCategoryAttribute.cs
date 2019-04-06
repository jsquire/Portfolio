using System;
using System.Collections.Generic;
using System.Linq;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Squire.Toolbox.Tests
{
    /// <summary>
    ///   The attribute responsible for specifying a Test Category trait to a test or suite of
    ///   tests to clarify their scope and context.
    /// </summary>
    ///
    /// <seealso cref="System.Attribute" />
    /// <seealso cref="Xunit.Sdk.ITraitAttribute" />
    ///
    [TraitDiscoverer("Squire.Toolbox.Tests.TestCategoryAttribute+Discoverer", "Squire.Toolbox.Tests")]
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public class TestCategoryAttribute : Attribute, ITraitAttribute
    {
        /// <summaryThe name of the trait to associate with this attribute.</summary>
        public const string TraitName = "TestCategory";

        /// <summary>
        ///   Initializes a new instance of the <see cref="TestCategoryAttribute"/> class.
        /// </summary>
        ///
        /// <param name="category">The category to use for test classification.</param>
        ///
        public TestCategoryAttribute(Category category)
        {
        }

        /// <summary>
        ///   Allows the XUnit framework to discover traits for members decorated with the
        ///   <see cref="TestCategoryAttribute" />.
        /// </summary>
        ///
        /// <seealso cref="Xunit.Sdk.ITraitDiscoverer" />
        ///
        public class Discoverer : ITraitDiscoverer
        {
            /// <summary>
            ///   Gets the trait metadata for XUnit to consume and surface for attribution to tests.
            /// </summary>
            ///
            /// <param name="traitAttribute">An attribute identified as a trait to be processed by the XUnit framework.</param>
            ///
            /// <returns>The properties of the trait to associate with the decorated tests.</returns>
            ///
            public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
            {
                yield return new KeyValuePair<string, string>
                (
                    TestCategoryAttribute.TraitName,
                    traitAttribute.GetConstructorArguments().FirstOrDefault()?.ToString() ?? Category.Unknown.ToString()
                );
            }
        }
    }
}
