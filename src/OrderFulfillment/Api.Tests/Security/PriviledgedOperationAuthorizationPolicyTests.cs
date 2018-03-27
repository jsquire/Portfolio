using System;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Routing;
using FluentAssertions;
using OrderFulfillment.Api.Configuration;
using OrderFulfillment.Api.Infrastructure;
using OrderFulfillment.Api.Security;
using Moq;
using Xunit;

namespace OrderFulfillment.Api.Tests.Security
{
    /// <summary>
    ///   The suite of tests for the <see cref="PriviledgedOperationAuthorizationPolicy" />
    ///   class.
    /// </summary>
    ///     
    public class PriviledgedOperationAuthorizationPolicyTests
    {
        [Fact]
        public void ConstructorValidatesConfigurationIsPresent()
        {
            Action actionUnderTest = () => new PriviledgedOperationAuthorizationPolicy(null);
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the configuration must be present");
        }

        /// <summary>
        ///   Verifies functionality of the Policy property.
        /// </summary>
        /// 
        [Fact]
        public void PolicyReflectsTheExpectedPolicy()
        {
            var policy = new PriviledgedOperationAuthorizationPolicy(new PriviledgedOperationAuthorizationPolicyConfiguration());
            policy.Policy.Should().Be(AuthorizationPolicy.RequireSudo, "because the policy should match the class name");
        }

        /// <summary>
        ///   Verifies functionality of the Enabled property.
        /// </summary>
        /// 
        [Fact]
        public void EnabledPropertyIsConfigured()
        {
            var config = new PriviledgedOperationAuthorizationPolicyConfiguration { Enabled = true };
            var policy = new PriviledgedOperationAuthorizationPolicy(config);
            policy.Enabled.Should().Be(config.Enabled, "because the Enabled property should be driven by configuration");
        }

        /// <summary>
        ///   Verifies functionality of the Evaluate method.
        /// </summary>
        /// 
        [Fact]
        public void EvaluateValidatesContextIsPresent()
        {
            Action actionUnderTest = () => new PriviledgedOperationAuthorizationPolicy(new PriviledgedOperationAuthorizationPolicyConfiguration()).Evaluate(null);
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the HTTP context must be present");
        }

        /// <summary>
        ///   Verifies functionality of the Evalute method.
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
            var config               = new PriviledgedOperationAuthorizationPolicyConfiguration { Enabled = false };
            var policy               = new PriviledgedOperationAuthorizationPolicy(config);

            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);

            policy.Evaluate(actionContext).Should().BeNull("because the policy should always be satisfied if not enabled");
        }

        /// <summary>
        ///   Verifies functionality of the Evalute method.
        /// </summary>
        /// 
        [Fact]
        public void EvaluateFailsWhenThereIsNoPrincipal()
        {
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var config               = new PriviledgedOperationAuthorizationPolicyConfiguration { Enabled = true };
            var policy               = new PriviledgedOperationAuthorizationPolicy(config);

            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);

            actionContext.RequestContext.Principal = null;

            policy.Evaluate(actionContext).Should().Be(HttpStatusCode.Forbidden, "because the policy should fail when no principal is present");
        }

        /// <summary>
        ///   Verifies functionality of the Evalute method.
        /// </summary>
        /// 
        [Fact]
        public void EvaluateFailsWhenThereIsNoSudoClaim()
        {
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var config               = new PriviledgedOperationAuthorizationPolicyConfiguration { Enabled = true };
            var policy               = new PriviledgedOperationAuthorizationPolicy(config);
            var identity             = new ClaimsIdentity(new Claim[] { new Claim(CustomClaimTypes.IdentityType, "UnitTest") });

            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");
            routeData.Values.Add(ActionArguments.Partner, "SQUIRE");

            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);

            actionContext.RequestContext.Principal = new ClaimsPrincipal(identity);

            policy.Evaluate(actionContext).Should().Be(HttpStatusCode.Forbidden, "because the policy should fail when the principal does not hold the required claim");
        }        

        /// <summary>
        ///   Verifies functionality of the Evalute method.
        /// </summary>
        /// 
        [Fact]
        public void EvaluateSucceedsWhenTheClaimIsHeld()
        {
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var config               = new PriviledgedOperationAuthorizationPolicyConfiguration { Enabled = true };
            var policy               = new PriviledgedOperationAuthorizationPolicy(config);
            var identity             = new ClaimsIdentity(new Claim[] { new Claim(CustomClaimTypes.MayAccessPriviledgedOperations, "true") });

            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);

            actionContext.RequestContext.Principal = new ClaimsPrincipal(identity);

            policy.Evaluate(actionContext).Should().BeNull("because the policy should be satisfied when the principal holds the required claim");
        }

        /// <summary>
        ///   Verifies functionality of the Evalute method.
        /// </summary>
        /// 
        [Fact]
        public void EvaluateFailsWhenTheClaimIsHeldWithADifferentCase()
        {
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var config               = new PriviledgedOperationAuthorizationPolicyConfiguration { Enabled = true };
            var policy               = new PriviledgedOperationAuthorizationPolicy(config);
            var identity             = new ClaimsIdentity(new Claim[] { new Claim(CustomClaimTypes.MayAccessPriviledgedOperations.ToUpper(), "true") });

            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);

            actionContext.RequestContext.Principal = new ClaimsPrincipal(identity);

            policy.Evaluate(actionContext).Should().Be(HttpStatusCode.Forbidden, "because the policy should fail when the principal holds the required claim but there is a case mismatch");
        }
    }
}
