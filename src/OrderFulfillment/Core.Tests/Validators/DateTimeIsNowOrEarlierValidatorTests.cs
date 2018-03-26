using System;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation;
using OrderFulfillment.Core.Extensions;
using OrderFulfillment.Core.Models.Errors;
using OrderFulfillment.Core.Validators;
using NodaTime;
using NodaTime.Testing;
using Xunit;


namespace OrderFulfillment.Core.Tests.Validators
{
    /// <summary>
    ///   The suite of tests for the <see cref="DateTimeIsNowOrEarlierValidator" /> class.
    /// </summary>
    /// 
    public class DateTimeIsNowOrEarlierValidatorTests
    {
        /// <summary>
        ///   Verifies functionality of the validator by calling it's Validate method.
        /// </summary>
        /// 
        [Fact]
        public async Task UtcDateBeforeNowIsValid()
        {
            var clock     = new FakeClock(Instant.FromUtc(2015, 2, 2, 12, 0));
            var validDate = new DateTime(2015, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var validator = new FakeDateTimeValidator(clock);
            var actual    = await validator.ValidateAsync(validDate);

            actual.Should().BeEmpty();
        }

        /// <summary>
        ///   Verifies functionality of the validator by calling it's Validate method.
        /// </summary>
        /// 
        [Fact]
        public async Task UtcDateAfterNowIsValidIfWithinSkew()
        {
            var clock     = new FakeClock(Instant.FromUtc(2015, 2, 2, 12, 0));
            var validDate = new DateTime(2015, 2, 2, 12, 0, 0, DateTimeKind.Utc).AddSeconds(DateTimeIsNowOrEarlierValidator.SkewAllowanceSeconds);
            var validator = new FakeDateTimeValidator(clock);
            var actual    = await validator.ValidateAsync(validDate);

            actual.Should().BeEmpty();
        }

        /// <summary>
        ///   Verifies functionality of the validator by calling it's Validate method.
        /// </summary>
        ///
        [Fact]
        public async Task UtcDateAfterNowIsNotValidIfMoreThanAllowedSkew()
        {
            var clock     = new FakeClock(Instant.FromUtc(2015, 2, 2, 12, 0));
            var validDate = new DateTime(2015, 2, 2, 12, 0, 0, DateTimeKind.Utc).AddSeconds(DateTimeIsNowOrEarlierValidator.SkewAllowanceSeconds + 1);
            var validator = new FakeDateTimeValidator(clock);
            var actual    = await validator.ValidateAsync(validDate);

            actual.Should().Contain(e => e.Code == ErrorCode.InvalidValue.ToString());
        }

        /// <summary>
        ///   Verifies functionality of the validator by calling it's Validate method.
        /// </summary>
        /// 
        [Fact]
        public async Task LocalDateBeforeNowIsValid()
        {
            var clockDate  = new DateTime(2015, 2, 2, 12, 0, 0, DateTimeKind.Local);
            var clockLocal = LocalDateTime.FromDateTime(clockDate);
            var clockZoned = clockLocal.InZoneStrictly(DateTimeZoneProviders.Tzdb.GetSystemDefault());
            var clock      = new FakeClock(clockZoned.ToInstant());

            var validDate = new DateTime(2015, 1, 1, 0, 0, 0, DateTimeKind.Local);
            var validator = new FakeDateTimeValidator(clock);
            var actual    = await validator.ValidateAsync(validDate);

            actual.Should().BeEmpty();
        }

        /// <summary>
        ///   Verifies functionality of the validator by calling it's Validate method.
        /// </summary>
        /// 
        [Fact]
        public async Task LocalDateAfterNowIsValidIfWithinSkew()
        {
            var clockDate  = new DateTime(2015, 2, 2, 12, 0, 0, DateTimeKind.Local);
            var clockLocal = LocalDateTime.FromDateTime(clockDate);
            var clockZoned = clockLocal.InZoneStrictly(DateTimeZoneProviders.Tzdb.GetSystemDefault());
            var clock      = new FakeClock(clockZoned.ToInstant());

            var validDate  = new DateTime(2015, 2, 2, 12, 0, 0, DateTimeKind.Local).AddSeconds(DateTimeIsNowOrEarlierValidator.SkewAllowanceSeconds);
            var validator  = new FakeDateTimeValidator(clock);
            var actual     = await validator.ValidateAsync(validDate);

            actual.Should().BeEmpty();
        }

        /// <summary>
        ///   Verifies functionality of the validator by calling it's Validate method.
        /// </summary>
        ///
        [Fact]
        public async Task LocalDateAfterNowIsNotValidIfMoreThanAllowedSkew()
        {
            var clockDate  = new DateTime(2015, 2, 2, 12, 0, 0, DateTimeKind.Local);
            var clockLocal = LocalDateTime.FromDateTime(clockDate);
            var clockZoned = clockLocal.InZoneStrictly(DateTimeZoneProviders.Tzdb.GetSystemDefault());
            var clock      = new FakeClock(clockZoned.ToInstant());

            var validDate = new DateTime(2015, 2, 2, 12, 0, 0, DateTimeKind.Local).AddSeconds(DateTimeIsNowOrEarlierValidator.SkewAllowanceSeconds + 1);
            var validator = new FakeDateTimeValidator(clock);
            var actual    = await validator.ValidateAsync(validDate);

            actual.Should().Contain(e => e.Code == ErrorCode.InvalidValue.ToString());
        }

        /// <summary>
        ///   Verifies functionality of the validator by calling it's Validate method.
        /// </summary>
        ///
        [Fact]
        public async Task UnspecifiedDateBeforeNowIsNotValid()
        {
            var clock     = new FakeClock(Instant.FromUtc(2015, 2, 2, 12, 0));
            var validDate = new DateTime(2015, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
            var validator = new FakeDateTimeValidator(clock);
            var actual    = await validator.ValidateAsync(validDate);

            actual.Should().Contain(e => e.Code == ErrorCode.InvalidValue.ToString());
        }

        /// <summary>
        ///   Verifies functionality of the validator by calling it's Validate method.
        /// </summary>
        ///
        [Fact]
        public async Task NullableNullDateIsInvalid()
        {
            var clock     = new FakeClock(Instant.FromUtc(2015, 2, 2, 12, 0));
            var validDate = (DateTime?)null;
            var validator = new FakeNullableDateTimeValidator(clock);
            var actual    = await validator.ValidateAsync(validDate);

            actual.Should().Contain(e => e.Code == ErrorCode.InvalidValue.ToString());
        }

        /// <summary>
        ///   Verifies functionality of the validator by calling it's Validate method.
        /// </summary>
        /// 
        [Fact]
        public async Task NullableUtcDateBeforeNowIsValid()
        {
            var clock     = new FakeClock(Instant.FromUtc(2015, 2, 2, 12, 0));
            var validDate = (DateTime?) new DateTime(2015, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var validator = new FakeDateTimeValidator(clock);
            var actual    = await validator.ValidateAsync(validDate);

            actual.Should().BeEmpty();
        }

        /// <summary>
        ///   Verifies functionality of the validator by calling it's Validate method.
        /// </summary>
        /// 
        [Fact]
        public async Task NullableUtcDateAfterNowIsValidIfWithinSkew()
        {
            var clock     = new FakeClock(Instant.FromUtc(2015, 2, 2, 12, 0));
            var validDate = (DateTime?) new DateTime(2015, 2, 2, 12, 0, 0, DateTimeKind.Utc).AddSeconds(DateTimeIsNowOrEarlierValidator.SkewAllowanceSeconds);
            var validator = new FakeDateTimeValidator(clock);
            var actual    = await validator.ValidateAsync(validDate);

            actual.Should().BeEmpty();
        }

        /// <summary>
        ///   Verifies functionality of the validator by calling it's Validate method.
        /// </summary>
        ///
        [Fact]
        public async Task NullableUtcDateAfterNowIsNotValidIfMoreThanAllowedSkew()
        {
            var clock     = new FakeClock(Instant.FromUtc(2015, 2, 2, 12, 0));
            var validDate = (DateTime?)new DateTime(2015, 2, 2, 12, 0, 0, DateTimeKind.Utc).AddSeconds(DateTimeIsNowOrEarlierValidator.SkewAllowanceSeconds + 1);
            var validator = new FakeDateTimeValidator(clock);
            var actual    = await validator.ValidateAsync(validDate);

            actual.Should().Contain(e => e.Code == ErrorCode.InvalidValue.ToString());
        }

        /// <summary>
        ///   Verifies functionality of the validator by calling it's Validate method.
        /// </summary>
        /// 
        [Fact]
        public async Task NullableLocalDateBeforeNowIsValid()
        {
            var clockDate  = new DateTime(2015, 2, 2, 12, 0, 0, DateTimeKind.Local);
            var clockLocal = LocalDateTime.FromDateTime(clockDate);
            var clockZoned = clockLocal.InZoneStrictly(DateTimeZoneProviders.Tzdb.GetSystemDefault());
            var clock      = new FakeClock(clockZoned.ToInstant());

            var validDate = (DateTime?) new DateTime(2015, 1, 1, 0, 0, 0, DateTimeKind.Local);
            var validator = new FakeDateTimeValidator(clock);
            var actual    = await validator.ValidateAsync(validDate);

            actual.Should().BeEmpty();
        }

        /// <summary>
        ///   Verifies functionality of the validator by calling it's Validate method.
        /// </summary>
        /// 
        [Fact]
        public async Task NullableLocalDateAfterNowIsValidIfWithinSkew()
        {
            var clockDate  = new DateTime(2015, 2, 2, 12, 0, 0, DateTimeKind.Local);
            var clockLocal = LocalDateTime.FromDateTime(clockDate);
            var clockZoned = clockLocal.InZoneStrictly(DateTimeZoneProviders.Tzdb.GetSystemDefault());
            var clock      = new FakeClock(clockZoned.ToInstant());

            var validDate  = (DateTime?) new DateTime(2015, 2, 2, 12, 0, 0, DateTimeKind.Local).AddSeconds(DateTimeIsNowOrEarlierValidator.SkewAllowanceSeconds);
            var validator  = new FakeDateTimeValidator(clock);
            var actual     = await validator.ValidateAsync(validDate);

            actual.Should().BeEmpty();
        }

        /// <summary>
        ///   Verifies functionality of the validator by calling it's Validate method.
        /// </summary>
        ///
        [Fact]
        public async Task NullableLocalDateAfterNowIsNotValidIfMoreThanAllowedSkew()
        {
            var clockDate  = new DateTime(2015, 2, 2, 12, 0, 0, DateTimeKind.Local);
            var clockLocal = LocalDateTime.FromDateTime(clockDate);
            var clockZoned = clockLocal.InZoneStrictly(DateTimeZoneProviders.Tzdb.GetSystemDefault());
            var clock      = new FakeClock(clockZoned.ToInstant());

            var validDate = (DateTime?) new DateTime(2015, 2, 2, 12, 0, 0, DateTimeKind.Local).AddSeconds(DateTimeIsNowOrEarlierValidator.SkewAllowanceSeconds + 1);
            var validator = new FakeDateTimeValidator(clock);
            var actual    = await validator.ValidateAsync(validDate);

            actual.Should().Contain(e => e.Code == ErrorCode.InvalidValue.ToString());
        }

        /// <summary>
        ///   Verifies functionality of the validator by calling it's Validate method.
        /// </summary>
        ///
        [Fact]
        public async Task NullableUnspecifiedDateBeforeNowIsNotValid()
        {
            var clock     = new FakeClock(Instant.FromUtc(2015, 2, 2, 12, 0));
            var validDate = (DateTime?) new DateTime(2015, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
            var validator = new FakeDateTimeValidator(clock);
            var actual    = await validator.ValidateAsync(validDate);

            actual.Should().Contain(e => e.Code == ErrorCode.InvalidValue.ToString());
        }

        #region Nested Classes

        private class FakeDateTimeValidator : MessageValidatorBase<DateTime>
            {
                public FakeDateTimeValidator(IClock clock)
                {
                    this.RuleFor(value => value)
                        .SetValidator(new DateTimeIsNowOrEarlierValidator(clock))
                        .WithErrorCode(ErrorCode.InvalidValue);
                }
            }

            private class FakeNullableDateTimeValidator : MessageValidatorBase<Nullable<DateTime>>
            {
                public FakeNullableDateTimeValidator(IClock clock)
                {
                    this.RuleFor(value => value)
                        .SetValidator(new DateTimeIsNowOrEarlierValidator(clock))
                        .WithErrorCode(ErrorCode.InvalidValue);
                }
            }
            
        #endregion
    }
}
