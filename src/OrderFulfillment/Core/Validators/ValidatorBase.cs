using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using OrderFulfillment.Core.Models.Errors;

namespace OrderFulfillment.Core.Validators
{
    /// <summary>
    ///   Serves as a base class for validators.
    /// </summary>
    /// 
    /// <typeparam name="T">The type that is being validated.</typeparam>
    /// 
    /// <seealso cref="FluentValidation.AbstractValidator{T}" />
    /// 
    public abstract class ValidatorBase<T> : AbstractValidator<T>, Core.Validators.IValidator<T>, Core.Validators.IValidator
    {
        /// <summary>The implicit name of the rule set created using Fluent Validation without an explicitly named ruleset assigned.</summary>
        private const string CommonRuleSetName = "default";

        /// <summary>A failed validation result that corresponds to when the validation target is null.</summary>
        private static readonly ValidationResult NullTargetResult = new ValidationResult(new[] { new ValidationFailure("target", "The value may not be null") { ErrorCode = ErrorCode.InvalidValue.ToString() } });

        /// <summary>
        ///   Validates the specified target.
        /// </summary>
        /// 
        /// <param name="target">The target to be validated.</param>
        /// 
        /// <returns>A set of <see cref="Error"/> instances that correspond to validation failures; if the <paramref name="target"/> is valid, the set will be empty.</returns>
        /// 
        public new IEnumerable<Error> Validate(T target)
        {
            return this.Validate(target, null);
        }
        
        /// <summary>
        ///   Validates the specified target.
        /// </summary>
        /// 
        /// <param name="target">The target to be validated.</param>
        /// <param name="ruleNames">The names of validation rule sets to run in addition to the default ruleset.</param>
        /// 
        /// <returns>A set of <see cref="Error"/> instances that correspond to validation failures; if the <paramref name="target"/> is valid, the set will be empty.</returns>
        ///
        public IEnumerable<Error> Validate(T               target, 
                                           params string[] ruleNames)
        {           
            ValidationResult result;

            if (target != null)
            {
                result =  this.Validate(target, ruleSet: ValidatorBase<T>.BuildRuleSets(ruleNames)); 
            }
            else
            {
                result = ValidatorBase<T>.NullTargetResult;
            }
             
            return ValidatorBase<T>.TransformValidationResult(result);
        }

        /// <summary>
        ///   Validates the specified target.
        /// </summary>
        /// 
        /// <param name="target">The target to be validated.</param>
        /// 
        /// <returns>A set of <see cref="Error"/> instances that correspond to validation failures; if the <paramref name="target"/> is valid, the set will be empty.</returns>
        ///
        public Task<IEnumerable<Error>> ValidateAsync(T target)
        {
            return this.ValidateAsync(target, null);
        }

        /// <summary>
        ///   Validates the specified target.
        /// </summary>
        /// 
        /// <param name="target">The target to be validated.</param>
        /// <param name="ruleNames">The names of validation rule sets to run in addition to the default ruleset.</param>
        /// 
        /// <returns>A set of <see cref="Error"/> instances that correspond to validation failures; if the <paramref name="target"/> is valid, the set will be empty.</returns>
        ///
        public async Task<IEnumerable<Error>> ValidateAsync(T               target, 
                                                            params string[] ruleNames)
        {
            ValidationResult result;

            if (target != null)
            {
                result = await this.ValidateAsync(target, ruleSet: ValidatorBase<T>.BuildRuleSets(ruleNames)).ConfigureAwait(false);
            }
            else
            {
                result = ValidatorBase<T>.NullTargetResult;
            }

            return ValidatorBase<T>.TransformValidationResult(result);
        }

        /// <summary>
        ///   Validates the specified target.
        /// </summary>
        /// 
        /// <param name="target">The target to be validated.</param>
        /// 
        /// <returns>A set of <see cref="Error"/> instances that correspond to validation failures; if the <paramref name="target"/> is valid, the set will be empty.</returns>
        /// 
        public IEnumerable<Error> Validate(object target)
        {
            if (!(target is T))
            {
               throw new ArgumentException(nameof(target), $"Only objects of { typeof(T).Name } can be validated.");
            }

            return this.Validate((T)target, null);
        }
        
        /// <summary>
        ///   Validates the specified target.
        /// </summary>
        /// 
        /// <param name="target">The target to be validated.</param>
        /// <param name="ruleNames">The names of validation rule sets to run in addition to the default ruleset.</param>
        /// 
        /// <returns>A set of <see cref="Error"/> instances that correspond to validation failures; if the <paramref name="target"/> is valid, the set will be empty.</returns>
        ///
        public IEnumerable<Error> Validate(object          target, 
                                           params string[] ruleNames)
        {           
            if (!(target is T))
            {
               throw new ArgumentException(nameof(target), $"Only objects of { typeof(T).Name } can be validated.");
            }

            return this.Validate((T)target, ruleNames);
        }

        /// <summary>
        ///   Validates the specified target.
        /// </summary>
        /// 
        /// <param name="target">The target to be validated.</param>
        /// 
        /// <returns>A set of <see cref="Error"/> instances that correspond to validation failures; if the <paramref name="target"/> is valid, the set will be empty.</returns>
        ///
        public async Task<IEnumerable<Error>> ValidateAsync(object target)
        {
            if (!(target is T))
            {
               throw new ArgumentException(nameof(target), $"Only objects of { typeof(T).Name } can be validated.");
            }

            return await this.ValidateAsync((T)target, null).ConfigureAwait(false);
        }

        /// <summary>
        ///   Validates the specified target.
        /// </summary>
        /// 
        /// <param name="target">The target to be validated.</param>
        /// <param name="ruleNames">The names of validation rule sets to run in addition to the default ruleset.</param>
        /// 
        /// <returns>A set of <see cref="Error"/> instances that correspond to validation failures; if the <paramref name="target"/> is valid, the set will be empty.</returns>
        ///
        public async Task<IEnumerable<Error>> ValidateAsync(object          target, 
                                                            params string[] ruleNames)
        {
            if (!(target is T))
            {
               throw new ArgumentException(nameof(target), $"Only objects of { typeof(T).Name } can be validated.");
            }

            return await this.ValidateAsync((T)target, ruleNames).ConfigureAwait(false);
        }

        /// <summary>
        ///   Transforms the raw validation result into the expected set of errors.
        /// </summary>
        /// 
        /// <param name="result">The result to transform.</param>
        /// 
        /// <returns>The set of errors that correspond to the result; if the result was valid, the set will be empty.</returns>
        ///         
        private static IEnumerable<Error> TransformValidationResult(ValidationResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (result.IsValid)
            {
                return Enumerable.Empty<Error>();
            }

            return result.Errors.Select(failure => new Error(failure.ErrorCode, failure.PropertyName, failure.ErrorMessage));
        }

        /// <summary>
        ///   Builds the rule sets to be used for validation.
        /// </summary>
        /// 
        /// <param name="ruleNames">The non-default rule names to include</param>
        /// 
        /// <returns>A string representing the rulesets to pass for validation</returns>
        /// 
        private static string BuildRuleSets(params string[] ruleNames)
        {                     
            var ruleSets = ValidatorBase<T>.CommonRuleSetName;

            if ((ruleNames != null) && (ruleNames.Length > 0))
            {
                ruleSets = $"{ ruleSets },{  String.Join(",", ruleNames.Where(name => !String.IsNullOrEmpty(name))) }";
            }

            return ruleSets;
        }
    }
}
