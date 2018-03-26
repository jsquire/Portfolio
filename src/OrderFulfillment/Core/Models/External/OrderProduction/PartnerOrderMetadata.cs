using System;
using System.Collections.Generic;

namespace OrderFulfillment.Core.Models.External.OrderProduction
{
    public class PartnerOrderMetadata
    {
        public DateTime? OrderDateUtc { get;  set; }
        public List<SequencedData> CustomerReferenceData { get;  set; }
    }
}
