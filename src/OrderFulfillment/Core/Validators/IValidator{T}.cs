using System.Collections.Generic;
using System.Threading.Tasks;
using OrderFulfillment.Core.Models.Errors;

namespace OrderFulfillment.Core.Validators
{
    /// <summary>
    ///   Defines the contract to be implemented by validators.
    /// </summary>
    /// 
    /// <typeparam name="T">The type to be validated.</typeparam>
    /// 
    public interface IValidator<T>
    {
        /// <summary>
        ///   Validates the specified target.
        /// </summary>
        /// 
        /// <param name="target">The target to be validated.</param>
        /// 
        /// <returns>A set of <see cref="Error"/> instances that correspond to validation failures; if the <paramref name="target"/> is valid, the set will be empty.</returns>
        /// 
        IEnumerable<Error> Validate(T target);
        
        /// <summary>
        ///   Validates the specified target.
        /// </summary>
        /// 
        /// <param name="target">The target to be validated.</param>
        /// <param name="ruleNames">The names of validation rule sets to run in addition to the default ruleset.</param>
        /// 
        /// <returns>A set of <see cref="Error"/> instances that correspond to validation failures; if the <paramref name="target"/> is valid, the set will be empty.</returns>
        ///
        IEnumerable<Error> Validate(T               target, 
                                    params string[] ruleNames);

        /// <summary>
        ///   Validates the specified target.
        /// </summary>
        /// 
        /// <param name="target">The target to be validated.</param>
        /// 
        /// <returns>A set of <see cref="Error"/> instances that correspond to validation failures; if the <paramref name="target"/> is valid, the set will be empty.</returns>
        ///
        Task<IEnumerable<Error>> ValidateAsync(T target);

        /// <summary>
        ///   Validates the specified target.
        /// </summary>
        /// 
        /// <param name="target">The target to be validated.</param>
        /// <param name="ruleNames">The names of validation rule sets to run in addition to the default ruleset.</param>
        /// 
        /// <returns>A set of <see cref="Error"/> instances that correspond to validation failures; if the <paramref name="target"/> is valid, the set will be empty.</returns>
        ///
        Task<IEnumerable<Error>> ValidateAsync(T               target, 
                                               params string[] ruleNames);
    }
}
