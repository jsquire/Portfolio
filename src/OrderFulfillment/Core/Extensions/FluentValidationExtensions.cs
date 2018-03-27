using System;
using FluentValidation;
using OrderFulfillment.Core.Models.Errors;

namespace OrderFulfillment.Core.Extensions
{
    /// <summary>
    ///   The set of extension methods for Fluent Validation types.
    /// </summary>
    /// 
    public static class FluentValidationExtensions
    {
        /// <summary>
        ///   Specifies that a validation failure includes a
        /// </summary>
        /// <typeparam name="T">The type to which the validaiton rule applies.</typeparam>
        /// <typeparam name="TProperty">The type of the property that the rule is validating.</typeparam>
        /// 
        /// <param name="instance">The validation rule to be associated with the <paramref name="errorCode" />.</param>
        /// <param name="errorCode">The error code to associate with a validation failure of the rule.</param>
        /// 
        /// <returns>The rule builder for use in chaining further rule definition syntax.</returns>
        /// 
        public static IRuleBuilderOptions<T, TProperty> WithErrorCode<T, TProperty>(this IRuleBuilderOptions<T, TProperty> instance, 
                                                                                         ErrorCode                         errorCode)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            return instance.WithErrorCode(errorCode.ToString());
        }
    }
}
