using System;
using System.Collections.Generic;
using FluentAssertions;
using OrderFulfillment.Core.Models.Errors;
using Xunit;

namespace OrderFulfillment.Core.Tests.Models.Errors
{
    /// <summary>
    ///   The suite of tests for the <see cref="ErrorSet" /> class.
    /// </summary>
    /// 
    public class ErrorSetTests
    {
        /// <summary>
        ///   Verifies validation rules in the constructor.
        /// </summary>
        ///
        [Fact]
        public void ConstructionValidatesTheErrors()
        {
           Action actionUnderTest = () => new ErrorSet((IEnumerable<Error>)null);
           actionUnderTest.ShouldThrow<ArgumentNullException>("because the errors collection must be populated");
        }

        /// <summary>
        ///   Verifies validation rules in the constructor.
        /// </summary>
        ///
        [Fact]
        public void ConstructionAllowsAnEmptySetOfErrorss()
        {
           Action actionUnderTest = () => new ErrorSet(new Error[0]);
           actionUnderTest.ShouldNotThrow("because tan empty set of errors is allowed");
        }

        /// <summary>
        ///   Verifies validation rules in the constructor.
        /// </summary>
        ///
        [Fact]
        public void ConstructionAllowsASingleError()
        {
           var expectedError = new Error("Code", "Description");
           var set           = new ErrorSet(expectedError);

           set.Should().NotBeNull("because the construction should have been successful");

           set.Errors.Should().HaveCount(1, "because the provided error should be the only one present.")
                              .And.Equal(new[] { expectedError }, (left, right) => ((left.Code == right.Code) && (left.Description == right.Description)), "because the provided error should have been used for the final error set");
        }

        /// <summary>
        ///   Verifies validation rules in the constructor.
        /// </summary>
        ///
        [Fact]
        public void ConstructionAllowsAnErrorByComponentsWithoutMemberPath()
        {
           var expectedError = new Error("Code", "Description");
           var set           = new ErrorSet(expectedError.Code, expectedError.Description);

           set.Should().NotBeNull("because the construction should have been successful");

           set.Errors.Should().HaveCount(1, "because the provided error should be the only one present.")
                              .And.Equal(new[] { expectedError }, (left, right) => ((left.Code == right.Code) && (left.Description == right.Description)), "because the provided error should have been used for the final error set");
        }

        /// <summary>
        ///   Verifies validation rules in the constructor.
        /// </summary>
        ///
        [Fact]
        public void ConstructionAllowsAnErrorByComponents()
        {
           var expectedError = new Error("Code", "Path", "Description");
           var set           = new ErrorSet(expectedError.Code, expectedError.MemberPath, expectedError.Description);

           set.Should().NotBeNull("because the construction should have been successful");

           set.Errors.Should().HaveCount(1, "because the provided error should be the only one present.")
                              .And.Equal(new[] { expectedError }, (left, right) => ((left.Code == right.Code) && (left.Description == right.Description)), "because the provided error should have been used for the final error set");
        }
    }
}
