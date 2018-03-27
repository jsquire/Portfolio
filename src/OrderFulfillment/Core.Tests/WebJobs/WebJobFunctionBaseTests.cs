using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using OrderFulfillment.Core.Commands;
using OrderFulfillment.Core.WebJobs;
using Moq;
using NodaTime;
using NodaTime.Testing;
using Xunit;

namespace OrderFulfillment.Core.Tests.WebJobs
{
    /// <summary>
    ///   The suite of tests for the <see cref="WebJobFunctionBase"/>
    ///   class.
    /// </summary>
    /// 
    public class WebJobFunctionBaseTests
    {
        /// <summary>
        ///    Verifies functionality of the <see cref="WebJobFunctionBase.Dispose" />
        ///    method;
        /// </summary>
        /// 
        [Fact]
        public void LifetimeScopeIsDisposedWhenTheClassIsDisposed()
        {
            var mockLifetimeScope = new Mock<IDisposable>();
            var functionBase      = new WebJobFunctionBase(mockLifetimeScope.Object);

            functionBase.Dispose();

            mockLifetimeScope.Verify(scope => scope.Dispose(), Times.AtLeastOnce, "The lifetime scope should have been disposed when the class was disposed");
        }

        /// <summary>
        ///    Verifies functionality of the <see cref="WebJobFunctionBase.Dispose" />
        ///    method;
        /// </summary>
        /// 
        [Fact]
        public void NullLifetimeScopeIsAllowed()
        {
           var functionBase = new WebJobFunctionBase(null);

           Action actionUnderTest = () => functionBase.Dispose();

           actionUnderTest.ShouldNotThrow("because a null lifetime scope should not be attempted to be disposed");
        }

        /// <summary>
        ///    Verifies functionality of the <see cref="WebJobFunctionBase.ScheduleCommandForRetryIfEligibleAsync" />
        ///    method;
        /// </summary>
        /// 
        [Fact]
        public void ScheduleCommandForRetryIfEligibleAsyncValidatesTheCommand()
        {
            var functionBase = new TestWebJobFunction(null);

            Action actionUnderTest = () => functionBase.ScheduleCommandForRetryIfEligibleAsync(null, new CommandRetryThresholds(), new Random(), Mock.Of<IClock>(), Mock.Of<ICommandPublisher<ProcessOrder>>()).GetAwaiter().GetResult();
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the command is required");
        }

        /// <summary>
        ///    Verifies functionality of the <see cref="WebJobFunctionBase.ScheduleCommandForRetryIfEligibleAsync" />
        ///    method;
        /// </summary>
        /// 
        [Fact]
        public void ScheduleCommandForRetryIfEligibleAsyncValidatesTheThresholdsd()
        {
            var functionBase = new TestWebJobFunction(null);

            Action actionUnderTest = () => functionBase.ScheduleCommandForRetryIfEligibleAsync(new ProcessOrder(), null, new Random(), Mock.Of<IClock>(), Mock.Of<ICommandPublisher<ProcessOrder>>()).GetAwaiter().GetResult();
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the thresholds are required");
        }

        /// <summary>
        ///    Verifies functionality of the <see cref="WebJobFunctionBase.ScheduleCommandForRetryIfEligibleAsync" />
        ///    method;
        /// </summary>
        /// 
        [Fact]
        public void ScheduleCommandForRetryIfEligibleAsyncValidatesTheRandomNumberGenerator()
        {
            var functionBase = new TestWebJobFunction(null);

            Action actionUnderTest = () => functionBase.ScheduleCommandForRetryIfEligibleAsync(new ProcessOrder(), new CommandRetryThresholds(), null, Mock.Of<IClock>(), Mock.Of<ICommandPublisher<ProcessOrder>>()).GetAwaiter().GetResult();
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the random number generator is required");
        }

        /// <summary>
        ///    Verifies functionality of the <see cref="WebJobFunctionBase.ScheduleCommandForRetryIfEligibleAsync" />
        ///    method;
        /// </summary>
        /// 
        [Fact]
        public void ScheduleCommandForRetryIfEligibleAsyncValidatesTheClock()
        {
            var functionBase = new TestWebJobFunction(null);

            Action actionUnderTest = () => functionBase.ScheduleCommandForRetryIfEligibleAsync(new ProcessOrder(), new CommandRetryThresholds(), new Random(), null, Mock.Of<ICommandPublisher<ProcessOrder>>()).GetAwaiter().GetResult();
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the clock is required");
        }

        /// <summary>
        ///    Verifies functionality of the <see cref="WebJobFunctionBase.ScheduleCommandForRetryIfEligibleAsync" />
        ///    method;
        /// </summary>
        ///
        [Fact]
        public void ScheduleCommandForRetryIfEligibleAsyncValidatesThePublisher()
        {
            var functionBase = new TestWebJobFunction(null);

            Action actionUnderTest = () => functionBase.ScheduleCommandForRetryIfEligibleAsync(new ProcessOrder(), new CommandRetryThresholds(), new Random(), Mock.Of<IClock>(), null).GetAwaiter().GetResult();
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the command publisher is required");
        }

        /// <summary>
        ///    Verifies functionality of the <see cref="WebJobFunctionBase.ScheduleCommandForRetryIfEligibleAsync" />
        ///    method;
        /// </summary>
        /// 
        [Fact]
        public async Task ScheduleCommandForRetryIfEligibleAsyncDoesNotScheduleIfRetryCountIsExceeded()
        {
            var functionBase = new TestWebJobFunction(null);
            var thresholds   = new CommandRetryThresholds { CommandRetryMaxCount = 2 };
            var command      = new SubmitOrderForProduction { PreviousAttemptsToHandleCount = 3 };
            var result       = await functionBase.ScheduleCommandForRetryIfEligibleAsync(command, thresholds, new Random(), Mock.Of<IClock>(), Mock.Of<ICommandPublisher<SubmitOrderForProduction>>());

            result.Should().BeFalse("because the command had no remaining retries");
        }

        /// <summary>
        ///    Verifies functionality of the <see cref="WebJobFunctionBase.ScheduleCommandForRetryIfEligibleAsync" />
        ///    method;
        /// </summary>
        /// 
        [Fact]
        public async Task ScheduleCommandForRetryIfEligibleAsyncSchedulesTheCommand()
        {
            var initialPublishCount = 2;
            var mockPublisher       = new Mock<ICommandPublisher<SubmitOrderForProduction>>();
            var fakeClock           = new FakeClock(Instant.FromDateTimeUtc(new DateTime(2011, 11, 14, 03, 56, 22, DateTimeKind.Utc)));
            var functionBase        = new TestWebJobFunction(null);
            var thresholds          = new CommandRetryThresholds { CommandRetryMaxCount = 4, CommandRetryExponentialSeconds = 1, CommandRetryJitterSeconds = 1 };
            var command             = new SubmitOrderForProduction { PreviousAttemptsToHandleCount = initialPublishCount };
            var result              = await functionBase.ScheduleCommandForRetryIfEligibleAsync(command, thresholds, new Random(), fakeClock, mockPublisher.Object);

            result.Should().BeTrue("because the command should be recognized as schedulable");
            command.PreviousAttemptsToHandleCount.Should().Be(initialPublishCount + 1, "because the publish should should have been incremented");

            mockPublisher.Verify(publisher => publisher.PublishAsync(It.Is<SubmitOrderForProduction>(value => value == command), It.Is<Instant?>(value => value.HasValue)), 
                Times.Once, 
                "The command should have been published");
        }

        /// <summary>
        ///    Verifies functionality of the <see cref="WebJobFunctionBase.ScheduleCommandForRetryIfEligibleAsync" />
        ///    method;
        /// </summary>
        /// 
        [Fact]
        public async Task ScheduleCommandForRetryIfEligibleAsyncSchedulesInTheFuture()
        {
            var initialPublishCount = 2;
            var publishTimes        = new List<Instant>();
            var mockPublisher       = new Mock<ICommandPublisher<SubmitOrderForProduction>>();
            var fakeClock           = new FakeClock(Instant.FromDateTimeUtc(new DateTime(2011, 11, 14, 03, 56, 22, DateTimeKind.Utc)));
            var functionBase        = new TestWebJobFunction(null);
            var thresholds          = new CommandRetryThresholds { CommandRetryMaxCount = 4, CommandRetryExponentialSeconds = 5, CommandRetryJitterSeconds = 1 };
            var command             = new SubmitOrderForProduction { PreviousAttemptsToHandleCount = initialPublishCount };
            var result              = await functionBase.ScheduleCommandForRetryIfEligibleAsync(command, thresholds, new Random(), fakeClock, mockPublisher.Object);            

            mockPublisher
                .Setup(publisher => publisher.PublishAsync(It.Is<SubmitOrderForProduction>(value => value == command), It.Is<Instant?>(value => value.HasValue)))
                .Returns(Task.CompletedTask)
                .Callback<SubmitOrderForProduction, Instant?>( (publishedCommand, publishTime) => publishTimes.Add(publishTime.Value));



            result.Should().BeTrue("because the command should be recognized as schedulable");
            publishTimes.Count(time => time > fakeClock.GetCurrentInstant()).Should().Be(publishTimes.Count, "because all of the scheduled publish times should have been in the future");
        }

        #region NestedTypes

            public class TestWebJobFunction : WebJobFunctionBase
            {
                public TestWebJobFunction(IDisposable lifetimeScope) : base (lifetimeScope)
                {
                }

                public new Task<bool> ScheduleCommandForRetryIfEligibleAsync<TCommand>(TCommand                   command,
                                                                                       CommandRetryThresholds      retryThresholds,                                                                                            
                                                                                       Random                      rng,
                                                                                       IClock                      clock,                                                                                            
                                                                                       ICommandPublisher<TCommand> commandPublisher) where TCommand : CommandBase =>
                    base.ScheduleCommandForRetryIfEligibleAsync(command, retryThresholds, rng, clock, commandPublisher);
            }

        #endregion
    }
}
