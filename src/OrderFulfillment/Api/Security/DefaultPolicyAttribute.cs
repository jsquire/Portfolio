using System;

namespace OrderFulfillment.Api.Security
{
    /// <summary>
    ///   Allows annotating security policies as part of set of default policies
    ///   to be applied.
    /// </summary>
    ///         
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum)]
    public class DefaultPolicyAttribute : Attribute
    {
    }
}