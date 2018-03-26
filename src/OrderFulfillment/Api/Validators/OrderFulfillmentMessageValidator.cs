using System;
using FluentValidation;
using OrderFulfillment.Api.Models.Requests;
using OrderFulfillment.Core.Extensions;
using OrderFulfillment.Core.Models.Errors;
using OrderFulfillment.Core.Validators;

namespace OrderFulfillment.Api.Validators
{
    /// <summary>
    ///   Serves as a validator for the <see cref="OrderFulfillmentMessage" />
    ///   structure.
    /// </summary>
    /// 
    /// <remarks>
    ///   This validator is intended to provided basic structural checks
    ///   for a type.  It is not intended to enforce buiness rules where 
    ///   the relationship of a field to other values is concerned.
    /// </remarks>
    ///
    public class OrderFulfillmentMessageValidator : MessageValidatorBase<OrderFulfillmentMessage>, IMessageValidator<OrderFulfillmentMessage>
    {
        /// <summary>The validator to use for validating the order header.</summary>
        private readonly IMessageValidator<OrderHeader> headerValidator;

        /// <summary>The validator to use for validating the item items.</summary>
        private readonly IMessageValidator<LineItem> itemOutValidator;

        /// <summary>
        ///   Initializes a new instance of the <see cref="OrderFulfillmentMessageValidator" /> class.
        /// </summary>
        /// 
        /// <param name="headerValidator">The validator to use for validaiton of the order header.</param>
        /// <param name="itemOutValidator">The validator to use for valdiation of the item items assocaited with the order.</param>
        /// 
        public OrderFulfillmentMessageValidator(IMessageValidator<OrderHeader> headerValidator,
                                                IMessageValidator<LineItem>     itemOutValidator)
        {
            // Initialize the class.

            this.headerValidator  = headerValidator ?? throw new ArgumentNullException(nameof(headerValidator));
            this.itemOutValidator = itemOutValidator ?? throw new ArgumentNullException(nameof(itemOutValidator));

            // Define the validation rules.

            this.RuleFor(message => message.OrderRequestHeader)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotEmpty().WithErrorCode(ErrorCode.ValueIsRequired)
                .SetValidator((FluentValidation.IValidator<OrderHeader>)this.headerValidator);

            this.RuleFor(message => message.LineItems)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithErrorCode(ErrorCode.ValueIsRequired)
                .SetCollectionValidator((FluentValidation.IValidator<LineItem>)itemOutValidator);
        }
    }
}