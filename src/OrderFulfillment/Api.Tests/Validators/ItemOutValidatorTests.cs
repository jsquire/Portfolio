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
    public class ItemOutValidatorTests
    {
        /// <summary>
        ///   Verifies validation of the order identifier.
        /// </summary>
        /// 
        [Fact]
        public async Task itemIsRequired()
        {
            var currentInstant = Instant.FromUtc(2017, 08, 17, 12, 0, 0);
            var fakeClock = new FakeClock(currentInstant);
            var itemValidator = new itemItemValidator() as IMessageValidator<ItemAsset>;
            var validator = new ItemOutValidator(itemValidator);
            var header = new OrderHeader { OrderId = "ABC123", OrderDate = currentInstant.ToDateTimeUtc() };

            var target = new LineItem
            {
                Assets = null
            };

            var result = await validator.ValidateAsync(target);

            result.Should().NotBeNull("because a validation result should have been returned");
            result.Should().ContainSingle(error => ((error.MemberPath == nameof(LineItem.Assets)) && (error.Code == ErrorCode.ValueIsRequired.ToString())), "because the item set was not provided");
        }

        /// <summary>
        ///   Verifies validation of the order identifier.
        /// </summary>
        /// 
        [Fact]
        public async Task EmptyitemIsValid()
        {
            var currentInstant = Instant.FromUtc(2017, 08, 17, 12, 0, 0);
            var fakeClock = new FakeClock(currentInstant);
            var itemValidator = new itemItemValidator() as IMessageValidator<ItemAsset>;
            var validator = new ItemOutValidator(itemValidator);
            var header = new OrderHeader { OrderId = "ABC123", OrderDate = currentInstant.ToDateTimeUtc() };

            var target = new LineItem
            {
                Assets = new List<ItemAsset>()
            };

            var result = await validator.ValidateAsync(target);

            result.Should().NotBeNull("because a validation result should have been returned");
            result.Should().BeEmpty("because an empty item set is allowed");
        }

        /// <summary>
        ///   Verifies validation of the order identifier.
        /// </summary>
        /// 
        [Fact]
        public async Task itemSizeIsValidated()
        {
            var currentInstant = Instant.FromUtc(2017, 08, 17, 12, 0, 0);
            var fakeClock = new FakeClock(currentInstant);
            var itemValidator = new itemItemValidator() as IMessageValidator<ItemAsset>;
            var validator = new ItemOutValidator(itemValidator);
            var header = new OrderHeader { OrderId = "ABC123", OrderDate = currentInstant.ToDateTimeUtc() };
            var item = new List<ItemAsset>();

            // Note that the test is "<=" which will add one extra item to the set, since the index
            // starts at 0.

            for (var index = 0; index <= ItemOutValidator.MaxitemItemCount; ++index)
            {
                item.Add(new ItemAsset { Name = $"Key{ index }", Location = $"Value{ index }" });
            }

            var target = new LineItem
            {
                Assets = item
            };

            var result = await validator.ValidateAsync(target);

            result.Should().NotBeNull("because a validation result should have been returned");
            result.Should().ContainSingle(error => ((error.MemberPath == nameof(LineItem.Assets)) && (error.Code == ErrorCode.SetCountIsInvalid.ToString())), "because the item set has too many items");
        }

        /// <summary>
        ///   Verifies validation of the order identifier.
        /// </summary>
        /// 
        [Fact]
        public async Task itemItemsAreValidated()
        {
            var currentInstant = Instant.FromUtc(2017, 08, 17, 12, 0, 0);
            var fakeClock = new FakeClock(currentInstant);
            var headerValidator = new OrderHeaderValidator(fakeClock) as IMessageValidator<OrderHeader>;
            var itemValidator = new itemItemValidator() as IMessageValidator<ItemAsset>;
            var validator = new ItemOutValidator(itemValidator);
            var header = new OrderHeader { OrderId = "ABC123", OrderDate = currentInstant.ToDateTimeUtc() };
            var item = new List<ItemAsset>();

            item.Add(new ItemAsset { Name = "Item", Location = null });

            var target = new LineItem
            {
                Assets = item
            };

            var result = await validator.ValidateAsync(target);
            var failurePath = $"{ nameof(LineItem.Assets) }[0].{ nameof(ItemAsset.Location) }";

            result.Should().NotBeNull("because a validation result should have been returned");
            result.Should().ContainSingle(error => error.MemberPath == failurePath, "because the item item value was not set");
        }
    }
}
