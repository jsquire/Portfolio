using System.Collections.Generic;

namespace OrderFulfillment.Core.Models.External.OrderProduction
{
    public class Recipient
    {
        public string Id { get;  set; }
        public string LanguageCode { get;  set; }
        public RecipientShippingInformation Shipping { get;  set; }
        public List<OrderItemDetails> OrderedItems { get;  set; }
    }
}
