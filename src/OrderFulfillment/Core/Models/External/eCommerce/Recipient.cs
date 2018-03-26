using System.Collections.Generic;

namespace OrderFulfillment.Core.Models.External.Ecommerce
{
    public class Recipient
    {
        public string Id { get;  set; }
        public string LanguageCode { get;  set; }
        public RecipientShippingInformation Shipping { get;  set; }
        public List<OrderItemDetails> OrderedItems { get;  set; }
    }
}
