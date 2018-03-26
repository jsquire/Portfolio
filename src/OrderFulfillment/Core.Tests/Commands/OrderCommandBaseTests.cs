using System;
using FluentAssertions;
using OrderFulfillment.Core.Commands;
using OrderFulfillment.Core.Events;
using OrderFulfillment.Core.Extensions;
using OrderFulfillment.Core.Infrastructure;
using Xunit;

namespace OrderFulfillment.Core.Tests.Commands
{
    /// <summary>
    ///   The suite of tests for the <see cref="OrderCommandBase" />
    ///   class.
    /// </summary>
    public class OrderCommandBaseTests
    {
        /// <summary>
        ///   Verifies functionality of the <see cref="MessageBaseExtensions.CreateNewOrderEvent" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void CreateNewOrderEventWithNoMutator()
        {
            var source = new EmptyCommand
            {
               Id              = Guid.NewGuid(),
               CorrelationId   = "ABC123",
               CurrentUser     = "Some Guy",
               OccurredTimeUtc = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(1)),
            };

            var created = source.CreateNewOrderEvent<EmptyEvent>();

            created.Should().NotBeNull("because the created instance should exist");
            
            // Verify new values are populated.

            created.Id.Should().NotBe(source.Id, "because the id should be a new value");
            created.Id.Should().NotBe(default(Guid), "because the id should be populated");
            created.OccurredTimeUtc.Should().NotBe(source.OccurredTimeUtc, "because the occurred time should be a new value");
            created.OccurredTimeUtc.Should().NotBe(default(DateTime), "because the occurred time should be populated");

            // Verify copied values are copied.

            created.CorrelationId.Should().Be(source.CorrelationId, "because the correlation identifier should be copied");
            created.CurrentUser.Should().Be(source.CurrentUser, "because the current user should be copied");
            created.PartnerCode.Should().Be(source.PartnerCode, "because the current partner code should be copied");
            created.OrderId.Should().Be(source.OrderId, "because the current order identifier should be copied");
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="MessageBaseExtensions.CreateNewOrderCommand" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void CreateNewOrderCommandWithNoMutator()
        {
            var source = new EmptyCommand
            {
               Id              = Guid.NewGuid(),
               CorrelationId   = "ABC123",
               CurrentUser     = "Some Guy",
               OccurredTimeUtc = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(1)),
               PartnerCode     = "UnitTest",
               OrderId         = "Order123"
            };

            var created = source.CreateNewOrderCommand<EmptyCommand>();

            created.Should().NotBeNull("because the created instance should exist");
            
            // Verify new values are populated.

            created.Id.Should().NotBe(source.Id, "because the id should be a new value");
            created.Id.Should().NotBe(default(Guid), "because the id should be populated");
            created.OccurredTimeUtc.Should().NotBe(source.OccurredTimeUtc, "because the occurred time should be a new value");
            created.OccurredTimeUtc.Should().NotBe(default(DateTime), "because the occurred time should be populated");

            // Verify copied values are copied.

            created.CorrelationId.Should().Be(source.CorrelationId, "because the correlation identifier should be copied");
            created.CurrentUser.Should().Be(source.CurrentUser, "because the current user should be copied");
            created.PartnerCode.Should().Be(source.PartnerCode, "because the current partner code should be copied");
            created.OrderId.Should().Be(source.OrderId, "because the current order identifier should be copied");
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="MessageBaseExtensions.CreateNewOrderEvent" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void CreateNewOrderEventWithMutator()
        {
            var newEventName = "Event Name";
            var newEventAge  = 22;

            var source = new TestCommand
            {
               Id              = Guid.NewGuid(),
               CorrelationId   = "ABC123",
               CurrentUser     = "Some Guy",
               OccurredTimeUtc = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(1)),
               PartnerCode     = "UnitTest",
               OrderId         = "Order123",
               Name            = "Command Name"               
            };

            var created = source.CreateNewOrderEvent<TestEvent>(e =>
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
            created.PartnerCode.Should().Be(source.PartnerCode, "because the current partner code should be copied");
            created.OrderId.Should().Be(source.OrderId, "because the current order identifier should be copied");

            // Verify mutator values were set.

            created.Name.Should().Be(newEventName, "because the mutator values should be set");
            created.Age.Should().Be(newEventAge, "because the mutator values should be set");
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="MessageBaseExtensions.CreateNewOrderEvent" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void CreateNewOrderCommandWithMutator()
        {
            var newCommandName = "Event Name";

            var source = new TestCommand
            {
               Id              = Guid.NewGuid(),
               CorrelationId   = "ABC123",
               CurrentUser     = "Some Guy",
               OccurredTimeUtc = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(1)),
               PartnerCode     = "UnitTest",
               OrderId         = "Order123",
               Name            = "Command Name"               
            };

            var created = source.CreateNewOrderCommand<TestCommand>(e => e.Name = newCommandName);

            created.Should().NotBeNull("because the created instance should exist");
            
            // Verify new values are populated.

            created.Id.Should().NotBe(source.Id, "because the id should be a new value");
            created.Id.Should().NotBe(default(Guid), "because the id should be populated");
            created.OccurredTimeUtc.Should().NotBe(source.OccurredTimeUtc, "because the occurred time should be a new value");
            created.OccurredTimeUtc.Should().NotBe(default(DateTime), "because the occurred time should be populated");

            // Verify copied values are copied.

            created.CorrelationId.Should().Be(source.CorrelationId, "because the correlation identifier should be copied");
            created.CurrentUser.Should().Be(source.CurrentUser, "because the current user should be copied");
            created.PartnerCode.Should().Be(source.PartnerCode, "because the current partner code should be copied");
            created.OrderId.Should().Be(source.OrderId, "because the current order identifier should be copied");

            // Verify mutator values were set.

            created.Name.Should().Be(newCommandName, "because the mutator values should be set");
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="MessageBaseExtensions.CreateNewOrderEvent" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void CreateNewOrderEventCanMutateCopiedValues()
        {
            var expectedOrderId = "overwritten";

            var source = new TestCommand
            {
               Id              = Guid.NewGuid(),
               CorrelationId   = "ABC123",
               CurrentUser     = "Some Guy",
               OccurredTimeUtc = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(1)),
               PartnerCode     = "UnitTest",
               OrderId         = "Order123",
               Name            = "Command Name"               
            };

            var created = source.CreateNewOrderEvent<TestEvent>(e => e.OrderId = expectedOrderId);

            created.Should().NotBeNull("because the created instance should exist");
            
            
            created.PartnerCode.Should().Be(source.PartnerCode, "because the current partner code should be copied");
            created.OrderId.Should().Be(expectedOrderId, "because the current order identifier should be mutated");
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="MessageBaseExtensions.CreateNewOrderCommand" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void CreateNewOrderCommandCanMutateCopiedValues()
        {
            var expectedOrderId = "overwritten";

            var source = new TestCommand
            {
               Id              = Guid.NewGuid(),
               CorrelationId   = "ABC123",
               CurrentUser     = "Some Guy",
               OccurredTimeUtc = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(1)),
               PartnerCode     = "UnitTest",
               OrderId         = "Order123",
               Name            = "Command Name"               
            };

            var created = source.CreateNewOrderCommand<TestCommand>(e => e.OrderId = expectedOrderId);

            created.Should().NotBeNull("because the created instance should exist");
            
            created.PartnerCode.Should().Be(source.PartnerCode, "because the current partner code should be copied");
            created.OrderId.Should().Be(expectedOrderId, "because the current order identifier should be mutated");
        }

        #region Nested Classes
        private class TestEvent : OrderEventBase
        {
            public string Name
            {
                get; set;
            }
            public int Age
            {
                get; set;
            }
        }
        private class TestCommand : OrderCommandBase
        {
            public string Name
            {
                get; set;
            }
        }
        private class EmptyEvent : OrderEventBase
        {
        }
        private class EmptyCommand : OrderCommandBase
        {
        }

        #endregion
    }
}
