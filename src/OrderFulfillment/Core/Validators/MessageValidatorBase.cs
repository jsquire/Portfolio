namespace OrderFulfillment.Core.Validators
{
    /// <summary>
    ///   Serves as a base class for validators of message structure.
    /// </summary>
    /// 
    /// <typeparam name="T">The type being validated.</typeparam>
    /// 
    /// <seealso cref="ValidatorBase{T}" />
    /// <seealso cref="IMessageValidator{T}" />
    /// 
    public abstract class MessageValidatorBase<T> : ValidatorBase<T>, IMessageValidator<T>
    {
    }
}
