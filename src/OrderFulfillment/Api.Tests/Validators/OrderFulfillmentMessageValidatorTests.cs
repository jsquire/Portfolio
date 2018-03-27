using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using OrderFulfillment.Api.Models.Requests;
using OrderFulfillment.Api.Validators;
using OrderFulfillment.Core.Models.Errors;
using OrderFulfillment.Core.Validators;
using NodaTime;
using NodaTime.Testing;
using Xunit;

namespace OrderFulfillment.Api.Tests.Validators
{
    /// <summary>
    ///   The suite of tests for the <see cref="OrderFulfillmentMessageValidator" />
    ///   class.
    /// </summary>
    /// 
    public class OrderFulfillmentMessageValidatorTests
    {
        /// <summary>
        ///   Verifies validation of the order identifier.
        /// </summary>
        /// 
        [Fact]
        public async Task RequestHeaderIsRequired()
        {
            var currentInstant     = Instant.FromUtc(2017, 08, 17, 12, 0, 0);
            var fakeClock          = new FakeClock(currentInstant);
            var headerValidator    = new OrderHeaderValidator(fakeClock) as IMessageValidator<OrderHeader>;
            var itemValidator = new itemItemValidator() as IMessageValidator<ItemAsset>;
            var itemOutValidator   = new ItemOutValidator(itemValidator) as IMessageValidator<LineItem>;
            var validator          = new OrderFulfillmentMessageValidator(headerValidator, itemOutValidator);

            var target = new OrderFulfillmentMessage
            {
                OrderRequestHeader = null,
                LineItems            = new List<LineItem>()
            };

            var result = await validator.ValidateAsync(target);

            result.Should().NotBeNull("because a validation result should have been returned");
            result.Should().ContainSingle(error => ((error.MemberPath == nameof(OrderFulfillmentMessage.OrderRequestHeader)) && (error.Code == ErrorCode.ValueIsRequired.ToString())), "because the order header was not provided");
        }

        /// <summary>
        ///   Verifies validation of the order identifier.
        /// </summary>
        /// 
        [Fact]
        public async Task RequestHeaderIsValidated()
        {
            var currentInstant     = Instant.FromUtc(2017, 08, 17, 12, 0, 0);
            var fakeClock          = new FakeClock(currentInstant);
            var headerValidator    = new OrderHeaderValidator(fakeClock) as IMessageValidator<OrderHeader>;
            var itemValidator = new itemItemValidator() as IMessageValidator<ItemAsset>;
            var itemOutValidator   = new ItemOutValidator(itemValidator) as IMessageValidator<LineItem>;
            var validator          = new OrderFulfillmentMessageValidator(headerValidator, itemOutValidator);
            var header             = new OrderHeader { OrderId = null, OrderDate = currentInstant.ToDateTimeUtc() };

            var target = new OrderFulfillmentMessage
            {
                OrderRequestHeader = header,
                LineItems            = new List<LineItem>()
            };

            var result = await validator.ValidateAsync(target);

            result.Should().NotBeNull("because a validation result should have been returned");
            result.Should().ContainSingle(error => (error.MemberPath == $"{ nameof(OrderFulfillmentMessage.OrderRequestHeader) }.{ nameof(OrderHeader.OrderId) }"), "because the order id was not provided in the header");
        }

        /// <summary>
        ///   Verifies validation of the order identifier.
        /// </summary>
        /// 
        [Fact]
        public async Task ItemOutIsRequired()
        {
            var currentInstant     = Instant.FromUtc(2017, 08, 17, 12, 0, 0);
            var fakeClock          = new FakeClock(currentInstant);
            var headerValidator    = new OrderHeaderValidator(fakeClock) as IMessageValidator<OrderHeader>;
            var itemValidator = new itemItemValidator() as IMessageValidator<ItemAsset>;
            var itemOutValidator   = new ItemOutValidator(itemValidator) as IMessageValidator<LineItem>;
            var validator          = new OrderFulfillmentMessageValidator(headerValidator, itemOutValidator);
            var header             = new OrderHeader { OrderId = "ABC123", OrderDate = currentInstant.ToDateTimeUtc() };

            var target = new OrderFulfillmentMessage
            {
                OrderRequestHeader = header,
                LineItems            = null
            };

            var result = await validator.ValidateAsync(target);

            result.Should().NotBeNull("because a validation result should have been returned");
            result.Should().ContainSingle(error => ((error.MemberPath == nameof(OrderFulfillmentMessage.LineItems)) && (error.Code == ErrorCode.ValueIsRequired.ToString())), "because the itemout set was not provided");
        }

        /// <summary>
        ///   Verifies validation of the order identifier.
        /// </summary>
        /// 
        [Fact]
        public async Task EmptyItemOutIsValid()
        {
            var currentInstant     = Instant.FromUtc(2017, 08, 17, 12, 0, 0);
            var fakeClock          = new FakeClock(currentInstant);
            var headerValidator    = new OrderHeaderValidator(fakeClock) as IMessageValidator<OrderHeader>;
            var itemValidator = new itemItemValidator() as IMessageValidator<ItemAsset>;
            var itemOutValidator   = new ItemOutValidator(itemValidator) as IMessageValidator<LineItem>;
            var validator          = new OrderFulfillmentMessageValidator(headerValidator, itemOutValidator);
            var header             = new OrderHeader { OrderId = "ABC123", OrderDate = currentInstant.ToDateTimeUtc() };

            var target = new OrderFulfillmentMessage
            {
                OrderRequestHeader = header,
                LineItems            = new List<LineItem>()
            };

            var result = await validator.ValidateAsync(target);

            result.Should().NotBeNull("because a validation result should have been returned");
            result.Should().BeEmpty("because an empty itemout set is allowed");
        }

        /// <summary>
        ///   Verifies validation of the order identifier.
        /// </summary>
        /// 
        [Fact]
        public async Task ItemOutsAreValidated()
        {
            var currentInstant     = Instant.FromUtc(2017, 08, 17, 12, 0, 0);
            var fakeClock          = new FakeClock(currentInstant);
            var headerValidator    = new OrderHeaderValidator(fakeClock) as IMessageValidator<OrderHeader>;
            var itemValidator = new itemItemValidator() as IMessageValidator<ItemAsset>;
            var itemOutValidator   = new ItemOutValidator(itemValidator) as IMessageValidator<LineItem>;
            var validator          = new OrderFulfillmentMessageValidator(headerValidator, itemOutValidator);
            var header             = new OrderHeader { OrderId = "ABC123", OrderDate = currentInstant.ToDateTimeUtc() };

            var itemOuts = new List<LineItem>
            {
                new LineItem
                {
                    Assets = new List<ItemAsset>
                    {
                        new ItemAsset(),
                        new ItemAsset()
                    }
                }
            };

            var target = new OrderFulfillmentMessage
            {
                OrderRequestHeader = header,
                LineItems            = itemOuts
            };

            var result      = await validator.ValidateAsync(target);
            var failurePath = $"{ nameof(OrderFulfillmentMessage.LineItems) }[0].{ nameof(LineItem.Assets) }";

            result.Should().NotBeNull("because a validation result should have been returned");
            result.Should().ContainSingle(error => error.MemberPath == failurePath, "because the itemout violated a rule");
        }
    }
}
