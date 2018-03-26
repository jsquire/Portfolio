using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dependencies;
using System.Web.Http.Filters;
using System.Web.Http.Hosting;
using System.Web.Http.Routing;
using FluentAssertions;
using OrderFulfillment.Api.Configuration;
using OrderFulfillment.Api.Security;
using Moq;
using Moq.Protected;
using NodaTime;
using Xunit;

namespace OrderFulfillment.Api.Tests.Security
{
    /// <summary>
    ///   The suite of tests for the <see cref="OrderFulfillment.Api.Common.Security.ClientCertificateAuthenticationHandler" /> 
    ///   class.
    /// </summary>
    /// 
    public class ClientCertificateAuthenticationHandlerTests
    {
        /// <summary>A base64 encoded certificate, expiring in 01/2031, that can be used for testing.</summary>
        internal const string Base64Certificate = "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";

        /// <summary>The password to the test certificate.</summary>
        internal const string CertificatePassword = "sslpass";

        /// <summary>
        ///   Verifies functionality of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesConfigurationIsPresent()
        {
            Action actionUnderTest = () => new ClientCertificateAuthenticationHandler(null, Mock.Of<IClock>());
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the configuration must be present")
               .And.ParamName.Should().Be("configuration", "because the configuration was missing");
        }

        /// <summary>
        ///   Verifies functionality of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesClockIsPresent()
        {
            var config = new ClientCertificateAuthenticationConfiguration 
            { 
                Enabled                            = true, 
                EnforceLocalCertificateValidation  = false, 
                SerializedCertificateClaimsMapping = "{\"4497ebb9f0f694d219fe8652a8d4922fead6a5d9\":{\"urn:ordering:security:privilege:sudo\":\"true\"}}" 
            };

            Action actionUnderTest = () => new ClientCertificateAuthenticationHandler(config, null);
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the clock must be present")
                .And.ParamName.Should().Be("clock", "because the clock was missing");
        }

        /// <summary>
        ///   Verifies functionality of the constructor.
        /// </summary>
        /// 
        /// <param name="serializedClaimsMap">The serialized claims mapping to use as configuration.</param>
        /// 
        [Theory]
        [InlineData((string)null)]
        [InlineData("")]
        public void ConstructorAllowsAnUnspecifiedClaimsMappingIsPresentInConfiguration(string serializedClaimsMap)
        {
            var config = new ClientCertificateAuthenticationConfiguration 
            { 
                Enabled                            = true, 
                EnforceLocalCertificateValidation  = false, 
                SerializedCertificateClaimsMapping = serializedClaimsMap 
            };

            var handler   = new ClientCertificateAuthenticationHandler(config, Mock.Of<IClock>());
            var claimsMap = handler.GetType().GetField("claimsMap", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(handler) as Lazy<ClientCertificateClaimsMap>;

            claimsMap.Should().NotBeNull("because the handler should have created a claims map");
            claimsMap.Value.Should().NotBeNull("because a default claims map should be created when no serialized value is present");
            claimsMap.Value.GetCertificateThumbprints().Any().Should().BeFalse("because the defaul claims map should be empty");                
        }

        /// <summary>
        ///   Verifies behavior of the Authenticate method.
        /// </summary>
        /// 
        [Fact]
        public void AuthenticateDoesNotSucceedWithMissingCallerCertificate()
        {
            var config = new ClientCertificateAuthenticationConfiguration 
            { 
                Enabled                            = true, 
                EnforceLocalCertificateValidation  = false, 
                SerializedCertificateClaimsMapping = "{\"4497ebb9f0f694d219fe8652a8d4922fead6a5d9\":{\"urn:ordering:security:privilege:sudo\":\"true\"}}" 
            };
                        
            var mockClock            = new Mock<IClock>();                        
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var handler              = new ClientCertificateAuthenticationHandler(config, mockClock.Object);
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var requestContext       = new HttpRequestContext();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var authcontext          = new HttpAuthenticationContext(actionContext, null);

            requestContext.ClientCertificate = null;
            controllerContext.RequestContext = requestContext;
            controllerContext.Request        = request;
                        
            request.Properties.Add(HttpPropertyKeys.RequestContextKey, requestContext);
            request.Properties.Add(HttpPropertyKeys.ClientCertificateKey, requestContext.ClientCertificate);

            var result  = handler.Authenticate(new Dictionary<string, string>(), authcontext);
            result.Should().BeNull("because there was no client certificate assocaited with the request");
        }

        /// <summary>
        ///   Verifies behavior of the Authenticate method.
        /// </summary>
        /// 
        [Fact]
        public void AuthenticateDoesNotSucceedWithUnexpectedCallerCertificate()
        {            
            var config = new ClientCertificateAuthenticationConfiguration 
            { 
                Enabled                            = true, 
                EnforceLocalCertificateValidation  = false, 
                SerializedCertificateClaimsMapping = "{\"NOT-REAL-THUMPRINT\":{\"urn:ordering:security:privilege:sudo\":\"true\"}}" 
            };
            
            var mockClock            = new Mock<IClock>();                        
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var mockDependencyScope  = new Mock<IDependencyScope>();
            var callerCertificate    = new X509Certificate2(Convert.FromBase64String(ClientCertificateAuthenticationHandlerTests.Base64Certificate), ClientCertificateAuthenticationHandlerTests.CertificatePassword);
            var handler              = new ClientCertificateAuthenticationHandler(config, mockClock.Object);
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var requestContext       = new HttpRequestContext();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var authcontext          = new HttpAuthenticationContext(actionContext, null);

            requestContext.ClientCertificate = callerCertificate;
            controllerContext.RequestContext = requestContext;
            controllerContext.Request        = request;
                        
            request.Properties.Add(HttpPropertyKeys.DependencyScope, mockDependencyScope.Object);
            request.Properties.Add(HttpPropertyKeys.RequestContextKey, requestContext);
            request.Properties.Add(HttpPropertyKeys.ClientCertificateKey, requestContext.ClientCertificate);

            var result  = handler.Authenticate(new Dictionary<string, string>(), authcontext);
            result.Should().BeNull("because the caller certificate does not exist in the claims mapping for known certificates");
        }

        /// <summary>
        ///   Verifies behavior of the Authenticate method.
        /// </summary>
        /// 
        [Fact]
        public void AuthenticateDoesNotSucceedWhenCertificateDateTooEarly()
        {            
            var callerCertificate = new X509Certificate2(Convert.FromBase64String(ClientCertificateAuthenticationHandlerTests.Base64Certificate), ClientCertificateAuthenticationHandlerTests.CertificatePassword);
            var storeCertificate  = new X509Certificate2(Convert.FromBase64String(ClientCertificateAuthenticationHandlerTests.Base64Certificate), ClientCertificateAuthenticationHandlerTests.CertificatePassword);      
            var mapping           = new ClientCertificateClaimsMap();

            mapping.AddCertificate(storeCertificate.Thumbprint, new Dictionary<string, string> 
            {
                { CustomClaimTypes.MayAccessPriviledgedOperations, "true" },
                { CustomClaimTypes.Partner, "SQUIRE" }
            });

            var config = new ClientCertificateAuthenticationConfiguration 
            { 
                Enabled                            = true, 
                EnforceLocalCertificateValidation  = false, 
                SerializedCertificateClaimsMapping = mapping.Serialize()
            };
            
            var mockClock            = new Mock<IClock>();                        
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var mockDependencyScope  = new Mock<IDependencyScope>();
            var mockHandler          = new Mock<ClientCertificateAuthenticationHandler>(config, mockClock.Object) { CallBase = true };
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var requestContext       = new HttpRequestContext();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var authcontext          = new HttpAuthenticationContext(actionContext, null);

            requestContext.ClientCertificate = callerCertificate;
            controllerContext.RequestContext = requestContext;
            controllerContext.Request        = request;
                        
            request.Properties.Add(HttpPropertyKeys.DependencyScope, mockDependencyScope.Object);
            request.Properties.Add(HttpPropertyKeys.RequestContextKey, requestContext);
            request.Properties.Add(HttpPropertyKeys.ClientCertificateKey, requestContext.ClientCertificate);
            
            mockClock.Setup(clock => clock.GetCurrentInstant()).Returns(Instant.FromDateTimeUtc(storeCertificate.NotBefore.AddDays(-1).ToUniversalTime()));
            
            mockHandler.Protected()
                       .Setup<X509Certificate2>("SearchForCertificate", ItExpr.Is<string>(thumb => thumb == storeCertificate.Thumbprint), ItExpr.IsAny<bool>())
                       .Returns(storeCertificate)
                       .Verifiable();

            var result  = mockHandler.Object.Authenticate(new Dictionary<string, string>(), authcontext);
            result.Should().BeNull("because the store certificate is not yet valid");

            mockHandler.VerifyAll();
        }

        /// <summary>
        ///   Verifies behavior of the Authenticate method.
        /// </summary>
        /// 
        [Fact]
        public void AuthenticateDoesNotSucceedWhenCertificateDateTooLate()
        {            
            var callerCertificate = new X509Certificate2(Convert.FromBase64String(ClientCertificateAuthenticationHandlerTests.Base64Certificate), ClientCertificateAuthenticationHandlerTests.CertificatePassword);
            var storeCertificate  = new X509Certificate2(Convert.FromBase64String(ClientCertificateAuthenticationHandlerTests.Base64Certificate), ClientCertificateAuthenticationHandlerTests.CertificatePassword);      
            var mapping           = new ClientCertificateClaimsMap();

            mapping.AddCertificate(storeCertificate.Thumbprint, new Dictionary<string, string> 
            {
                { CustomClaimTypes.MayAccessPriviledgedOperations, "true" },
                { CustomClaimTypes.Partner, "SQUIRE" }
            });

            var config = new ClientCertificateAuthenticationConfiguration 
            { 
                Enabled                            = true, 
                EnforceLocalCertificateValidation  = false, 
                SerializedCertificateClaimsMapping = mapping.Serialize()
            };
            
            var mockClock            = new Mock<IClock>();                        
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var mockDependencyScope  = new Mock<IDependencyScope>();
            var mockHandler          = new Mock<ClientCertificateAuthenticationHandler>(config, mockClock.Object) { CallBase = true };                  
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var requestContext       = new HttpRequestContext();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var authcontext          = new HttpAuthenticationContext(actionContext, null);

            requestContext.ClientCertificate = callerCertificate;
            controllerContext.RequestContext = requestContext;
            controllerContext.Request        = request;
                        
            request.Properties.Add(HttpPropertyKeys.DependencyScope, mockDependencyScope.Object);
            request.Properties.Add(HttpPropertyKeys.RequestContextKey, requestContext);
            request.Properties.Add(HttpPropertyKeys.ClientCertificateKey, requestContext.ClientCertificate);
            
            mockClock.Setup(clock => clock.GetCurrentInstant()).Returns(Instant.FromDateTimeUtc(storeCertificate.NotAfter.AddDays(1).ToUniversalTime()));
            
            mockHandler.Protected()
                       .Setup<X509Certificate2>("SearchForCertificate", ItExpr.Is<string>(thumb => String.Equals(thumb, storeCertificate.Thumbprint, StringComparison.OrdinalIgnoreCase)), ItExpr.IsAny<bool>())
                       .Returns(storeCertificate)
                       .Verifiable();

            var result  = mockHandler.Object.Authenticate(new Dictionary<string, string>(), authcontext);
            result.Should().BeNull("because the store certificate is not yet valid");

            mockHandler.VerifyAll();
        }

        /// <summary>
        ///   Verifies behavior of the Authenticate method.
        /// </summary>
        /// 
        [Fact]
        public void AuthenticateSucceedsForValidCertificates()
        {            
            var callerCertificate = new X509Certificate2(Convert.FromBase64String(ClientCertificateAuthenticationHandlerTests.Base64Certificate), ClientCertificateAuthenticationHandlerTests.CertificatePassword);
            var storeCertificate  = new X509Certificate2(Convert.FromBase64String(ClientCertificateAuthenticationHandlerTests.Base64Certificate), ClientCertificateAuthenticationHandlerTests.CertificatePassword);      
            var mapping           = new ClientCertificateClaimsMap();

            mapping.AddCertificate(storeCertificate.Thumbprint, new Dictionary<string, string> 
            {
                { CustomClaimTypes.MayAccessPriviledgedOperations, "true" },
                { CustomClaimTypes.Partner, "SQUIRE" }
            });

            var config = new ClientCertificateAuthenticationConfiguration 
            { 
                Enabled                            = true, 
                EnforceLocalCertificateValidation  = false, 
                SerializedCertificateClaimsMapping = mapping.Serialize()
            };
            
            var mockClock            = new Mock<IClock>();                        
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var mockDependencyScope  = new Mock<IDependencyScope>();
            var mockHandler          = new Mock<ClientCertificateAuthenticationHandler>(config, mockClock.Object) { CallBase = true };                  
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var requestContext       = new HttpRequestContext();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var authcontext          = new HttpAuthenticationContext(actionContext, null);

            requestContext.ClientCertificate = callerCertificate;
            controllerContext.RequestContext = requestContext;
            controllerContext.Request        = request;
                        
            request.Properties.Add(HttpPropertyKeys.DependencyScope, mockDependencyScope.Object);
            request.Properties.Add(HttpPropertyKeys.RequestContextKey, requestContext);
            request.Properties.Add(HttpPropertyKeys.ClientCertificateKey, requestContext.ClientCertificate);
            
            mockClock.Setup(clock => clock.GetCurrentInstant()).Returns(Instant.FromDateTimeUtc(storeCertificate.NotBefore.AddDays(1).ToUniversalTime()));
            
            mockHandler.Protected()
                       .Setup<X509Certificate2>("SearchForCertificate", ItExpr.Is<string>(thumb => String.Equals(thumb, storeCertificate.Thumbprint, StringComparison.OrdinalIgnoreCase)), ItExpr.IsAny<bool>())
                       .Returns(storeCertificate)
                       .Verifiable();

            var result  = mockHandler.Object.Authenticate(new Dictionary<string, string>(), authcontext);
            result.Should().NotBeNull("because the certificate was valid");

            mockHandler.VerifyAll();
        }

        /// <summary>
        ///   Verifies behavior of the Authenticate method.
        /// </summary>
        /// 
        [Fact]
        public void AuthenticatedPrincipalContainsStandardClaims()
        {            
            var callerCertificate = new X509Certificate2(Convert.FromBase64String(ClientCertificateAuthenticationHandlerTests.Base64Certificate), ClientCertificateAuthenticationHandlerTests.CertificatePassword);
            var storeCertificate  = new X509Certificate2(Convert.FromBase64String(ClientCertificateAuthenticationHandlerTests.Base64Certificate), ClientCertificateAuthenticationHandlerTests.CertificatePassword);      
            var mapping           = new ClientCertificateClaimsMap();

            mapping.AddCertificate(storeCertificate.Thumbprint, new Dictionary<string, string> 
            {
                { CustomClaimTypes.MayAccessPriviledgedOperations, "true" },
                { CustomClaimTypes.Partner, "SQUIRE" }
            });

            var config = new ClientCertificateAuthenticationConfiguration 
            { 
                Enabled                            = true, 
                EnforceLocalCertificateValidation  = false, 
                SerializedCertificateClaimsMapping = mapping.Serialize()
            };
            
            var mockClock            = new Mock<IClock>();                        
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var mockDependencyScope  = new Mock<IDependencyScope>();
            var mockHandler          = new Mock<ClientCertificateAuthenticationHandler>(config, mockClock.Object) { CallBase = true };                  
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var requestContext       = new HttpRequestContext();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var authcontext          = new HttpAuthenticationContext(actionContext, null);

            requestContext.ClientCertificate = callerCertificate;
            controllerContext.RequestContext = requestContext;
            controllerContext.Request        = request;
                        
            request.Properties.Add(HttpPropertyKeys.DependencyScope, mockDependencyScope.Object);
            request.Properties.Add(HttpPropertyKeys.RequestContextKey, requestContext);
            request.Properties.Add(HttpPropertyKeys.ClientCertificateKey, requestContext.ClientCertificate);
            
            mockClock.Setup(clock => clock.GetCurrentInstant()).Returns(Instant.FromDateTimeUtc(storeCertificate.NotBefore.AddDays(1).ToUniversalTime()));
            
            mockHandler.Protected()
                       .Setup<X509Certificate2>("SearchForCertificate", ItExpr.Is<string>(thumb => String.Equals(thumb, storeCertificate.Thumbprint, StringComparison.OrdinalIgnoreCase)), ItExpr.IsAny<bool>())
                       .Returns(storeCertificate);

            var principal = mockHandler.Object.Authenticate(new Dictionary<string, string>(), authcontext) as ClaimsPrincipal;
            principal.Should().NotBeNull("because the certificate was valid and a claims principal should have been returned");

            var identity = principal.Identity as ClaimsIdentity;
            identity.Should().NotBeNull("becaue the principal should contain a valid indentity");
            identity.IsAuthenticated.Should().BeTrue("because the principal was authenticated");
            identity.AuthenticationType.Should().Be(AuthenticationType.ClientCertificate.ToString(), "because a client certificate was used for authentication");

            var standardClaim = identity.FindFirst(claim => claim.Type == ClaimTypes.Thumbprint);
            standardClaim.Should().NotBeNull("because the Thumbprint claim should exist");
            standardClaim.Value.Should().Be(callerCertificate.Thumbprint, "because the caller thumbprint should be the identity");
            
            standardClaim = identity.FindFirst(claim => claim.Type == CustomClaimTypes.IdentityType);
            standardClaim.Should().NotBeNull("because the Identity claim should exist");
            standardClaim.Value.Should().Be(IdentityType.Service.ToString(), "because the caller should be considered a service identity when using a certificate");
        }

        /// <summary>
        ///   Verifies behavior of the Authenticate method.
        /// </summary>
        /// 
        [Fact]
        public void AuthenticatedPrincipalContainsMappedClaims()
        {            
            var callerCertificate = new X509Certificate2(Convert.FromBase64String(ClientCertificateAuthenticationHandlerTests.Base64Certificate), ClientCertificateAuthenticationHandlerTests.CertificatePassword);
            var storeCertificate  = new X509Certificate2(Convert.FromBase64String(ClientCertificateAuthenticationHandlerTests.Base64Certificate), ClientCertificateAuthenticationHandlerTests.CertificatePassword);      
            var mapping           = new ClientCertificateClaimsMap();

            mapping.AddCertificate(storeCertificate.Thumbprint, new Dictionary<string, string> 
            {
                { CustomClaimTypes.MayAccessPriviledgedOperations, "true" },
                { CustomClaimTypes.Partner, "SQUIRE" }
            });

            var config = new ClientCertificateAuthenticationConfiguration 
            { 
                Enabled                            = true, 
                EnforceLocalCertificateValidation  = false, 
                SerializedCertificateClaimsMapping = mapping.Serialize()
            };
            
            var mockClock            = new Mock<IClock>();                        
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var mockDependencyScope  = new Mock<IDependencyScope>();
            var mockHandler          = new Mock<ClientCertificateAuthenticationHandler>(config, mockClock.Object) { CallBase = true };                  
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var requestContext       = new HttpRequestContext();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var authcontext          = new HttpAuthenticationContext(actionContext, null);

            requestContext.ClientCertificate = callerCertificate;
            controllerContext.RequestContext = requestContext;
            controllerContext.Request        = request;
                        
            request.Properties.Add(HttpPropertyKeys.DependencyScope, mockDependencyScope.Object);
            request.Properties.Add(HttpPropertyKeys.RequestContextKey, requestContext);
            request.Properties.Add(HttpPropertyKeys.ClientCertificateKey, requestContext.ClientCertificate);
            
            mockClock.Setup(clock => clock.GetCurrentInstant()).Returns(Instant.FromDateTimeUtc(storeCertificate.NotBefore.AddDays(1).ToUniversalTime()));
            
            mockHandler.Protected()
                       .Setup<X509Certificate2>("SearchForCertificate", ItExpr.Is<string>(thumb => String.Equals(thumb, storeCertificate.Thumbprint, StringComparison.OrdinalIgnoreCase)), ItExpr.IsAny<bool>())
                       .Returns(storeCertificate);

            var principal = mockHandler.Object.Authenticate(new Dictionary<string, string>(), authcontext) as ClaimsPrincipal;
            principal.Should().NotBeNull("because the certificate was valid and a claims principal should have been returned");

            var identity = principal.Identity as ClaimsIdentity;
            identity.Should().NotBeNull("becaue the principal should contain a valid indentity");


            foreach (var mappedClaim in mapping[callerCertificate.Thumbprint])
            {
                var identityClaim = identity.FindFirst(claim => claim.Type == mappedClaim.Key);
                identityClaim.Should().NotBeNull($"because the { mappedClaim.Key } claim should exist");
                identityClaim.Value.Should().Be(mappedClaim.Value, $"because the claim value for { mappedClaim.Key } should match the mapping");
            }
        }
    }
}
