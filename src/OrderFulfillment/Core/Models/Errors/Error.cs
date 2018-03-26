using System;

namespace OrderFulfillment.Core.Models.Errors
{
    /// <summary>
    ///   An error that was observed within the API.  
    /// </summary>
    /// 
    /// <remarks>
    ///   The description contained herein is intended to provide context for debugging API calls only;
    ///   the text is not localized nor deemed appropriate to display to users.  
    ///   
    ///   Callers are encouraged to consider the error code for context and display any appropriate user-facing 
    ///   text that makes sense within their specific system.
    /// </remarks>
    /// 
    [Serializable]
    public class Error
    {
        /// <summary>The textual code that uniquely identifies the error scenario.</summary>
        public string Code { get; private set; }

        /// <summary>The text that describes the full path to the member where the error was encountered.</summary>
        public string MemberPath { get; private set; }

        /// <summary>The text intended to provide human-readable detail, as an aid for debugging API calls.</summary>
        public string Description { get; private set; }

        /// <summary>
        ///   Initializes a new instance of the <see cref="Error"/> class.
        /// </summary>
        /// 
        /// <param name="errorCode">The error code that identifies the error scenario.</param>
        /// <param name="memberPath">The path of the member where the error was encountered.</param>
        /// <param name="description">The human-readable description of the error scenario, for debugging context.</param>
        /// 
        public Error(string errorCode, 
                     string memberPath,
                     string description)
        {
            this.Code        = errorCode;
            this.MemberPath  = memberPath;
            this.Description = description;
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="Error"/> class.
        /// </summary>
        /// 
        /// <param name="errorCode">The error code that identifies the error scenario.</param>
        /// <param name="description">The human-readable description of the error scenario, for debugging context.</param>
        /// 
        public Error(string errorCode,
                     string description) : this(errorCode, String.Empty, description)
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="Error"/> class.
        /// </summary>
        /// 
        /// <param name="errorCode">The well-known error code that identifies the error scenario.</param>
        /// <param name="memberPath">The path of the member where the error was encountered.</param>
        /// <param name="description">The human-readable description of the error scenario, for debugging context.</param>
        /// 
        public Error(ErrorCode error,
                     string    memberPath,
                     string    description) : this(error.ToString(), memberPath, description)
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="Error"/> class.
        /// </summary>
        /// 
        /// <param name="errorCode">The well-known error code that identifies the error scenario.</param>
        /// <param name="description">The human-readable description of the error scenario, for debugging context.</param>
        /// 
        public Error(ErrorCode error,
                     string    description) : this(error.ToString(), description)
        {
        }

        /// <summary>
        ///   Generates a string representation of the entire error including <see cref="Code"/>, <see cref="MemberPath"/>, and <see cref="Description"/>.
        /// </summary>
        /// 
        /// <returns>A string that represents the error.</returns>
        /// 
        public override string ToString()
        {
            var memberPathFormatted = string.IsNullOrEmpty(this.MemberPath) ? null : $" path:'{this.MemberPath}'";
            return $"{this.Code}{memberPathFormatted} '{this.Description}'";
        }
    }
}
