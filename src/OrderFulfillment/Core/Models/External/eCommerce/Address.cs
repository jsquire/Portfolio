namespace OrderFulfillment.Core.Models.External.Ecommerce
{
    public class Address
    {
        public string FirstName { get;  set; }
        public string LastName { get;  set; }
        public string Company { get;  set; }
        public string CareOf { get;  set; }
        public string Line1 { get;  set; }
        public string Line2 { get;  set; }
        public string Line3 { get;  set; }
        public string Line4 { get;  set; }
        public string City { get;  set; }
        public string StateOrProvince { get;  set; }
        public string PostalCode { get;  set; }
        public string CountryCode { get;  set; }
        public string Email { get;  set; }
        public string Phone { get;  set; }
        public AddressType Type { get;  set; }
        public string CustomData { get;  set; }
        public Region Region { get;  set; }
    }
}
