namespace OrderFulfillment.Api.Security
{
    /// <summary>
    ///   Acts as a pseudo-enumeration for custom-defined claims that may
    ///   appear in an authenticated principal, providing friendly names for
    ///   the claim URIs.
    /// </summary>
    /// 
    public static class CustomClaimTypes
    {
        /// <summary>A claim that indicates the type of identity associated with the principal.</summary>
        public const string IdentityType = "urn:ordering:security:identity-type";

        /// <summary>The presence of this claim indicates that the principal may perform privileged operations.</summary>
        public const string MayAccessPriviledgedOperations = "urn:ordering:security:privilege:sudo";

        /// <summary>A claim that indicates an association between the principal and a partner in the ordering context.</summary>
        public const string Partner = "urn:ordering:partner";
    }
}