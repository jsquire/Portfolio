namespace OrderFulfillment.Api.Security
{
    /// <summary>
    ///   Represents the relative strength of an authentication scheme.
    /// </summary>
    /// 
    public enum AuthenticationStrength
    {        
        Unknown,
        Weak,
        Medium,
        Strong,
        Stronger,
        Strongest
    }
}