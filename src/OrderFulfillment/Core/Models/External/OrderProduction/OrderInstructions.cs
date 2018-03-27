using System.Collections.Generic;

namespace OrderFulfillment.Core.Models.External.OrderProduction
{
    public class OrderInstructions
    {
        public List<SequencedData> SpecialInstructions { get;  set; }
        public List<SequencedData> PackSlipInformation { get;  set; }
        public OrderPriority Priority { get;  set; }
        public string PriorityExplanation { get;  set; }
        public string SuggestedSite { get;  set; }
        public bool? IgnorePartnerAestheticDefects { get;  set; }
        public bool? IgnoreCopyright { get;  set; }
    }
}
