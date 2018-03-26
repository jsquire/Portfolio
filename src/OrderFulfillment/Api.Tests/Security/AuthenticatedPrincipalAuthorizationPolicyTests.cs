using System;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Principal;
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
    ///   The suite of tests for the <see cref="OrderFulfillment.Api.Security.AuthenticatedPrincipalAuthorizationPolicy" />
    ///   class.
    /// </summary>
    ///     
    public class AuthenticatedPrincipalAuthorizationPolicyTests
    {
        /// <summary>
        ///   Verifies functionality of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesConfigurationIsPresent()
        {
            Action actionUnderTest = () => new AuthenticatedPrincipalAuthorizationPolicy(null);
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the configuration must be present");
        }

        /// <summary>
        ///   Verifies functionality of the Policy property.
        /// </summary>
        /// 
        [Fact]
        public void PolicyReflectsTheExpectedPolicy()
        {
            var policy = new AuthenticatedPrincipalAuthorizationPolicy(new AuthenticatedPrincipalAuthorizationPolicyConfiguration());
            policy.Policy.Should().Be(AuthorizationPolicy.AuthenticatedPrincipal, "because the policy should match the class name");
        }

        /// <summary>
        ///   Verifies functionality of the Enabled property.
        /// </summary>
        /// 
        [Fact]
        public void EnabledPropertyIsConfigured()
        {
            var config = new AuthenticatedPrincipalAuthorizationPolicyConfiguration { Enabled = true };
            var policy = new AuthenticatedPrincipalAuthorizationPolicy(config);
            policy.Enabled.Should().Be(config.Enabled, "because the Enabled property should be driven by configuration");
        }

        /// <summary>
        ///   Verifies functionality of the Evaluate method.
        /// </summary>
        /// 
        [Fact]
        public void EvaluateValidatesContextIsPresent()
        {
            Action actionUnderTest = () => new AuthenticatedPrincipalAuthorizationPolicy(new AuthenticatedPrincipalAuthorizationPolicyConfiguration()).Evaluate(null);
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
            var config               = new AuthenticatedPrincipalAuthorizationPolicyConfiguration { Enabled = false };
            var policy               = new AuthenticatedPrincipalAuthorizationPolicy(config);

            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);

            policy.Evaluate(actionContext).Should().BeNull("because the policy should always be satisfied if not enabled");
        }

        /// <summary>
        ///   Verifies functionality of the EvaluteMethod.
        /// </summary>
        /// 
        [Fact]
        public void EvaluateSucceedsWhenTheEndpointAllowsAnonymous()
        {
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic", ControllerType = this.GetType() };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var config               = new AuthenticatedPrincipalAuthorizationPolicyConfiguration { Enabled = true };
            var policy               = new AuthenticatedPrincipalAuthorizationPolicy(config);

            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");
            mockActionDescriptor.Setup(descriptor => descriptor.GetCustomAttributes<AllowAnonymousAttribute>()).Returns(new Collection<AllowAnonymousAttribute>(new[] { new AllowAnonymousAttribute() }));

            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);

            policy.Evaluate(actionContext).Should().BeNull("because the policy should always be satisfied when the endpoint allows anonymous access");
        }

        /// <summary>
        ///   Verifies functionality of the EvaluteMethod.
        /// </summary>
        /// 
        [Fact]
        public void EvaluateSucceedsWhenTheControllerAllowsAnonymous()
        {
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic", ControllerType = typeof(AllowAnonymousController) };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var config               = new AuthenticatedPrincipalAuthorizationPolicyConfiguration { Enabled = true };
            var policy               = new AuthenticatedPrincipalAuthorizationPolicy(config);

            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");
            mockActionDescriptor.Setup(descriptor => descriptor.GetCustomAttributes<AllowAnonymousAttribute>()).Returns(new Collection<AllowAnonymousAttribute>());

            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);

            policy.Evaluate(actionContext).Should().BeNull("because the policy should always be satisfied when the controller allows anonymous access");
        }

        /// <summary>
        ///   Verifies functionality of the EvaluteMethod.
        /// </summary>
        /// 
        [Fact]
        public void EvaluateSucceedsForAuthenticatedPrincipals()
        {
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic", ControllerType = this.GetType() };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var config               = new AuthenticatedPrincipalAuthorizationPolicyConfiguration { Enabled = true };
            var policy               = new AuthenticatedPrincipalAuthorizationPolicy(config);

            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");
            mockActionDescriptor.Setup(descriptor => descriptor.GetCustomAttributes<AllowAnonymousAttribute>()).Returns(new Collection<AllowAnonymousAttribute>());
                        
            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);

            actionContext.RequestContext.Principal = new ClaimsPrincipal(new ClaimsIdentity("dummy auth"));

            policy.Evaluate(actionContext).Should().BeNull("because the policy should always be satisfied when an authenticated principal is present");
        }

        /// <summary>
        ///   Verifies functionality of the EvaluteMethod.
        /// </summary>
        /// 
        [Fact]
        public void EvaluateFailsWithoutAPrincipal()
        {
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic", ControllerType = this.GetType() };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var config               = new AuthenticatedPrincipalAuthorizationPolicyConfiguration { Enabled = true };
            var policy               = new AuthenticatedPrincipalAuthorizationPolicy(config);

            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");
            mockActionDescriptor.Setup(descriptor => descriptor.GetCustomAttributes<AllowAnonymousAttribute>()).Returns(new Collection<AllowAnonymousAttribute>());

            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);

            policy.Evaluate(actionContext).Should().Be(HttpStatusCode.Unauthorized, "because the policy should fail for a request that has no authentiected principal");
        }

        /// <summary>
        ///   Verifies functionality of the EvaluteMethod.
        /// </summary>
        /// 
        [Fact]
        public void EvaluateFailsWhenThePrincipalHasNoIdentity()
        {
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic", ControllerType = this.GetType() };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var config               = new AuthenticatedPrincipalAuthorizationPolicyConfiguration { Enabled = true };
            var policy               = new AuthenticatedPrincipalAuthorizationPolicy(config);

            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");
            mockActionDescriptor.Setup(descriptor => descriptor.GetCustomAttributes<AllowAnonymousAttribute>()).Returns(new Collection<AllowAnonymousAttribute>());

            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);

            actionContext.RequestContext.Principal = new ClaimsPrincipal();

            policy.Evaluate(actionContext).Should().Be(HttpStatusCode.Unauthorized, "because the policy should fail for a request that has no authentiected principal");
        }

        /// <summary>
        ///   Verifies functionality of the EvaluteMethod.
        /// </summary>
        /// 
        [Fact]
        public void EvaluateFailsWhenThePrincipalIsNotAuthenticated()
        {
            var mockIdentity         = new Mock<IIdentity>();
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic", ControllerType = this.GetType() };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);            
            var config               = new AuthenticatedPrincipalAuthorizationPolicyConfiguration { Enabled = true };
            var policy               = new AuthenticatedPrincipalAuthorizationPolicy(config);

            mockIdentity.SetupGet(identity => identity.IsAuthenticated).Returns(false);

            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");
            mockActionDescriptor.Setup(descriptor => descriptor.GetCustomAttributes<AllowAnonymousAttribute>()).Returns(new Collection<AllowAnonymousAttribute>());

            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
            
            actionContext.RequestContext.Principal = new ClaimsPrincipal(mockIdentity.Object);

            policy.Evaluate(actionContext).Should().Be(HttpStatusCode.Unauthorized, "because the policy should fail for a request that has no authentiected principal");
        }

        #region Nested Classes

            [AllowAnonymous]
            public class AllowAnonymousController {}

        #endregion
    }
}
