using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation;
using OrderFulfillment.Core.Extensions;
using OrderFulfillment.Core.Models.Errors;
using OrderFulfillment.Core.Validators;
using Xunit;

namespace OrderFulfillment.Core.Tests.Validators
{
    /// <summary>
    ///     The suite of tests for the <see cref="ValidatorBase{T}" />
    ///     class.
    /// </summary>
    /// 
    public class ValidatorBaseTests
    {        
        /// <summary>
        ///   Verifies that the base validator implements the expected interface contract.
        /// </summary>
        /// 
        [Fact]
        public void ImplementsIValidatorTBase()
        {
            var validator = new SimpleClassValidator() as Core.Validators.IValidator<SimpleClass>;
            validator.Should().NotBeNull("because the base validator should impleent the base IValidator<T> contract");
        }

        /// <summary>
        ///   Verifies that the base validator implements the expected interface contract.
        /// </summary>
        /// 
        [Fact]
        public void ImplementsIValidatorBase()
        {
            var validator = new SimpleClassValidator() as Core.Validators.IValidator;
            validator.Should().NotBeNull("because the base validator should impleent the base IValidator contract");
        }

        /// <summary>
        ///   Verifies that the non-generic IValidator implementation validates the type
        ///   being passed.
        /// </summary>
        /// 
        /// <param name="actionUnderTest">The action to execute for validation.</param>
        /// <param name="failureMessage">The message to pass on failure.</param>
        /// 
        [Fact]
        public void BaseValidatorValidatesArgumentsWithNoRuleNames()
        {
             var validator = new SimpleClassValidator() as Core.Validators.IValidator;
             
             Action underTest = () => validator.Validate(new object());
             underTest.ShouldThrow<ArgumentException>("because the type of argument should be verified");
        }

        /// <summary>
        ///   Verifies that the non-generic IValidator implementation validates the type
        ///   being passed.
        /// </summary>
        /// 
        /// <param name="actionUnderTest">The action to execute for validation.</param>
        /// <param name="failureMessage">The message to pass on failure.</param>
        /// 
        [Fact]
        public void BaseValidatorValidatesArgumentsWithRuleNames()
        {
             var validator = new SimpleClassValidator() as Core.Validators.IValidator;
             
             Action underTest = () => validator.Validate(new object(), "ruleOne", "ruleTwo");
             underTest.ShouldThrow<ArgumentException>("because the type of argument should be verified");
        }

        /// <summary>
        ///   Verifies that the non-generic IValidator implementation validates the type
        ///   being passed.
        /// </summary>
        /// 
        /// <param name="actionUnderTest">The action to execute for validation.</param>
        /// <param name="failureMessage">The message to pass on failure.</param>
        /// 
        [Fact]
        public void BaseValidatorValidatesArgumentsWithNoRuleNamesAsync()
        {
             var validator = new SimpleClassValidator() as Core.Validators.IValidator;
             
             Func<Task> underTest = async () => await validator.ValidateAsync(new object());
             underTest.ShouldThrow<ArgumentException>("because the type of argument should be verified");
        }

        /// <summary>
        ///   Verifies that the non-generic IValidator implementation validates the type
        ///   being passed.
        /// </summary>
        /// 
        /// <param name="actionUnderTest">The action to execute for validation.</param>
        /// <param name="failureMessage">The message to pass on failure.</param>
        /// 
        [Fact]
        public void BaseValidatorValidatesArgumentsWithRuleNamesAsync()
        {
             var validator = new SimpleClassValidator() as Core.Validators.IValidator;
             
             Func<Task> underTest = async () => await validator.ValidateAsync(new object(), "ruleOne");
             underTest.ShouldThrow<ArgumentException>("because the type of argument should be verified");
        }

        /// <summary>
        ///   Verifies that the base validator correctly recognizes a valid
        ///   set of data.
        /// </summary>
        /// 
        [Fact]
        public void ValidClassIsValidWithDefaultRules()
        {
            var simple    = new SimpleClass { Number = 2, Text = "A value" };
            var validator = new SimpleClassValidator() as Core.Validators.IValidator;
            var result    = validator?.Validate(simple);

            validator.Should().NotBeNull("because the base validator should impleent the base IValidator<T> contract");
            result.Should().NotBeNull("because a set should be returned even for valid objects");
            result.Should().HaveCount(0, "because a valid object should have no errors");
        }

        /// <summary>
        ///   Verifies that the base validator correctly recognizes a valid
        ///   set of data.
        /// </summary>
        /// 
        [Fact]
        public void ValidClassIsValidWithCustomRules()
        {
            var simple    = new SimpleClass { Number = 2, Text = "A value" };
            var complex   = new ComplexClass { Id = Guid.NewGuid(), Child = simple };
            var validator = new ComplexClassValidator(new SimpleClassValidator()) as Core.Validators.IValidator<ComplexClass>;
            var result    = validator?.Validate(complex, ComplexClassValidator.RuleSetName);

            validator.Should().NotBeNull("because the base validator should impleent the base IValidator<T> contract");
            result.Should().NotBeNull("because a set should be returned even for valid objects");
            result.Should().HaveCount(0, "because a valid object should have no errors");
        }

        /// <summary>
        ///   Verifies that the base validator correctly recognizes an invalid
        ///   set of data.
        /// </summary>
        /// 
        [Fact]
        public void InValidClassIsVDetectedWithDefaultRules()
        {
            var simple    = new SimpleClass { Number = 999, Text = null };
            var validator = new SimpleClassValidator() as Core.Validators.IValidator<SimpleClass>;
            var result    = validator?.Validate(simple);

            validator.Should().NotBeNull("because the base validator should impleent the base IValidator<T> contract");
            result.Should().NotBeNull("because a set should be returned even for valid objects");
            result.Should().HaveCount(2, "because both properties were invalid");
            result.Count(error => error.Code == ErrorCode.NumberIsOutOfRange.ToString()).Should().Be(1, "because the number property was the only property with a range error");
            result.Count(error => error.Code == ErrorCode.ValueIsRequired.ToString()).Should().Be(1, "because the text property was the only property missing a value");
        }

        /// <summary>
        ///   Verifies that the base validator correctly recognizes an invalid
        ///   set of data.
        /// </summary>
        /// 
        [Fact]
        public void InValidClassIsVDetectedWithCustomRules()
        {
            var simple    = new SimpleClass { Number = 999, Text = null };
            var complex   = new ComplexClass { Id = Guid.Empty, Child = simple };
            var validator = new ComplexClassValidator(new SimpleClassValidator()) as Core.Validators.IValidator;
            var result    = validator?.Validate(complex, ComplexClassValidator.RuleSetName);

            validator.Should().NotBeNull("because the base validator should impleent the base IValidator<T> contract");
            result.Should().NotBeNull("because a set should be returned even for valid objects");
            result.Should().HaveCount(3, "because the Id and both child properties were invalid");
            result.Count(error => error.Code == ErrorCode.InvalidValue.ToString()).Should().Be(1, "because the id property was the only property with a bad value");
            result.Count(error => error.Code == ErrorCode.NumberIsOutOfRange.ToString()).Should().Be(1, "because the number property was the only property with a range error");
            result.Count(error => error.Code == ErrorCode.ValueIsRequired.ToString()).Should().Be(1, "because the text property was the only property missing a value");
        }

        /// <summary>
        ///   Verifies that the base validator respects the requested rule set.
        /// </summary>
        /// 
        [Fact]
        public void CustomRuleIsNotRunWhenNotRequested()
        {
            var simple    = new SimpleClass { Number = 999, Text = null };
            var complex   = new ComplexClass { Id = Guid.Empty, Child = simple };
            var validator = new ComplexClassValidator(new SimpleClassValidator()) as Core.Validators.IValidator<ComplexClass>;
            var result    = validator?.Validate(complex);

            validator.Should().NotBeNull("because the base validator should impleent the base IValidator<T> contract");
            result.Should().NotBeNull("because a set should be returned even for valid objects");
            result.Should().HaveCount(1, "because child should not have been validated");
            result.Count(error => error.Code == ErrorCode.InvalidValue.ToString()).Should().Be(1, "because the id property was the only property with a bad value");
        }

        /// <summary>
        ///   Verifies that the base validator correctly recognizes a valid
        ///   set of data.
        /// </summary>
        /// 
        [Fact]
        public async Task ValidClassIsValidWithDefaultRulesForAsync()
        {
            var simple    = new SimpleClass { Number = 2, Text = "A value" };
            var validator = new SimpleClassValidator() as Core.Validators.IValidator;
            var result    = await validator?.ValidateAsync(simple);

            validator.Should().NotBeNull("because the base validator should impleent the base IValidator<T> contract");
            result.Should().NotBeNull("because a set should be returned even for valid objects");
            result.Should().HaveCount(0, "because a valid object should have no errors");
        }

        /// <summary>
        ///   Verifies that the base validator correctly recognizes a valid
        ///   set of data.
        /// </summary>
        /// 
        [Fact]
        public async Task ValidClassIsValidWithCustomRulesForAsync()
        {
            var simple    = new SimpleClass { Number = 2, Text = "A value" };
            var complex   = new ComplexClass { Id = Guid.NewGuid(), Child = simple };
            var validator = new ComplexClassValidator(new SimpleClassValidator()) as Core.Validators.IValidator<ComplexClass>;
            var result    = await validator?.ValidateAsync(complex, ComplexClassValidator.RuleSetName);

            validator.Should().NotBeNull("because the base validator should impleent the base IValidator<T> contract");
            result.Should().NotBeNull("because a set should be returned even for valid objects");
            result.Should().HaveCount(0, "because a valid object should have no errors");
        }

        /// <summary>
        ///   Verifies that the base validator correctly recognizes an invalid
        ///   set of data.
        /// </summary>
        /// 
        [Fact]
        public async Task InValidClassIsVDetectedWithDefaultRulesForAsync()
        {
            var simple    = new SimpleClass { Number = 999, Text = null };
            var validator = new SimpleClassValidator() as Core.Validators.IValidator<SimpleClass>;
            var result    = await validator?.ValidateAsync(simple);

            validator.Should().NotBeNull("because the base validator should impleent the base IValidator<T> contract");
            result.Should().NotBeNull("because a set should be returned even for valid objects");
            result.Should().HaveCount(2, "because both properties were invalid");
            result.Count(error => error.Code == ErrorCode.NumberIsOutOfRange.ToString()).Should().Be(1, "because the number property was the only property with a range error");
            result.Count(error => error.Code == ErrorCode.ValueIsRequired.ToString()).Should().Be(1, "because the text property was the only property missing a value");
        }

        /// <summary>
        ///   Verifies that the base validator correctly recognizes an invalid
        ///   set of data.
        /// </summary>
        /// 
        [Fact]
        public async Task InValidClassIsVDetectedWithCustomRulesForAsync()
        {
            var simple    = new SimpleClass { Number = 999, Text = null };
            var complex   = new ComplexClass { Id = Guid.Empty, Child = simple };
            var validator = new ComplexClassValidator(new SimpleClassValidator()) as Core.Validators.IValidator;
            var result    = await validator?.ValidateAsync(complex, ComplexClassValidator.RuleSetName);

            validator.Should().NotBeNull("because the base validator should impleent the base IValidator<T> contract");
            result.Should().NotBeNull("because a set should be returned even for valid objects");
            result.Should().HaveCount(3, "because the Id and both child properties were invalid");
            result.Count(error => error.Code == ErrorCode.InvalidValue.ToString()).Should().Be(1, "because the id property was the only property with a bad value");
            result.Count(error => error.Code == ErrorCode.NumberIsOutOfRange.ToString()).Should().Be(1, "because the number property was the only property with a range error");
            result.Count(error => error.Code == ErrorCode.ValueIsRequired.ToString()).Should().Be(1, "because the text property was the only property missing a value");
        }

        /// <summary>
        ///   Verifies that the base validator respects the requested rule set.
        /// </summary>
        /// 
        [Fact]
        public async Task CustomRuleIsNotRunWhenNotRequestedForAsync()
        {
            var simple    = new SimpleClass { Number = 999, Text = null };
            var complex   = new ComplexClass { Id = Guid.Empty, Child = simple };
            var validator = new ComplexClassValidator(new SimpleClassValidator()) as Core.Validators.IValidator<ComplexClass>;
            var result    = await validator?.ValidateAsync(complex);

            validator.Should().NotBeNull("because the base validator should impleent the base IValidator<T> contract");
            result.Should().NotBeNull("because a set should be returned even for valid objects");
            result.Should().HaveCount(1, "because child should not have been validated");
            result.Count(error => error.Code == ErrorCode.InvalidValue.ToString()).Should().Be(1, "because the id property was the only property with a bad value");
        }
        
        #region Nested Classes

            private class ComplexClass
            {
                public Guid Id { get;  set; }
                public SimpleClass Child { get;  set; }
            }
            
            private class SimpleClass
            {
                public string Text { get;  set; }
                public int Number { get;  set; }
            }
            
            private class SimpleClassValidator : ValidatorBase<SimpleClass>
            {
                public SimpleClassValidator()
            {
                this.RuleFor(simple => simple.Text).NotEmpty().WithErrorCode(ErrorCode.ValueIsRequired)
                                                   .Length(1, 10).WithErrorCode(ErrorCode.LengthIsInvalid);

                this.RuleFor(simple => simple.Number).InclusiveBetween(1, 5).WithErrorCode(ErrorCode.NumberIsOutOfRange);
            }
            }
            
            private class ComplexClassValidator : ValidatorBase<ComplexClass>
            {
                public const string RuleSetName = "child";

                public ComplexClassValidator(SimpleClassValidator childValidator)
            {
                this.RuleFor(complex => complex.Id).NotEqual(Guid.Empty).WithErrorCode(ErrorCode.InvalidValue);

                this.RuleSet(ComplexClassValidator.RuleSetName, () =>
                {
                    this.RuleFor(complex => complex.Child).SetValidator(childValidator);
                });
            }
            }

        #endregion
    }
}
