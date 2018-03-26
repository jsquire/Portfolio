using System;
using System.Runtime.Serialization;

namespace OrderFulfillment.Core.Exceptions
{
    /// <summary>
    ///   Represents an exception for the scenario when a needed dependency is missing.
    /// </summary>
    /// 
    /// <seealso cref="System.Exception" />
    /// 
    [Serializable]
    public class MissingDependencyException : Exception
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref="MissingDependencyException"/> class.
        /// </summary>
        /// 
        public MissingDependencyException() : base() 
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="MissingDependencyException"/> class.
        /// </summary>
        /// 
        /// <param name="message">The message that describes the error.</param>
        /// 
        public MissingDependencyException(string message) : base(message)
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="MissingDependencyException"/> class.
        /// </summary>
        /// 
        /// <param name="message">The message that describes the error.</param>
        /// <param name="inner">The source exception that caused this exception scenario.</param>
        /// 
        public MissingDependencyException(string    message, 
                                          Exception inner) : base(message, inner) 
        { 
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="MissingDependencyException"/> class.
        /// </summary>
        /// 
        /// <param name="info">The <see cref="System.Runtime.Serialization.SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="System.Runtime.Serialization.StreamingContext" /> that contains contextual information about the source or destination.</param>
        /// 
        protected MissingDependencyException(SerializationInfo info,
                                             StreamingContext context) 
        { 
        }
    }
}
