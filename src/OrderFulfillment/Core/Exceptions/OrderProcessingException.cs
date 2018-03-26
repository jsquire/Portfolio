using System;
using System.Runtime.Serialization;

namespace OrderFulfillment.Core.Exceptions
{
    /// <summary>
    ///   Represents an exception for the scenario when order processing has failed.
    /// </summary>
    /// 
    /// <seealso cref="System.Exception" />
    /// 
    [Serializable]
    public class OrderProcessingException : Exception
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref="OrderProcessingException"/> class.
        /// </summary>
        /// 
        public OrderProcessingException() : base() 
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="OrderProcessingException"/> class.
        /// </summary>
        /// 
        /// <param name="message">The message that describes the error.</param>
        /// 
        public OrderProcessingException(string message) : base(message)
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="OrderProcessingException"/> class.
        /// </summary>
        /// 
        /// <param name="message">The message that describes the error.</param>
        /// <param name="inner">The source exception that caused this exception scenario.</param>
        /// 
        public OrderProcessingException(string    message, 
                                        Exception inner) : base(message, inner) 
        { 
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="OrderProcessingException"/> class.
        /// </summary>
        /// 
        /// <param name="info">The <see cref="System.Runtime.Serialization.SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="System.Runtime.Serialization.StreamingContext" /> that contains contextual information about the source or destination.</param>
        /// 
        protected OrderProcessingException(SerializationInfo info,
                                           StreamingContext context) 
        { 
        }
    }
}
