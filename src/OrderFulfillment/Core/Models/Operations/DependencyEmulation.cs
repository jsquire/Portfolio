namespace OrderFulfillment.Core.Models.Operations
{
    /// <summary>
    ///   The set of emulated results for operations with external
    ///   dependencies.
    /// </summary>
    /// 
    public class DependencyEmulation
    {
        /// <summary>
        ///   The result of the operation to retrieve the details of an order.
        /// </summary>
        /// 
        public OperationResult OrderDetails { get;  set; }

        /// <summary>
        ///   The result of the operation to build a CreateOrderMessage for an order..
        /// </summary>
        /// 
        public OperationResult CreateOrderMessage { get;  set; }

        /// <summary>
        ///   The result of the operation to submit the order for production.
        /// </summary>
        /// 
        public OperationResult OrderSubmission { get;  set; }
    }
}
