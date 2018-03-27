using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using System.Web.Http.Results;
using System.Web.Http.Routing;
using FluentAssertions;
using OrderFulfillment.Api.Configuration;
using OrderFulfillment.Api.Security;
using Moq;
using Xunit;

namespace OrderFulfillment.Api.Tests.Security
{
    /// <summary>
    ///   The suite of tests for hte <see cref="OrderFulfillment.Api.Security.SharedSecretAuthenticationHandler "/>
    ///   class.
    /// </summary>
    /// 
    public class SharedSecretAuthenticationHandlerTests
    {
        /// <summary>
        ///   Verifies functionality of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesConfigurationIsPresent()
        {
            Action actionUnderTest = () => new SharedSecretAuthenticationHandler(null);
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the configuration must be present");
        }
        

        /// <summary>
        ///   Verifies functionality of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValdiatesPrimaryKeyIsConfiguredWhenEnabled()
        {
            Action actionUnderTest = () => new SharedSecretAuthenticationHandler(new SharedSecretAuthenticationConfiguration { Enabled = true, PrimarySecret = "omg-secret" });
            actionUnderTest.ShouldThrow<ArgumentException>("because the primary secret must be populated in the configuration").And.Message.Should().Contain("PrimaryKey", "because the correct confiuration item should be detected");
        }

        /// <summary>
        ///   Verifies functionality of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValdiatesPrimarySecretIsConfiguredWhenEnabled()
        {
            Action actionUnderTest = () => new SharedSecretAuthenticationHandler(new SharedSecretAuthenticationConfiguration { Enabled = true, PrimaryKey = "omg-key" });
            actionUnderTest.ShouldThrow<ArgumentException>("because the primary secret must be populated in the configuration").And.Message.Should().Contain("PrimarySecret", "because the correct confiuration item should be detected");
        }

        /// <summary>
        ///   Verifies functionality of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorDoesNotValdiateKeyAndSecretIsConfiguredWhenDisabled()
        {
            Action actionUnderTest = () => new SharedSecretAuthenticationHandler(new SharedSecretAuthenticationConfiguration { Enabled = false });
            actionUnderTest.ShouldNotThrow("because the primary key and secret are not validated when the handler is disabled");
        }

        /// <summary>
        ///   Verifies functionality of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorAllowsMissingSecondaryKey()
        {
            Action actionUnderTest = () => new SharedSecretAuthenticationHandler(new SharedSecretAuthenticationConfiguration { PrimaryKey = "omg-key", PrimarySecret = "zomg!", SecondarySecret = "another one!" });
            actionUnderTest.ShouldNotThrow("because the secondary key is optional in the configuration");
        }

        /// <summary>
        ///   Verifies functionality of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorAllowsMissingSecondarySecret()
        {
            Action actionUnderTest = () => new SharedSecretAuthenticationHandler(new SharedSecretAuthenticationConfiguration { PrimaryKey = "omg-key", PrimarySecret = "zomg!", SecondaryKey = "omg-key2" });
            actionUnderTest.ShouldNotThrow("because the secondary secret is optional in the configuration");
        }

        /// <summary>
        ///   Verifies behavior of the Authenticate method.
        /// </summary>
        /// 
        [Fact]
        public void AuthenticateDoesNotSucceedWithMissingKeyHeader()
        {
            var config = new SharedSecretAuthenticationConfiguration
            {
                PrimaryKey    = "its-a-key!",
                PrimarySecret = "zomg!"
            };

            var handler              = new SharedSecretAuthenticationHandler(config);
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var authcontext          = new HttpAuthenticationContext(actionContext, null);

            request.Headers.Add(Core.Infrastructure.HttpHeaders.ApplicationSecret, "something");

            var result  = handler.Authenticate(null, authcontext);
            result.Should().BeNull("because the header token collection was null");
        }

        /// <summary>
        ///   Verifies behavior of the Authenticate method.
        /// </summary>
        /// 
        [Fact]
        public void AuthenticateDoesNotSucceedWithMissingSecretHeader()
        {
            var config = new SharedSecretAuthenticationConfiguration
            {
                PrimaryKey    = "its-a-key!",
                PrimarySecret = "zomg!"
            };

            var handler              = new SharedSecretAuthenticationHandler(config);
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var authcontext          = new HttpAuthenticationContext(actionContext, null);

            request.Headers.Add(Core.Infrastructure.HttpHeaders.ApplicationKey, "something");

            var result  = handler.Authenticate(null, authcontext);
            result.Should().BeNull("because the header token collection was null");
        }

        /// <summary>
        ///   Verifies behavior of the Authenticate method.
        /// </summary>
        /// 
        [Fact]
        public void AuthenticateSuceedsWithPrimaryKeyAndSecret()
        {
            var config = new SharedSecretAuthenticationConfiguration
            {
                PrimaryKey      = "its-a-key!",
                PrimarySecret   = "zomg!",
                SecondaryKey    = String.Empty,
                SecondarySecret = String.Empty
            };

            var headerTokens = new Dictionary<string, string>
            {
                { "Authentication", "SharedSecret" }

            };

            var handler              = new SharedSecretAuthenticationHandler(config);
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var authcontext          = new HttpAuthenticationContext(actionContext, null);

            request.Headers.Add(Core.Infrastructure.HttpHeaders.ApplicationKey, config.PrimaryKey);
            request.Headers.Add(Core.Infrastructure.HttpHeaders.ApplicationSecret, config.PrimarySecret);
            
            var result = handler.Authenticate(headerTokens, authcontext);
            result.Should().NotBeNull("because the primary secret was used in the header");
            result.Should().BeOfType<ClaimsPrincipal>("because authentication should return a claims principal");
        }

        /// <summary>
        ///   Verifies behavior of the Authenticate method.
        /// </summary>
        /// 
        [Fact]
        public void AuthenticateSuceedsWithSecondaryKeyAndSecret()
        {
            var config = new SharedSecretAuthenticationConfiguration
            {
                PrimaryKey      = "its-a-key!",
                PrimarySecret   = "zomg!",
                SecondaryKey    = "another-key",
                SecondarySecret = "another-secret"
            };

            var headerTokens = new Dictionary<string, string>
            {
                { "Authentication", "SharedSecret" }

            };

            var handler              = new SharedSecretAuthenticationHandler(config);
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var authcontext          = new HttpAuthenticationContext(actionContext, null);

            request.Headers.Add(Core.Infrastructure.HttpHeaders.ApplicationKey, config.SecondaryKey);
            request.Headers.Add(Core.Infrastructure.HttpHeaders.ApplicationSecret, config.SecondarySecret);
            
            var result = handler.Authenticate(headerTokens, authcontext);
            result.Should().NotBeNull("because the primary secret was used in the header");
            result.Should().BeOfType<ClaimsPrincipal>("because authentication should return a claims principal");
        }

        /// <summary>
        ///   Verifies behavior of the Authenticate method.
        /// </summary>
        /// 
        [Fact]
        public void AuthenticateDoesNotSucceedWithInvalidKey()
        {
            var config = new SharedSecretAuthenticationConfiguration
            {
                PrimaryKey      = "its-a-key!",
                PrimarySecret   = "zomg!"
            };

            var headerTokens = new Dictionary<string, string>
            {
                { "Authentication", "SharedSecret" }

            };

            var handler              = new SharedSecretAuthenticationHandler(config);
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var authcontext          = new HttpAuthenticationContext(actionContext, null);

            request.Headers.Add(Core.Infrastructure.HttpHeaders.ApplicationKey, "wrong!");
            request.Headers.Add(Core.Infrastructure.HttpHeaders.ApplicationSecret, config.PrimarySecret);
            
            var result = handler.Authenticate(headerTokens, authcontext);
            result.Should().BeNull("because an invalid key was specified in the header");
        }

        /// <summary>
        ///   Verifies behavior of the Authenticate method.
        /// </summary>
        /// 
        [Fact]
        public void AuthenticateDoesNotSucceedWithInvalidSecret()
        {
            var config = new SharedSecretAuthenticationConfiguration
            {
                PrimaryKey      = "its-a-key!",
                PrimarySecret   = "zomg!"
            };

            var headerTokens = new Dictionary<string, string>
            {
                { "Authentication", "SharedSecret" }

            };

            var handler              = new SharedSecretAuthenticationHandler(config);
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var authcontext          = new HttpAuthenticationContext(actionContext, null);

            request.Headers.Add(Core.Infrastructure.HttpHeaders.ApplicationKey, config.PrimaryKey);
            request.Headers.Add(Core.Infrastructure.HttpHeaders.ApplicationSecret, "wrong!");
            
            var result = handler.Authenticate(headerTokens, authcontext);
            result.Should().BeNull("because an invalid key was specified in the header");
        }

        /// <summary>
        ///   Verifies behavior of the Authenticate method.
        /// </summary>
        /// 
        [Fact]
        public void AuthenticateDoesNotSucceedWithMismatchedKey()
        {
            var config = new SharedSecretAuthenticationConfiguration
            {
                PrimaryKey      = "its-a-key!",
                PrimarySecret   = "zomg!",
                SecondaryKey    = "not primary",
                SecondarySecret = "other secret"
            };

            var headerTokens = new Dictionary<string, string>
            {
                { "Authentication", "SharedSecret" }

            };

            var handler              = new SharedSecretAuthenticationHandler(config);
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var authcontext          = new HttpAuthenticationContext(actionContext, null);

            request.Headers.Add(Core.Infrastructure.HttpHeaders.ApplicationKey, config.SecondaryKey);
            request.Headers.Add(Core.Infrastructure.HttpHeaders.ApplicationSecret, config.PrimarySecret);
            
            var result = handler.Authenticate(headerTokens, authcontext);
            result.Should().BeNull("because an invalid key/secret combo was specified in the header");
        }

        /// <summary>
        ///   Verifies behavior of the Authenticate method.
        /// </summary>
        /// 
        [Fact]
        public void AuthenticateDoesNotSucceedWithMismatchedSecret()
        {
            var config = new SharedSecretAuthenticationConfiguration
            {
                PrimaryKey      = "its-a-key!",
                PrimarySecret   = "zomg!",
                SecondaryKey    = "not primary",
                SecondarySecret = "other secret"
            };

            var headerTokens = new Dictionary<string, string>
            {
                { "Authentication", "SharedSecret" }

            };

            var handler              = new SharedSecretAuthenticationHandler(config);
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var authcontext          = new HttpAuthenticationContext(actionContext, null);

            request.Headers.Add(Core.Infrastructure.HttpHeaders.ApplicationKey, config.PrimaryKey);
            request.Headers.Add(Core.Infrastructure.HttpHeaders.ApplicationSecret, config.SecondarySecret);
            
            var result = handler.Authenticate(headerTokens, authcontext);
            result.Should().BeNull("because an invalid key/secret combo was specified in the header");
        }

        /// <summary>
        ///   Verifies behavior of hte GenerateChallenge method.
        /// </summary>
        /// 
        [Fact]
        public void GenerateChallengeProducesTheChallenge()
        {
            var config = new SharedSecretAuthenticationConfiguration
            {
                PrimaryKey      = "its-a-key!",
                PrimarySecret   = "zomg!",
                SecondaryKey    = "another-key",
                SecondarySecret = "another-secret"
            };

            var headerTokens = new Dictionary<string, string>
            {
                { "Authentication", "SharedSecret" }

            };

            var handler              = new SharedSecretAuthenticationHandler(config);
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var authcontext          = new HttpAuthenticationChallengeContext(actionContext, new UnauthorizedResult(new [] { new AuthenticationHeaderValue("TEST", "") }, request));

            request.Headers.Add(Core.Infrastructure.HttpHeaders.ApplicationKey, "bad-key");
            request.Headers.Add(Core.Infrastructure.HttpHeaders.ApplicationSecret, "bad secret");
            
            var result = handler.GenerateChallenge(headerTokens, authcontext);
            result.Should().NotBeNull("because a challenge should always be generated");
            result.Scheme.Should().Be(handler.HandlerType.ToString(), "because the scheme should match the authentication type");
        }
    }
}
