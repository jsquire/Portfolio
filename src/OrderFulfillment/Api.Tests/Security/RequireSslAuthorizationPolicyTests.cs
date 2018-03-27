using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Routing;
using FluentAssertions;
using OrderFulfillment.Api.Configuration;
using OrderFulfillment.Api.Security;
using Moq;
using Xunit;

namespace OrderFulfillment.Api.Tests.Security
{
    /// <summary>
    ///   The suite of tests for the <see cref="RequireSslAuthorizationPolicy" />
    ///   class.
    /// </summary>
    /// 
    public class RequireSslAuthorizationPolicyTests
    {
        /// <summary>
        ///   Verifies functionality of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesConfigurationIsPresent()
        {
            Action actionUnderTest = () => new RequireSslAuthorizationPolicy(null);
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the configuration must be present");
        }

        /// <summary>
        ///   Verifies functionality of the Policy property.
        /// </summary>
        /// 
        [Fact]
        public void PolicyReflectsTheExpectedPolicy()
        {
            var policy = new RequireSslAuthorizationPolicy(new RequireSslAuthorizationPolicyConfiguration());
            policy.Policy.Should().Be(AuthorizationPolicy.RequireSsl, "because the policy should match the class name");
        }

        /// <summary>
        ///   Verifies functionality of the Enabled property.
        /// </summary>
        /// 
        [Fact]
        public void EnabledPropertyIsConfigured()
        {
            var config = new RequireSslAuthorizationPolicyConfiguration { Enabled = true };
            var policy = new RequireSslAuthorizationPolicy(config);
            policy.Enabled.Should().Be(config.Enabled, "because the Enabled property should be driven by configuration");
        }

        /// <summary>
        ///   Verifies functionality of the Evaluate method.
        /// </summary>
        /// 
        [Fact]
        public void EvaluateValidatesContextIsPresent()
        {
            Action actionUnderTest = () => new RequireSslAuthorizationPolicy(new RequireSslAuthorizationPolicyConfiguration()).Evaluate(null);
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the HTTP context must be present");
        }

        /// <summary>
        ///   Verifies functionality of the EvaluteMethod.
        /// </summary>
        /// 
        [Fact]
        public void EvaluateSucceedsWhenNotEnabled()
        {
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var config               = new RequireSslAuthorizationPolicyConfiguration { Enabled = false };
            var policy               = new RequireSslAuthorizationPolicy(config);

            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            request.RequestUri = new Uri("http://www.someServer.com");
            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);

            policy.Evaluate(actionContext).Should().BeNull("because the policy should always be satisfied if not enabled");
        }

        /// <summary>
        ///   Verifies functionality of the EvaluteMethod.
        /// </summary>
        /// 
        [Fact]
        public void EvaluateSucceedsForUnsecuredLocalRequestWhenTheExceptionIsAllowed()
        {
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var config               = new RequireSslAuthorizationPolicyConfiguration { Enabled = true, AllowLoopbackException = true };
            var policy               = new RequireSslAuthorizationPolicy(config);

            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            request.RequestUri = new Uri("http://localhost/some-endpoint");
            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);

            policy.Evaluate(actionContext).Should().BeNull("because the policy should always be satisfied for a local request when the exception is enabled");
        }

        /// <summary>
        ///   Verifies functionality of the EvaluteMethod.
        /// </summary>
        /// 
        [Fact]
        public void EvaluateFailsForUnsecuredLocalRequestWhenTheExceptionIsDisallowed()
        {
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var config               = new RequireSslAuthorizationPolicyConfiguration { Enabled = true, AllowLoopbackException = false };
            var policy               = new RequireSslAuthorizationPolicy(config);

            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            request.RequestUri = new Uri("http://localhost/some-endpoint");
            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);

            policy.Evaluate(actionContext).Should().Be(HttpStatusCode.Forbidden, "because the policy should not be satisfied for an unsecured local request when the exception is disabled");
        }

        /// <summary>
        ///   Verifies functionality of the EvaluteMethod.
        /// </summary>
        /// 
        [Fact]
        public void EvaluateSucceedsForSecureLocalRequestWhenTheExceptionIsAllowed()
        {
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var config               = new RequireSslAuthorizationPolicyConfiguration { Enabled = true, AllowLoopbackException = true };
            var policy               = new RequireSslAuthorizationPolicy(config);

            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            request.RequestUri = new Uri("https://localhost/some-endpoint");
            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);

            policy.Evaluate(actionContext).Should().BeNull("because the policy should always be satisfied for a local request when the exception is enabled");
        }

        /// <summary>
        ///   Verifies functionality of the EvaluteMethod.
        /// </summary>
        /// 
        [Fact]
        public void EvaluateSucceedsForSecureLocalRequestWhenTheExceptionIsDisallowed()
        {
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var config               = new RequireSslAuthorizationPolicyConfiguration { Enabled = true, AllowLoopbackException = false };
            var policy               = new RequireSslAuthorizationPolicy(config);

            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            request.RequestUri = new Uri("https://localhost/some-endpoint");
            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);

            policy.Evaluate(actionContext).Should().BeNull("because the policy should be satisfied for a secure local request when the exception is disallowed");
        }

        /// <summary>
        ///   Verifies functionality of the EvaluteMethod.
        /// </summary>
        /// 
        [Fact]
        public void EvaluateSucceedsForSecureRequest()
        {
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var config               = new RequireSslAuthorizationPolicyConfiguration { Enabled = true };
            var policy               = new RequireSslAuthorizationPolicy(config);

            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            request.RequestUri = new Uri("https://api.someserver.com/some-endpoint");
            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);

            policy.Evaluate(actionContext).Should().BeNull("because the policy should always be satisfied for a request over SSL");
        }

        /// <summary>
        ///   Verifies functionality of the EvaluteMethod.
        /// </summary>
        /// 
        [Fact]
        public void EvaluateFailsForUnsecuredRequest()
        {
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var config               = new RequireSslAuthorizationPolicyConfiguration { Enabled = true, AllowLoopbackException = true };
            var policy               = new RequireSslAuthorizationPolicy(config);

            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            request.RequestUri = new Uri("http://api.someserver.com/some-endpoint");
            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);

            policy.Evaluate(actionContext).Should().Be(HttpStatusCode.Forbidden, "because the policy should fail for a request that is not local or over SSL.");
        }
    }
}
