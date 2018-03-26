using System.Linq;
using System.Reflection;
using FluentAssertions;
using OrderFulfillment.Api.RouteConstraints;
using Xunit;

namespace OrderFulfillment.Api.Tests.RouteConstraints
{
    /// <summary>
    ///   The suite of unit tests for the <see cref="OrderFulfillment.Api.RouteConstraints.RouteConstraintLocator" />
    ///   class.
    /// </summary>
    public class RouteConstraintLocatorTests
    {
        /// <summary>
        ///   Validates that the locator finds a set of constraints.
        /// </summary>
        /// 
        [Fact]
        public void LocatorFindsConstraints()
        {
            var locator = new RouteConstraintLocator(Assembly.GetExecutingAssembly());
            locator.DiscoveredConstraints.Should().NotBeNull("because there are classes decorated with constraints");
            locator.DiscoveredConstraints.Count.Should().BeGreaterOrEqualTo(1, "because at least one class was decorated with constraints");
        }

        /// <summary>
        ///   Validates that the locator maintains the correct constraint and name association.
        /// </summary>
        /// 
        [Fact]
        public void LocatorCorrectlyAssociatesConstraints()
        {
            var locator = new RouteConstraintLocator(Assembly.GetExecutingAssembly());
            
            foreach (var constraintPair in locator.DiscoveredConstraints.Where(pair => pair.Key != "D"))
            {
                constraintPair.Value.Name.EndsWith(constraintPair.Key).Should().BeTrue("because the constraint name should be associated with the decorated class");
            }
        }

        /// <summary>
        ///   Validates that the locator allows multiple route dectorators.
        /// </summary>
        /// 
        [Fact]
        public void LocatorAllowsMultipleDecorators()
        {
            var locator = new RouteConstraintLocator(Assembly.GetExecutingAssembly());
            
            locator.DiscoveredConstraints.Where(pair => ((pair.Key == "C") || (pair.Key == "D")))
                                         .Select(pair => pair.Value.Name)
                                         .Distinct()
                                         .Should().HaveCount(1, "because TestConstraintC should handle two constraints");
        }
    
        [RouteConstraint("A")]
        public class TestConstraintA
        {
        }

        [RouteConstraint("B")]
        public class TestConstraintB
        {
        }

        [RouteConstraint("C")]
        [RouteConstraint("D")]
        public class TestConstraintC
        {
        }
    }
}
