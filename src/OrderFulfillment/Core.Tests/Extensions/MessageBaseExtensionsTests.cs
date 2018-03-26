using System;
using FluentAssertions;
using OrderFulfillment.Core.Commands;
using OrderFulfillment.Core.Events;
using OrderFulfillment.Core.Extensions;
using OrderFulfillment.Core.Infrastructure;
using Xunit;

namespace OrderFulfillment.Core.Tests.Extensions
{
    /// <summary>
    ///   The suite of unit tests for the <see cref="OrderFulfillment.Core.Extensions.MessageBaseExtensions" />
    ///   class.
    /// </summary>
    /// 
    public class MessageBaseExtensionsTests
    {
        /// <summary>
        ///   Verifies functionality of the <see cref="MessageBaseExtensions.CreateNewEvent" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void CreateNewEventWithNullInstance()
        {
            Action actionUnderTest = () => ((MessageBase)null).CreateNewEvent<EmptyEvent>();
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the instance was null");
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="MessageBaseExtensions.CreateNewCommand" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void CreateNewCommandWithNullInstance()
        {
            Action actionUnderTest = () => ((MessageBase)null).CreateNewCommand<EmptyCommand>();
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the instance was null");
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="MessageBaseExtensions.CreateNewEvent" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void CreateNewEventFromEventSourceWithNoMutator()
        {
            var source = new EmptyEvent
            {
               Id              = Guid.NewGuid(),
               CorrelationId   = "ABC123",
               CurrentUser     = "Some Guy",
               OccurredTimeUtc = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(1)),
            };

            var created = source.CreateNewEvent<EmptyEvent>();

            created.Should().NotBeNull("because the created instance should exist");
            
            // Verify new values are populated.

            created.Id.Should().NotBe(source.Id, "because the id should be a new value");
            created.Id.Should().NotBe(default(Guid), "because the id should be populated");
            created.OccurredTimeUtc.Should().NotBe(source.OccurredTimeUtc, "because the occurred time should be a new value");
            created.OccurredTimeUtc.Should().NotBe(default(DateTime), "because the occurred time should be populated");

            // Verify copied values are copied.

            created.CorrelationId.Should().Be(source.CorrelationId, "because the correlation identifier should be copied");
            created.CurrentUser.Should().Be(source.CurrentUser, "because the current user should be copied");
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="MessageBaseExtensions.CreateNewCommand" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void CreateNewCommandFromCommandSourceWithNoMutator()
        {
            var source = new EmptyCommand
            {
               Id              = Guid.NewGuid(),
               CorrelationId   = "ABC123",
               CurrentUser     = "Some Guy",
               OccurredTimeUtc = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(1))
            };

            var created = source.CreateNewCommand<EmptyCommand>();

            created.Should().NotBeNull("because the created instance should exist");
            
            // Verify new values are populated.

            created.Id.Should().NotBe(source.Id, "because the id should be a new value");
            created.Id.Should().NotBe(default(Guid), "because the id should be populated");
            created.OccurredTimeUtc.Should().NotBe(source.OccurredTimeUtc, "because the occurred time should be a new value");
            created.OccurredTimeUtc.Should().NotBe(default(DateTime), "because the occurred time should be populated");

            // Verify copied values are copied.

            created.CorrelationId.Should().Be(source.CorrelationId, "because the correlation identifier should be copied");
            created.CurrentUser.Should().Be(source.CurrentUser, "because the current user should be copied");
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="MessageBaseExtensions.CreateNewEvent" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void CreateNewEventFromCommandSourceWithMutator()
        {
            var newEventName = "Event Name";
            var newEventAge  = 22;

            var source = new TestCommand
            {
               Id              = Guid.NewGuid(),
               CorrelationId   = "ABC123",
               CurrentUser     = "Some Guy",
               OccurredTimeUtc = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(1)),
               Name            = "Command Name"               
            };

            var created = source.CreateNewEvent<TestEvent>(e =>
            {
                e.Name = newEventName;
                e.Age  = newEventAge;
            });

            created.Should().NotBeNull("because the created instance should exist");
            
            // Verify new values are populated.

            created.Id.Should().NotBe(source.Id, "because the id should be a new value");
            created.Id.Should().NotBe(default(Guid), "because the id should be populated");
            created.OccurredTimeUtc.Should().NotBe(source.OccurredTimeUtc, "because the occurred time should be a new value");
            created.OccurredTimeUtc.Should().NotBe(default(DateTime), "because the occurred time should be populated");

            // Verify copied values are copied.

            created.CorrelationId.Should().Be(source.CorrelationId, "because the correlation identifier should be copied");
            created.CurrentUser.Should().Be(source.CurrentUser, "because the current user should be copied");

            // Verify mutator values were set.

            created.Name.Should().Be(newEventName, "because the mutator values should be set");
            created.Age.Should().Be(newEventAge, "because the mutator values should be set");
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="MessageBaseExtensions.CreateNewCommand" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void CreateNewCommandFromEventSourceWithMutator()
        {
            var newCommandName = "Event Name";

            var source = new TestEvent
            {
               Id              = Guid.NewGuid(),
               CorrelationId   = "ABC123",
               CurrentUser     = "Some Guy",
               OccurredTimeUtc = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(1)),
               Name            = "EventName",
               Age             = 15
            };

            var created = source.CreateNewCommand<TestCommand>(c =>
            {
              c.Name = newCommandName;
            });

            created.Should().NotBeNull("because the created instance should exist");
            
            // Verify new values are populated.

            created.Id.Should().NotBe(source.Id, "because the id should be a new value");
            created.Id.Should().NotBe(default(Guid), "because the id should be populated");
            created.OccurredTimeUtc.Should().NotBe(source.OccurredTimeUtc, "because the occurred time should be a new value");
            created.OccurredTimeUtc.Should().NotBe(default(DateTime), "because the occurred time should be populated");

            // Verify copied values are copied.

            created.CorrelationId.Should().Be(source.CorrelationId, "because the correlation identifier should be copied");
            created.CurrentUser.Should().Be(source.CurrentUser, "because the current user should be copied");

            // Verify mutator values were set.

            created.Name.Should().Be(newCommandName, "because the mutator values should be set");
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="MessageBaseExtensions.CreateNewEvent" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void CreateNewEventFromCommandSourceWithNoMutator()
        {
            var source = new TestCommand
            {
               Id              = Guid.NewGuid(),
               CorrelationId   = "ABC123",
               CurrentUser     = "Some Guy",
               OccurredTimeUtc = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(1)),
               Name            = "Command Name"               
            };

            var created = source.CreateNewEvent<TestEvent>();

            created.Should().NotBeNull("because the created instance should exist");
            
            // Verify new values are populated.

            created.Id.Should().NotBe(source.Id, "because the id should be a new value");
            created.Id.Should().NotBe(default(Guid), "because the id should be populated");
            created.OccurredTimeUtc.Should().NotBe(source.OccurredTimeUtc, "because the occurred time should be a new value");
            created.OccurredTimeUtc.Should().NotBe(default(DateTime), "because the occurred time should be populated");

            // Verify copied values are copied.

            created.CorrelationId.Should().Be(source.CorrelationId, "because the correlation identifier should be copied");
            created.CurrentUser.Should().Be(source.CurrentUser, "because the current user should be copied");

            // Verify non-base values were not set.

            created.Name.Should().Be(default(string), "because non-base values should not be set");
            created.Age.Should().Be(default(int), "because non-base values should not be set");
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="MessageBaseExtensions.CreateNewCommand" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void CreateNewCommandFromEventSourceWithNoMutator()
        {
            var source = new TestEvent
            {
               Id              = Guid.NewGuid(),
               CorrelationId   = "ABC123",
               CurrentUser     = "Some Guy",
               OccurredTimeUtc = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(1)),
               Name            = "EventName",
               Age             = 15
            };

            var created = source.CreateNewCommand<TestCommand>();

            created.Should().NotBeNull("because the created instance should exist");
            
            // Verify new values are populated.

            created.Id.Should().NotBe(source.Id, "because the id should be a new value");
            created.Id.Should().NotBe(default(Guid), "because the id should be populated");
            created.OccurredTimeUtc.Should().NotBe(source.OccurredTimeUtc, "because the occurred time should be a new value");
            created.OccurredTimeUtc.Should().NotBe(default(DateTime), "because the occurred time should be populated");

            // Verify copied values are copied.

            created.CorrelationId.Should().Be(source.CorrelationId, "because the correlation identifier should be copied");
            created.CurrentUser.Should().Be(source.CurrentUser, "because the current user should be copied");

            // Verify non-base values were not set.

            created.Name.Should().Be(default(string), "because non-base values should not be set");
        }

        #region Nested Classes

            private class TestEvent : EventBase 
            {
                public string Name { get;  set; }

                public int Age { get;  set; }
            }

            private class TestCommand : CommandBase
            {
                public string Name { get;  set; }
            }

            private class EmptyEvent : EventBase {}

            private class EmptyCommand : CommandBase {}
            
        #endregion
    }
}
