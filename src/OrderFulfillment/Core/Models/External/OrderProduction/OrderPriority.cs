namespace OrderFulfillment.Core.Models.External.OrderProduction
{
    public enum OrderPriority
    {
        Normal = 0,
        Elevated,
        Critical,
        TestOrder,
        FirstPaid,
        FirstOrder
    }
}
