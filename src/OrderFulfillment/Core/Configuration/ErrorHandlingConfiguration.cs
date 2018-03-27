namespace OrderFulfillment.Core.Configuration
{
    /// <summary>
    ///    The set of configuration that influences the behavior of error handling-related
    ///    operations.
    /// </summary>
    /// 
    public class ErrorHandlingConfiguration : IConfiguration
    {
        /// <summary>
        ///   Indicates whether exception details are enabled.
        /// </summary>
        /// 
        /// <value>
        ///   <c>true</c> if exception details are enabled; otherwise, <c>false</c>.
        /// </value>
        /// 
        public bool ExceptionDetailsEnabled { get;  set; }
    }
}
