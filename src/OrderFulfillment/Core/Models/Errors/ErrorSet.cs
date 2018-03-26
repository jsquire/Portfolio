using System;
using System.Collections.Generic;

namespace OrderFulfillment.Core.Models.Errors
{
    /// <summary>
    ///   A set of errors that was observed within the API.  
    /// </summary>
    /// 
    /// <seealso cref="OrderFulfillment.Core.Models.Errors.Error"/>
    /// 
    public class ErrorSet
    {
        /// <summary>The errors in this set; this may be an empty set, but will not be <c>null</c>.</summary>
        public IEnumerable<Error> Errors { get; }

        /// <summary>
        ///   Initializes a new instance of the <see cref="ErrorSet"/> class,
        ///   with an empty set of errors.
        /// </summary>
        /// 
        public ErrorSet()
        {
            this.Errors = new Error[0];
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="ErrorSet"/> class,
        ///   with the set containing just the provided <param name="error" />.
        /// </summary>
        /// 
        /// <param name="error">The lone error to be contained by the set.</param>
        /// 
        public ErrorSet(Error error)
        {
            if (error == null)
            {
                throw new ArgumentNullException(nameof(error));
            }

            this.Errors = new[] { error };
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="ErrorSet"/> class,
        ///   with the set containing just the provided error information.
        /// </summary>
        /// 
        /// <param name="errorCode">The error code that identifies the error scenario.</param>
        /// <param name="memberPath">The path of the member where the error was encountered.</param>
        /// <param name="errorDescription">The human-readable description of the error scenario, for debugging context.</param>
        /// 
        public ErrorSet(ErrorCode errorCode,
                        string    memberPath,
                        string    errorDescription) : this(new Error(errorCode, memberPath, errorDescription))
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="ErrorSet"/> class,
        ///   with the set containing just the provided error information.
        /// </summary>
        /// 
        /// <param name="errorCode">The error code that identifies the error scenario.</param>
        /// <param name="errorDescription">The human-readable description of the error scenario, for debugging context.</param>
        /// 
        public ErrorSet(ErrorCode errorCode,
                        string    errorDescription) : this(new Error(errorCode, errorDescription))
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="ErrorSet"/> class,
        ///   with the set containing just the provided error information.
        /// </summary>
        /// 
        /// <param name="errorCode">The error code that identifies the error scenario.</param>
        /// <param name="memberPath">The path of the member where the error was encountered.</param>
        /// <param name="errorDescription">The human-readable description of the error scenario, for debugging context.</param>
        /// 
        public ErrorSet(string errorCode,
                        string memberPath,
                        string errorDescription) : this(new Error(errorCode, memberPath, errorDescription))
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="ErrorSet"/> class,
        ///   with the set containing just the provided error information.
        /// </summary>
        /// 
        /// <param name="errorCode">The error code that identifies the error scenario.</param>
        /// <param name="errorDescription">The human-readable description of the error scenario, for debugging context.</param>
        /// 
        public ErrorSet(string errorCode,
                        string errorDescription) : this(new Error(errorCode, errorDescription))
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="ErrorSet"/> class,
        ///   with the set containing just the provided <param name="error" />.
        /// </summary>
        /// 
        /// <param name="errors">The errors error to be contained by the set.</param>
        /// 
        public ErrorSet(IEnumerable<Error> errors)
        {
            if (errors == null)
            {
                throw new ArgumentNullException(nameof(errors));
            }

            this.Errors = errors;
        }
    }
}
