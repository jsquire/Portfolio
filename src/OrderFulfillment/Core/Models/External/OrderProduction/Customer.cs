namespace OrderFulfillment.Core.Models.External.OrderProduction
{
    public class Customer
    {
        public string Code { get;  set; }
        public string EmergencyPhone { get;  set; }
        public string LanguageCode { get;  set; }
        public Address Address { get;  set; }
    }
}
