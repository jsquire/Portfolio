namespace OrderFulfillment.Core.Models.Errors
{
    /// <summary>
    ///   The set of error codes that are well-known within the system. 
    /// </summary>
    /// 
    public enum ErrorCode
    {
        /// <summary>The exact nature of the error is unknown.</summary>
        Unknown,

        /// <summary>The partner identifier does not comply with the expected pattern in some way.</summary>
        PartnerIdentifierMalformed,

        /// <summary>The order identifier does not comply with the expected pattern in some way.</summary>
        OrderIdentifierMalformed,

        /// <summary>A required value was missing.  This should be accompanied by a member path for context.</summary>
        ValueIsRequired,

        /// <summary>A value had an invalid length.  This should be accompanied by a member path for context.</summary>
        LengthIsInvalid,

        /// <summary>A set had an invalid number of items.  This should be accompanied by a member path for context.</summary>
        SetCountIsInvalid,

        /// <summary>A value had characters that are not permitted.  This should be accompanied by a member path for context.</summary>
        InvalidCharacters,

        /// <summary>A value was not permitted.  This should be accompanied by a member path for context.</summary>
        InvalidValue,

        /// <summary>A value was not a member of a known set.  This should be accompanied by a member path for context.</summary>
        UnknownValue,

        /// <summary>A numeric value was out of the allowable range.  This should be accompanied by a member path for context.</summary>
        NumberIsOutOfRange,

        /// <summary>An internal exception occured.  This should be accompanied by a description for context.</summary>
        Exception,

        /// <summary>This order was not found in the system of record; order details are unknown.</summary>
        UnknownOrder,

        /// <summary>The order submission has failed.</summary>
        OrderSubmissionFailed
    }
}
