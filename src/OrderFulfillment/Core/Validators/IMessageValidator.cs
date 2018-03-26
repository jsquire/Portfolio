namespace OrderFulfillment.Core.Validators
{
    // <summary>
    ///   Defines the contract to be implemented by message validators.
    /// </summary>
    /// 
    /// <typeparam name="T">The type to be validated.</typeparam>
    ///
    public interface IMessageValidator<T> : IValidator<T>
    {
    }
}
