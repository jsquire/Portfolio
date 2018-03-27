namespace OrderFulfillment.Api.Security
{
    /// <summary>
    ///   The type of identity associated with a principal.  
    /// </summary>
    /// 
    public enum IdentityType
    {
        /// <summary>The identity type is not known.</summary>
        Unknown = 0,

        /// <summary>The principal is considered an individual user of the system; analagous to a person behind the keyboard.</summary>
        User = 1,

        /// <summary>The principal is considered a service acting on it's own behalf.</summary>
        Service = 2,

        /// <summary>The princilap is considered a service acting on behalf of a specific user.</summary>
        ServiceOnBehalfOf = 3
    }
}