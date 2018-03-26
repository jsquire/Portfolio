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
    ///   The suite of tests for the <see cref="PartnerAuthorizationPolicy" />
    ///   class.
    /// </summary>
    ///     
    public class PartnerAuthorizationPolicyTests
    {
        [Fact]
        public void ConstructorValidatesConfigurationIsPresent()
        {
            Action actionUnderTest = () => new PartnerAuthorizationPolicy(null);
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the configuration must be present");
        }

        /// <summary>
        ///   Verifies functionality of the Policy property.
        /// </summary>
        /// 
        [Fact]
        public void PolicyReflectsTheExpectedPolicy()
        {
            var policy = new PartnerAuthorizationPolicy(new PartnerAuthorizationPolicyConfiguration());
            policy.Policy.Should().Be(AuthorizationPolicy.EnforcePartner, "because the policy should match the class name");
        }

        /// <summary>
        ///   Verifies functionality of the Enabled property.
        /// </summary>
        /// 
        [Fact]
        public void EnabledPropertyIsConfigured()
        {
            var config = new PartnerAuthorizationPolicyConfiguration { Enabled = true };
            var policy = new PartnerAuthorizationPolicy(config);
            policy.Enabled.Should().Be(config.Enabled, "because the Enabled property should be driven by configuration");
        }

        /// <summary>
        ///   Verifies functionality of the Evaluate method.
        /// </summary>
        /// 
        [Fact]
        public void EvaluateValidatesContextIsPresent()
        {
            Action actionUnderTest = () => new PartnerAuthorizationPolicy(new PartnerAuthorizationPolicyConfiguration()).Evaluate(null);
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
            var config               = new PartnerAuthorizationPolicyConfiguration { Enabled = false };
            var policy               = new PartnerAuthorizationPolicy(config);

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
        public void EvaluateSucceedsWhenThereIsNoPrincipal()
        {
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var config               = new PartnerAuthorizationPolicyConfiguration { Enabled = true };
            var policy               = new PartnerAuthorizationPolicy(config);

            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            routeData.Values.Add(ActionArguments.Partner, "SQUIRE");

            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);

            actionContext.RequestContext.Principal = null;

            policy.Evaluate(actionContext).Should().BeNull("because the policy should be satisfied if there is no principal");
        }

        /// <summary>
        ///   Verifies functionality of the Evalute method.
        /// </summary>
        /// 
        [Fact]
        public void EvaluateSucceedsWhenThereIsNoPartnerClaim()
        {
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var config               = new PartnerAuthorizationPolicyConfiguration { Enabled = true };
            var policy               = new PartnerAuthorizationPolicy(config);
            var identity             = new ClaimsIdentity(new Claim[] { new Claim(CustomClaimTypes.IdentityType, "UnitTest") });

            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            routeData.Values.Add(ActionArguments.Partner, "SQUIRE");

            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);

            actionContext.RequestContext.Principal = new ClaimsPrincipal(identity);

            policy.Evaluate(actionContext).Should().BeNull("because the policy should always be satisfied if not enabled");
        }

        /// <summary>
        ///   Verifies functionality of the Evalute method.
        /// </summary>
        /// 
        [Fact]
        public void EvaluateSucceedsWhenThereIsNoPartnerActionArgument()
        {
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var config               = new PartnerAuthorizationPolicyConfiguration { Enabled = true };
            var policy               = new PartnerAuthorizationPolicy(config);
            var identity             = new ClaimsIdentity(new Claim[] { new Claim(CustomClaimTypes.Partner, "SQUIRE") });

            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);

            actionContext.RequestContext.Principal = new ClaimsPrincipal(identity);

            policy.Evaluate(actionContext).Should().BeNull("because the policy should always be satisfied if there is no partner argument");
        }

        /// <summary>
        ///   Verifies functionality of the Evalute method.
        /// </summary>
        /// 
        [Fact]
        public void EvaluateSucceedsWhenThereIsAnEmptyPartnerActionArgument()
        {
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var config               = new PartnerAuthorizationPolicyConfiguration { Enabled = true };
            var policy               = new PartnerAuthorizationPolicy(config);
            var identity             = new ClaimsIdentity(new Claim[] { new Claim(CustomClaimTypes.Partner, "SQUIRE") });

            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            routeData.Values.Add(ActionArguments.Partner, "");

            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);

            actionContext.RequestContext.Principal = new ClaimsPrincipal(identity);

            policy.Evaluate(actionContext).Should().BeNull("because the policy should always be satisfied if there is an empty partner argument");
        }

        /// <summary>
        ///   Verifies functionality of the Evalute method.
        /// </summary>
        /// 
        [Fact]
        public void EvaluateSucceedsWhenThereIsANullPartnerActionArgument()
        {
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var config               = new PartnerAuthorizationPolicyConfiguration { Enabled = true };
            var policy               = new PartnerAuthorizationPolicy(config);
            var identity             = new ClaimsIdentity(new Claim[] { new Claim(CustomClaimTypes.Partner, "SQUIRE") });

            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            routeData.Values.Add(ActionArguments.Partner, null);

            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);

            actionContext.RequestContext.Principal = new ClaimsPrincipal(identity);

            policy.Evaluate(actionContext).Should().BeNull("because the policy should always be satisfied if there is a null partner argument");
        }

        /// <summary>
        ///   Verifies functionality of the Evalute method.
        /// </summary>
        /// 
        [Fact]
        public void EvaluateSucceedsWhenThePartnerDataMatches()
        {
            var partner              = "SQUIRE";
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var config               = new PartnerAuthorizationPolicyConfiguration { Enabled = true };
            var policy               = new PartnerAuthorizationPolicy(config);
            var identity             = new ClaimsIdentity(new Claim[] { new Claim(CustomClaimTypes.Partner, partner) });

            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            routeData.Values.Add(ActionArguments.Partner, partner);

            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);

            actionContext.RequestContext.Principal = new ClaimsPrincipal(identity);

            policy.Evaluate(actionContext).Should().BeNull("because the policy should always be satisfied when the partner data is a match");
        }

        /// <summary>
        ///   Verifies functionality of the Evalute method.
        /// </summary>
        /// 
        [Fact]
        public void EvaluateSucceedsWhenThePartnerDataMatchesWithCaseDifferences()
        {
            var partner              = "SQUIRE";
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var config               = new PartnerAuthorizationPolicyConfiguration { Enabled = true };
            var policy               = new PartnerAuthorizationPolicy(config);
            var identity             = new ClaimsIdentity(new Claim[] { new Claim(CustomClaimTypes.Partner, partner.ToUpper()) });

            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            routeData.Values.Add(ActionArguments.Partner, partner.ToLower());

            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);

            actionContext.RequestContext.Principal = new ClaimsPrincipal(identity);

            policy.Evaluate(actionContext).Should().BeNull("because the policy should always be satisfied when the partner data is a match, regardless of case");
        }

        /// <summary>
        ///   Verifies functionality of the Evalute method.
        /// </summary>
        /// 
        [Fact]
        public void EvaluateFailsWhenThePartnerDataIsMismatched()
        {
            var partner              = "SQUIRE";
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var config               = new PartnerAuthorizationPolicyConfiguration { Enabled = true };
            var policy               = new PartnerAuthorizationPolicy(config);
            var identity             = new ClaimsIdentity(new Claim[] { new Claim(CustomClaimTypes.Partner, partner + "NOTTHERIGHTONE") });

            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            routeData.Values.Add(ActionArguments.Partner, partner);

            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);

            actionContext.RequestContext.Principal = new ClaimsPrincipal(identity);

            policy.Evaluate(actionContext).Should().Be(HttpStatusCode.Forbidden, "because the policy should fail for a request where the principal claim differs from the requested partner");
        }
    }
}
