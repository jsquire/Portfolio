using System;
using System.Threading.Tasks;
using FluentAssertions;
using OrderFulfillment.Api.Models.Requests;
using OrderFulfillment.Api.Validators;
using OrderFulfillment.Core.Models.Errors;
using Xunit;

namespace OrderFulfillment.Api.Tests.Validators
{
    /// <summary>
    ///   The suite of tests for the <see cref="itemItemValidator" /> class.
    /// </summary>
    /// 
    public class itemItemValidatorTests
    {
        /// <summary>
        ///   Verifies validation of the name.
        /// </summary>
        /// 
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task NameIsRequired(string name)
        {
            var validator = new itemItemValidator();
            var target    = new ItemAsset { Name = name, Location = "SomeValue" };

            var result = await validator.ValidateAsync(target);

            result.Should().NotBeNull("because a validation result should have been returned");
            result.Should().ContainSingle(error => ((error.MemberPath == nameof(ItemAsset.Name)) && (error.Code == ErrorCode.ValueIsRequired.ToString())), "because the name was not provided");
        }

        /// <summary>
        ///   Verifies validation of the name.
        /// </summary>
        /// 
        [Fact]
        public async Task NameIsValidated()
        {
            var name      = new String('j', itemItemValidator.MaxNameLength + 1);
            var validator = new itemItemValidator();
            var target    = new ItemAsset { Name = name, Location = "SomeValue" };

            var result = await validator.ValidateAsync(target);

            result.Should().NotBeNull("because a validation result should have been returned");
            result.Should().ContainSingle(error => ((error.MemberPath == nameof(ItemAsset.Name)) && (error.Code == ErrorCode.LengthIsInvalid.ToString())), "because the name was too long");
        }

        /// <summary>
        ///   Verifies validation of the name.
        /// </summary>
        /// 
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task ValueIsRequired(string value)
        {
            var validator = new itemItemValidator();
            var target    = new ItemAsset { Name = "SomeName", Location = value };

            var result = await validator.ValidateAsync(target);

            result.Should().NotBeNull("because a validation result should have been returned");
            result.Should().ContainSingle(error => ((error.MemberPath == nameof(ItemAsset.Location)) && (error.Code == ErrorCode.ValueIsRequired.ToString())), "because the value was not provided");
        }

        /// <summary>
        ///   Verifies validation of the name.
        /// </summary>
        /// 
        [Fact]
        public async Task ValueIsValidated()
        {
            var value     = new String('j', itemItemValidator.MaxValueLength + 1);
            var validator = new itemItemValidator();
            var target    = new ItemAsset { Name = "SomeName", Location = value };

            var result = await validator.ValidateAsync(target);

            result.Should().NotBeNull("because a validation result should have been returned");
            result.Should().ContainSingle(error => ((error.MemberPath == nameof(ItemAsset.Location)) && (error.Code == ErrorCode.LengthIsInvalid.ToString())), "because the value was too long");
        }
    }
}
