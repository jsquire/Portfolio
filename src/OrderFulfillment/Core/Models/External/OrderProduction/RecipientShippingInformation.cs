using System;

namespace OrderFulfillment.Core.Models.External.OrderProduction
{
    public class RecipientShippingInformation
    {
        public Address Address { get;  set; }
        public SignatureRequirement SignatureRequirement { get;  set; }
        public DeliveryExpectation DeliveryExpectation { get;  set; }
        public string DeliveryExpectedBy { get;  set; }
        public DateTime? ExpectedShipDateUtc { get;  set; }
        public Incoterms Incoterms { get;  set; }
        public string RequestedProviderCode { get;  set; }
        public string RequestedServiceLevelCode { get;  set; }
        public string RatingAccountCode { get;  set; }
        public bool RequestedSaturdayDelivery { get;  set; }
    }
}
