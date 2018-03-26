using System;
using FluentValidation;
using OrderFulfillment.Api.Models.Requests;
using OrderFulfillment.Core.Extensions;
using OrderFulfillment.Core.Models.Errors;
using OrderFulfillment.Core.Validators;
using NodaTime;

namespace OrderFulfillment.Api.Validators
{
    /// <summary>
    ///   Serves as a validator for the <see cref="OrderHeader" />
    ///   structure.
    /// </summary>
    /// 
    /// <remarks>
    ///   This validator is intended to provided basic structural checks
    ///   for a type.  It is not intended to enforce buiness rules where 
    ///   the relationship of a field to other values is concerned.
    /// </remarks>
    /// 
    public class OrderHeaderValidator : MessageValidatorBase<OrderHeader>
    {
        /// <summary>The maximum length of the order identifier field.</summary>
        internal const int OrderIdMaxLength = 50;

        /// <summary>The validator to use for validating that a date/time is now or earlier.</summary>
        private readonly DateTimeIsNowOrEarlierValidator dateTimeNowOrEarlierValidator;

        /// <summary>
        ///   Initializes a new instance of the <see cref="OrderHeaderValidator" /> class.
        /// </summary>
        /// 
        /// <param name="clock">The clock to use for date/time operations.</param>
        /// 
        public OrderHeaderValidator(IClock clock)
        {
            // Initialize the class.

            if (clock == null)
            {
                throw new ArgumentNullException(nameof(clock));
            }

            this.dateTimeNowOrEarlierValidator = new DateTimeIsNowOrEarlierValidator(clock);

            // Define the validation rules.

            this.RuleFor(orderHeader => orderHeader.OrderId)
                .NotEmpty().WithErrorCode(ErrorCode.ValueIsRequired)
                .Length(0, OrderHeaderValidator.OrderIdMaxLength).WithErrorCode(ErrorCode.LengthIsInvalid);

            this.RuleFor(orderHeader => orderHeader.OrderDate)
                .SetValidator(this.dateTimeNowOrEarlierValidator)
                .When(orderHeader => orderHeader.OrderDate.HasValue);
        }
    }
}