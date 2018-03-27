namespace OrderFulfillment.Core.Models.External.OrderProduction
{
    public enum SignatureRequirement
    {
        NoSignatureRequired = 0,
        Indirect,
        SignatureRequired,
        DirectSignatureRequired,
        AdultSignatureRequired
    }
}
