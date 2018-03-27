using FluentValidation;
using OrderFulfillment.Api.Models.Requests;
using OrderFulfillment.Core.Extensions;
using OrderFulfillment.Core.Models.Errors;
using OrderFulfillment.Core.Validators;

namespace OrderFulfillment.Api.Validators
{

    /// <summary>
    ///   Serves as a validator for the <see cref="ItemAsset" />
    ///   structure.
    /// </summary>
    /// 
    /// <remarks>
    ///   This validator is intended to provided basic structural checks
    ///   for a type.  It is not intended to enforce buiness rules where 
    ///   the relationship of a field to other values is concerned.
    /// </remarks>
    /// 
    public class itemItemValidator : MessageValidatorBase<ItemAsset>
    {
        /// <summary>The maximum length of an item item name.</summary>
        internal const int MaxNameLength = 250;

        /// <summary>The maximum length of an item item value.</summary>
        internal const int MaxValueLength = 4000;

        /// <summary>
        ///   Initializes a new instance of the <see cref="itemItemValidator" /> class.
        /// </summary>
        /// 
        public itemItemValidator()
        {
            // Define the validation rules.

            this.RuleFor(item => item.Name)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotEmpty().WithErrorCode(ErrorCode.ValueIsRequired)
                .Length(0, itemItemValidator.MaxNameLength).WithErrorCode(ErrorCode.LengthIsInvalid);

            this.RuleFor(item => item.Location)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotEmpty().WithErrorCode(ErrorCode.ValueIsRequired)
                .Length(0, itemItemValidator.MaxValueLength).WithErrorCode(ErrorCode.LengthIsInvalid);
        }
    }
}