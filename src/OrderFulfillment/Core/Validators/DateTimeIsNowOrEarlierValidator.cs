using System;
using FluentValidation.Validators;
using NodaTime;

namespace OrderFulfillment.Core.Validators
{
    /// <summary>
    ///   Validates that a given <see cref="DateTime" /> is the current date/time or earlier, within
    ///   a small allowance for clock skew.
    /// </summary>
    /// 
    /// <seealso cref="FluentValidation.Validators.PropertyValidator" />
    /// 
    public class DateTimeIsNowOrEarlierValidator : PropertyValidator
    {
        /// <summary>The number of seconds to allow for clock skew.</summary>
        internal const int SkewAllowanceSeconds = 300;

        /// <summary>The instance of the clock to use for date/time operations.</summary>
        private readonly IClock clock;

        /// <summary>
        ///   Initializes a new instance of the <see cref="DateTimeIsNowOrEarlierValidator"/> class.
        /// </summary>
        /// 
        /// <param name="clock">The clock instance to use for date/time operations.</param>
        ///         
        public DateTimeIsNowOrEarlierValidator(IClock clock) : base("The specified date/time must be either now or earlier.")
        {
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        /// <summary>
        ///   Determines if the given context is valid.
        /// </summary>
        /// 
        /// <param name="context">The context to consider.</param>
        /// 
        /// <returns><c>true</c> if the specified context is valid; otherwise, <c>false</c>.</returns>
        /// 
        protected override bool IsValid(PropertyValidatorContext context)
        {
            if (context.PropertyValue is DateTime dateTimeValue)
            {
                Instant instantValue;

                switch (dateTimeValue.Kind)
                {
                    case DateTimeKind.Utc:
                      instantValue = Instant.FromDateTimeUtc(dateTimeValue);
                      break;

                    case DateTimeKind.Local:

                      instantValue = LocalDateTime.FromDateTime(dateTimeValue)
                          .InZoneStrictly(DateTimeZoneProviders.Tzdb.GetSystemDefault())
                          .ToInstant();
                      break;

                    default:
                        return false;
                }

                return instantValue <= clock.GetCurrentInstant().Plus(Duration.FromSeconds(DateTimeIsNowOrEarlierValidator.SkewAllowanceSeconds));
            }

            return false;
        }
    }
}
