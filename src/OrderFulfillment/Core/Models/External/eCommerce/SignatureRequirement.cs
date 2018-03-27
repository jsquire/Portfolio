namespace OrderFulfillment.Core.Models.External.Ecommerce
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
