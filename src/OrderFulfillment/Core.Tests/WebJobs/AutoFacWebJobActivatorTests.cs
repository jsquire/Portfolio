using System;
using Autofac;
using FluentAssertions;
using OrderFulfillment.Core.WebJobs;
using Xunit;

namespace OrderFulfillment.Core.Tests.WbJobs
{
    /// <summary>
    ///   The suite of tess for the <see cref="WebJobAutoFactActivator" />
    ///   class.
    /// </summary>
    /// 
    public class WebJobAutofacActivatorTests
    {
        /// <summary>
        ///   Verifies functionality of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesTheContainer()
        {
            Action actionUnderTest = () => new AutoFacWebJobActivator(null);
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the container should be validated");
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="AutoFacWebJobActivator.CreateInstance{T}" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void CreateInstanceDelegatesToTheContainer()
        {
            var expected = new object();
            var builder  = new ContainerBuilder();

            builder
                .RegisterInstance<object>(expected)
                .AsSelf();

            var activator = new AutoFacWebJobActivator(builder.Build());
            var actual    = activator.CreateInstance<object>();

            actual.Should().Be(expected, "because the activator should delegate to the AutoFac container");
        }
    }
}
