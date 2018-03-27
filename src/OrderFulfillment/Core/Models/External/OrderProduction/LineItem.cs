namespace OrderFulfillment.Core.Models.External.OrderProduction
{
    public class LineItem
    {
        public string LineItemId { get;  set; }
        public string ProductCode { get;  set; }
        public string ResourceId { get;  set; }
        public string Description { get;  set; }
        public PriceInformation DeclaredValue { get;  set; }
        public ushort CountInSet { get;  set; }
        public string ServiceLevelAgreement { get;  set; }
        public PriceInformation UnitPrice { get;  set; }
        public string Item { get;  set; }
    }
}
