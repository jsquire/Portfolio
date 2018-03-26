using System;
using System.Threading.Tasks;
using FluentAssertions;
using OrderFulfillment.Api.Models.Requests;
using OrderFulfillment.Api.Validators;
using OrderFulfillment.Core.Models.Errors;
using NodaTime;
using NodaTime.Testing;
using Xunit;

namespace OrderFulfillment.Api.Tests.Validators
{
    /// <summary>
    ///   The suite of tests for the <see cref="OrderHeaderValidator" /> class.
    /// </summary>
    /// 
    public class OrderHeaderValidatorTests
    {
        /// <summary>
        ///   Verifies validation of the order identifier.
        /// </summary>
        /// 
        [Fact]
        public async Task OrderIdIsRequired()
        {
            var currentInstant = Instant.FromUtc(2017, 08, 17, 12, 0, 0);
            var fakeClock      = new FakeClock(currentInstant);
            var validator      = new OrderHeaderValidator(fakeClock);
            var target         = new OrderHeader { OrderId = null, OrderDate = currentInstant.ToDateTimeUtc() };

            var result = await validator.ValidateAsync(target);

            result.Should().NotBeNull("because a validation result should have been returned");
            result.Should().ContainSingle(error => ((error.MemberPath == nameof(OrderHeader.OrderId)) && (error.Code == ErrorCode.ValueIsRequired.ToString())), "because the order id was not provided");
        }

        /// <summary>
        ///   Verifies validation of the order identifier.
        /// </summary>
        /// 
        [Fact]
        public async Task OrderIdLengthIsEnforced()
        {
            var orderId        = new String('j', OrderHeaderValidator.OrderIdMaxLength + 1);
            var currentInstant = Instant.FromUtc(2017, 08, 17, 12, 0, 0);
            var fakeClock      = new FakeClock(currentInstant);
            var validator      = new OrderHeaderValidator(fakeClock);
            var target         = new OrderHeader { OrderId = orderId, OrderDate = currentInstant.ToDateTimeUtc() };

            var result = await validator.ValidateAsync(target);

            result.Should().NotBeNull("because a validation result should have been returned");
            result.Should().ContainSingle(error => ((error.MemberPath == nameof(OrderHeader.OrderId)) && (error.Code == ErrorCode.LengthIsInvalid.ToString())), "because the order id was too long");
        }

        /// <summary>
        ///   Verifies validation of the order date.
        /// </summary>
        /// 
        [Fact]
        public async Task OrderDateIsNotRequired()
        {
            var orderId        = new String('1', OrderHeaderValidator.OrderIdMaxLength - 1);
            var currentInstant = Instant.FromUtc(2017, 08, 17, 12, 0, 0);
            var fakeClock      = new FakeClock(currentInstant);
            var validator      = new OrderHeaderValidator(fakeClock);
            var target         = new OrderHeader { OrderId = orderId, OrderDate = null };

            var result = await validator.ValidateAsync(target);

            result.Should().NotBeNull("because a validation result should have been returned");
            result.Should().HaveCount(0, "because a null order date is allowed");
        }

        /// <summary>
        ///   Verifies validation of the order date.
        /// </summary>
        /// 
        [Fact]
        public async Task OrderDateIsValidated()
        {
            var orderId        = new String('5', OrderHeaderValidator.OrderIdMaxLength -1);
            var currentInstant = Instant.FromUtc(2017, 08, 17, 12, 0, 0);
            var laterInstant   = currentInstant.Plus(Duration.FromHours(1));
            var fakeClock      = new FakeClock(currentInstant);
            var validator      = new OrderHeaderValidator(fakeClock);
            var target         = new OrderHeader { OrderId = orderId, OrderDate = laterInstant.ToDateTimeUtc() };

            var result = await validator.ValidateAsync(target);

            result.Should().NotBeNull("because a validation result should have been returned");
            result.Should().ContainSingle(error => (error.MemberPath == nameof(OrderHeader.OrderDate)), "because the order date was in the future");
        }
    }
}
