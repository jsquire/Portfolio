using System;
using System.Linq;
using OrderFulfillment.Core.Models.External.Ecommerce;
using OrderFulfillment.Core.Models.External.OrderProduction;

using eCommerce = OrderFulfillment.Core.Models.External.Ecommerce;
using OrderProduction = OrderFulfillment.Core.Models.External.OrderProduction;

namespace OrderFulfillment.Core.Extensions
{
    /// <summary>
    ///   The set of extensions for the <see cref="OrderDetails" />
    ///   class.
    /// </summary>
    /// 
    public static class OrderDetailsExtensions
    {
        /// <summary>
        ///   Creates a new <see cref="CreateOrderMessage" /> based on the specified
        ///   <paramref name="instance"/>.
        /// </summary>
        /// 
        /// <param name="instance">The instance that this method was invoked on.</param>
        /// 
        /// <returns>A CreateOrderMessage materialzied with data sourced from the order details. </returns>
        /// 
        public static CreateOrderMessage ToCreateOrderMessage(this OrderDetails instance)
        {
            var createOrderMessage = new CreateOrderMessage();

            if (instance == null)
            {
                return createOrderMessage;
            }

            // Copy properties from the root of the details.
            
            createOrderMessage.Identity     = new OrderIdentity { PartnerOrderId = instance.OrderId };
            createOrderMessage.Customer     = new Customer { Code = instance.UserId, LanguageCode = instance.Recipients.FirstOrDefault()?.LanguageCode };
            createOrderMessage.Shipping     = new OrderShippingInformation();
            createOrderMessage.Instructions = new OrderInstructions();
            createOrderMessage.Recipients   = instance.Recipients.Select(recipient => OrderDetailsExtensions.CopyRecipientToNewRecipient(recipient)).ToList();
            createOrderMessage.LineItems    = instance.LineItems.Select(item => OrderDetailsExtensions.CopyLineItemToNewLineItem(item)).ToList();

            return createOrderMessage;
        }

        /// <summary>
        ///   Copies the eCommerce recipient to a new order recipient instance.
        /// </summary>
        /// 
        /// <param name="source">The source of data for the new instance.</param>
        /// 
        /// <returns>The new recipient instance, populated from the <paramref name="source" /></returns>
        /// 
        private static OrderProduction.Recipient CopyRecipientToNewRecipient(eCommerce.Recipient source)
        {
            var recipient = new OrderProduction.Recipient();

            if (source == null)
            {
                return recipient;
            }

            recipient.Id           = source.Id;
            recipient.LanguageCode = source.LanguageCode;
            recipient.Shipping     = OrderDetailsExtensions.CopyShippingInformationToNewShippingInformation(source.Shipping);
            recipient.OrderedItems = source.OrderedItems.Select(item => new OrderProduction.OrderItemDetails { Quantity = item.Quantity, LineItemId = item.LineItemId }).ToList();

            return recipient;
        }

        /// <summary>
        ///   Copies the eCommerce shipping information to a new order shipping information instance.
        /// </summary>
        /// 
        /// <param name="source">The source of data for the new instance.</param>
        /// 
        /// <returns>The new shipping information instance, populated from the <paramref name="source" /></returns>
        /// 
        private static OrderProduction.RecipientShippingInformation CopyShippingInformationToNewShippingInformation(eCommerce.RecipientShippingInformation source)
        {
            var shipping = new OrderProduction.RecipientShippingInformation();

            if (source == null)
            {
                return shipping;
            }

            var sourceAddress = source.Address;
            
            shipping.Address = new OrderProduction.Address
            {
               CareOf          = sourceAddress.CareOf,
               City            = sourceAddress.City,
               Company         = sourceAddress.Company,
               CountryCode     = sourceAddress.CountryCode,
               CustomData      = sourceAddress.CustomData,
               Email           = sourceAddress.Email,
               FirstName       = sourceAddress.FirstName,
               LastName        = sourceAddress.LastName,
               Line1           = sourceAddress.Line1,
               Line2           = sourceAddress.Line2,
               Line3           = sourceAddress.Line3,
               Line4           = sourceAddress.Line4,
               Phone           = sourceAddress.Phone,
               PostalCode      = sourceAddress.PostalCode,
               Region          = (OrderProduction.Region)sourceAddress.Region,
               StateOrProvince = sourceAddress.StateOrProvince,
               Type            = (OrderProduction.AddressType)sourceAddress.Type
            };

            shipping.DeliveryExpectation       = (OrderProduction.DeliveryExpectation)source.DeliveryExpectation;
            shipping.DeliveryExpectedBy        = String.IsNullOrEmpty(source.DeliveryExpectedBy) ? null : source.DeliveryExpectedBy;
            shipping.ExpectedShipDateUtc       = source.ExpectedShipDateUtc;
            shipping.Incoterms                 = (OrderProduction.Incoterms)source.Incoterms;
            shipping.RatingAccountCode         = source.RatingAccountCode;
            shipping.RequestedProviderCode     = source.RequestedProviderCode;
            shipping.RequestedSaturdayDelivery = source.RequestedSaturdayDelivery;
            shipping.RequestedServiceLevelCode = source.RequestedServiceLevelCode;
            shipping.SignatureRequirement      = (OrderProduction.SignatureRequirement)source.SignatureRequirement;

            return shipping;
        }

        /// <summary>
        ///   Copies the eCommerce line item to a new order line item instance.
        /// </summary>
        /// 
        /// <param name="source">The source of data for the new instance.</param>
        /// 
        /// <returns>The new line item instance, populated from the <paramref name="source" /></returns>
        /// 
        private static OrderProduction.LineItem CopyLineItemToNewLineItem(eCommerce.LineItem source)
        {
            var item = new OrderProduction.LineItem();

            if (source == null)
            {
                return item;
            }

            item.CountInSet            = source.CountInSet;
            item.DeclaredValue         = new OrderProduction.PriceInformation { Amount = source?.DeclaredValue?.Amount ?? 0, CurrencyCode = source?.DeclaredValue?.CurrencyCode ?? source?.UnitPrice?.CurrencyCode };
            item.Description           = source.Description;
            item.LineItemId            = source.LineItemId;
            item.ProductCode           = source.ProductCode;
            item.ResourceId            = source.ResourceId;
            item.ServiceLevelAgreement = source.ServiceLevelAgreement;
            item.UnitPrice             = new OrderProduction.PriceInformation { Amount = source?.UnitPrice?.Amount ?? 0, CurrencyCode = source?.UnitPrice?.CurrencyCode };

            return item;
        }
    }
}
