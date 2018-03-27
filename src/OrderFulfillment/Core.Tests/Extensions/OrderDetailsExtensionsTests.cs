using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using OrderFulfillment.Core.Extensions;
using OrderFulfillment.Core.Models.External.Ecommerce;
using OrderFulfillment.Core.Models.External.OrderProduction;
using Xunit;

using eCommerce = OrderFulfillment.Core.Models.External.Ecommerce;
using OrderProduction = OrderFulfillment.Core.Models.External.OrderProduction;

namespace OrderFulfillment.Core.Tests.Extensions
{
    /// <summary>
    ///   The suite of tests for the <see cref="OrderDetailsExtensions" /> class.
    /// </summary>
    /// 
    public class OrderDetailsExtensionsTests
    {
        /// <summary>
        ///   Verifies functionality of the <see cref="OrderDetailsExtensions.ToCreateOrderMessage" />
        ///   method.
        /// </summary>
        /// 
        [Fact] 
        public void CanTranslateOrderDetailsToCreateOrderMessage()
        {
            var details = this.GenerateOrderDetails();
            var result  = details.ToCreateOrderMessage();

            result.Should().NotBeNull("because the translation should produce a result");

            // Message level

            result.Identity.Should().NotBeNull("because an identity should have been craated");
            result.Identity.PartnerOrderId.Should().Be(details.OrderId, "because the OrderId should translate");
            result.Customer.Should().NotBeNull("because a customer should have been created");
            result.Customer.Code.Should().Be(details.UserId, "because the user identifier should be used as the customer code");
            result.Customer.LanguageCode.Should().Be(details.Recipients.First().LanguageCode, "because the customer should use the language of the first recipient");
            result.Shipping.Should().NotBeNull("because the shipping information should have been created");
            result.Instructions.Should().NotBeNull("because the instructions should have been created");

            // Recipient level

            var expectedRecipient = new OrderProduction.Recipient
            {
                Id           = "1",
                LanguageCode = "en-us",
                Shipping     = new OrderProduction.RecipientShippingInformation
                {
                    Address = new OrderProduction.Address
                    {
                        FirstName       = "Alex",
                        LastName        = "Summers",
                        CareOf          = "Lorna Dane",
                        Line1           = "1407 Graymalken Lane",
                        City            = "Salem Center",
                        StateOrProvince = "NY",
                        PostalCode      = "10560",
                        CountryCode     = "USA",
                        Email           = "havok@schoolforthegifted.com",
                        Phone           = "212-479-7990",
                        Region          = OrderProduction.Region.Americas,
                        Type            = OrderProduction.AddressType.Residential
                    },

                    DeliveryExpectation = OrderProduction.DeliveryExpectation.OnOrBeforeDate,
                    RatingAccountCode   = "A1"
                },

                OrderedItems = new List<OrderProduction.OrderItemDetails>
                {
                    new OrderProduction.OrderItemDetails { LineItemId = "1", Quantity = 2 },
                    new OrderProduction.OrderItemDetails { LineItemId = "3", Quantity = 4 }
                }
            };

            result.Recipients.Should().NotBeNull("because the recipients set should translate");
            result.Recipients.Should().HaveSameCount(details.Recipients, "because the same number of recipients should have translated");
            result.Recipients.First().ShouldBeEquivalentTo(expectedRecipient, "because the recipients should have translated");

            // Line Item Level

            var expectedLineItems = new List<OrderProduction.LineItem>
            {
                new OrderProduction.LineItem
                {
                    LineItemId            = "1",
                    CountInSet            = 27,
                    ProductCode           = "OMGNO",
                    Description           = "Some thing",
                    DeclaredValue         = new OrderProduction.PriceInformation { Amount = 5, CurrencyCode = "USD" },
                    UnitPrice             = new OrderProduction.PriceInformation { Amount = 10, CurrencyCode = "GBP" },
                    ResourceId            = "Hello",
                    ServiceLevelAgreement = "Some agreement"
                },

                new OrderProduction.LineItem
                {
                    LineItemId      = "3",
                    CountInSet      = 12,
                    ProductCode     = "YAS!",
                    Description     = "Other thing",
                    DeclaredValue   = new OrderProduction.PriceInformation { Amount = 0, CurrencyCode = null },
                    UnitPrice       = new OrderProduction.PriceInformation { Amount = 0, CurrencyCode = null },
                }
            };

            result.LineItems.Should().NotBeNull("because the line items should translate");
            result.LineItems.Should().HaveSameCount(details.LineItems, "because the same number of line items should have translated");
            result.LineItems.ShouldAllBeEquivalentTo(expectedLineItems, "because the line items should have translated");            
        }
        
        /// <summary>
        ///   Verifies functionality of the <see cref="OrderDetailsExtensions.ToCreateOrderMessage" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void NullDeliveryExpectedDateRemainsNull()
        {
            var details = this.GenerateOrderDetails();
            details.Recipients.Single().Shipping.DeliveryExpectedBy = null;

            var result = details.ToCreateOrderMessage();
            result.Recipients.Single().Shipping.DeliveryExpectedBy.Should().BeNull("because null indicates we don't have a value");
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="OrderDetailsExtensions.ToCreateOrderMessage" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void EmptyStringDeliveryExpectedDateBecomesNull()
        {
            var details = this.GenerateOrderDetails();
            details.Recipients.Single().Shipping.DeliveryExpectedBy = "";

            var result = details.ToCreateOrderMessage();
            result.Recipients.Single().Shipping.DeliveryExpectedBy.Should().BeNull("because invalid string is not compatible with many systems");
        }

        /// <summary>
        ///   Generates a mostly-populated OrderDetails instance for testing.
        /// </summary>
        /// 
        /// <returns>The generated order details.</returns>
        /// 
        private OrderDetails GenerateOrderDetails()
        {
            return new OrderDetails
            {
                OrderId = "ABC123",
                Recipients = new List<eCommerce.Recipient>
                {
                   new eCommerce.Recipient
                   {
                      Id           = "1",
                      LanguageCode = "en-us",
                      Shipping     = new eCommerce.RecipientShippingInformation
                      {
                          Address = new eCommerce.Address
                          {
                              FirstName       = "Alex",
                              LastName        = "Summers",
                              CareOf          = "Lorna Dane",
                              Line1           = "1407 Graymalken Lane",
                              City            = "Salem Center",
                              StateOrProvince = "NY",
                              PostalCode      = "10560",
                              CountryCode     = "USA",
                              Email           = "havok@schoolforthegifted.com",
                              Phone           = "212-479-7990",
                              Region          = eCommerce.Region.Americas,
                              Type            = eCommerce.AddressType.Residential
                          },

                          DeliveryExpectation = eCommerce.DeliveryExpectation.OnOrBeforeDate,
                          RatingAccountCode   = "A1"
                       },

                       OrderedItems = new List<eCommerce.OrderItemDetails>
                       {
                          new eCommerce.OrderItemDetails { LineItemId = "1", Quantity = 2 },
                          new eCommerce.OrderItemDetails { LineItemId = "3", Quantity = 4 }
                       }
                    }
                },

                LineItems = new List<eCommerce.LineItem>
                {
                   new eCommerce.LineItem
                   {
                     LineItemId            = "1",
                     AdditionalSheetCount  = 2,
                     CountInSet            = 27,
                     ProductCode           = "OMGNO",
                     Description           = "Some thing",
                     DeclaredValue         = new eCommerce.PriceInformation { Amount = 5, CurrencyCode = "USD" },
                     UnitPrice             = new eCommerce.PriceInformation { Amount = 10, CurrencyCode = "GBP" },
                     ResourceId            = "Hello",
                     ServiceLevelAgreement = "Some agreement"
                   },

                   new eCommerce.LineItem
                   {
                     LineItemId           = "3",
                     AdditionalSheetCount = 4,
                     CountInSet           = 12,
                     ProductCode          = "YAS!",
                     Description          = "Other thing"
                   }
                }
            };
        }
    }
}
