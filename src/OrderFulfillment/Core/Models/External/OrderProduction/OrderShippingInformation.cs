namespace OrderFulfillment.Core.Models.External.OrderProduction
{
    public class OrderShippingInformation
    {
        public Address ReturnAddress { get;  set; }
        public ShipWhen ShippingInstruction{ get;  set; }
    }
}
