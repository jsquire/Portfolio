using System;
using FluentAssertions;
using OrderFulfillment.Core.Extensions;
using OrderFulfillment.Core.Models.Errors;
using Xunit;

namespace OrderFulfillment.Core.Tests.Extensions
{
    /// <summary>
    ///   The suite of tests for the <see cref="FluentValidationExtensions" />
    /// </summary>
    /// 
    public class FluentValidatorExtensionsTests
    {
        /// <summary>
        ///   Validates that the WithErrorCode extension validates its arguments.
        /// </summary>
        /// 
        [Fact]
        public void WithErrorCodeValidatesArguments()
        {
            Action actionUnderTest = () => FluentValidationExtensions.WithErrorCode<string, int>(null, ErrorCode.LengthIsInvalid);
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the method was invoked on a null instance");
        }
    }
}
