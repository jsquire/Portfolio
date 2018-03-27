using System;
using FluentValidation;
using OrderFulfillment.Api.Models.Requests;
using OrderFulfillment.Core.Extensions;
using OrderFulfillment.Core.Models.Errors;
using OrderFulfillment.Core.Validators;

namespace OrderFulfillment.Api.Validators
{
    public class ItemOutValidator : MessageValidatorBase<LineItem>
    {
        /// <summary>The maximum number of items allowed in the item set.</summary>
        internal const int MaxitemItemCount = 1;

        /// <summary>The validator to use for validating the item items.</summary>
        private readonly IMessageValidator<ItemAsset> itemValidator;

        public ItemOutValidator(IMessageValidator<ItemAsset> itemValidator)
        {
            this.itemValidator = itemValidator ?? throw new ArgumentNullException(nameof(itemValidator));

            // Define the validation rules.

            this.RuleFor(message => message.Assets)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithErrorCode(ErrorCode.ValueIsRequired)
                .Must(item => item.Count <= ItemOutValidator.MaxitemItemCount).WithErrorCode(ErrorCode.SetCountIsInvalid)
                .SetCollectionValidator((FluentValidation.IValidator<ItemAsset>)itemValidator);
        }
    }
}