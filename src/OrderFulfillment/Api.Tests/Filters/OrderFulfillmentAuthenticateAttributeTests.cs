using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dependencies;
using System.Web.Http.Filters;
using System.Web.Http.Hosting;
using System.Web.Http.Results;
using System.Web.Http.Routing;
using FluentAssertions;
using OrderFulfillment.Api.Filters;
using OrderFulfillment.Api.Infrastructure;
using OrderFulfillment.Api.Security;
using OrderFulfillment.Api.Tests.Security;
using Moq;
using Serilog;
using Xunit;

namespace OrderFulfillment.Api.Tests.Filters
{
    /// <summary>
    ///   The suite of tests for the <see cref="OrderFulfillmentAuthenticateAttributeAttribute"/>
    ///   class.
    /// </summary> 
    /// 
    public class OrderFulfillmentAuthenticateAttributeTests
    {     
        /// <summary>
        ///   Verifies the behavior of the AuthenticateAsync method.
        /// </summary>
        /// 
        [Fact]
        public void AuthenticateAsyncValidatesTheContext()
        {
            Action actionUnderTest = () => new OrderFulfillmentAuthenticateAttributeAttribute().AuthenticateAsync(null, CancellationToken.None).GetAwaiter().GetResult();
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the authentication context should be validated");
        }

        /// <summary>
        ///   Verifies the behavior of the AuthenticateAsync method.
        /// </summary>
        /// 
        [Fact]
        public async Task AuthenticateAsyncHonorsTheCancellationToken()
        {   
            var requestBody          = "REQUEST BODY CONTENT";
            var mockLogger           = new Mock<ILogger>();         
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var mockDependencyScope  = new Mock<IDependencyScope>();
            var mockAuthHandler      = new Mock<IAuthenticationHandler>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var authContext          = new HttpAuthenticationContext(actionContext, null);
                        
            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            mockDependencyScope.Setup(scope => scope.GetService(It.Is<Type>(type => type == typeof(ILogger))))
                               .Returns(mockLogger.Object);

            mockDependencyScope.Setup(scope => scope.GetServices(It.Is<Type>(param => param == typeof(IAuthenticationHandler))))
                               .Returns(new [] { mockAuthHandler.Object });
           
            mockAuthHandler.SetupGet(handler => handler.HandlerType).Returns(AuthenticationType.SharedSecret);
            mockAuthHandler.SetupGet(handler => handler.Strength).Returns(AuthenticationStrength.Weak);

            mockAuthHandler.Setup(handler => handler.Authenticate(It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<HttpAuthenticationContext>()))
                           .Returns(new GenericPrincipal(new GenericIdentity("fake"), new string[0]));        
                           
            mockLogger.Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                      .Returns(mockLogger.Object);                   

            request.Content = new StringContent(requestBody);
            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
            request.Headers.TryAddWithoutValidation(Core.Infrastructure.HttpHeaders.Authorization, "NotARealScheme realm=\"localhost\"");
            request.Properties.Add(HttpPropertyKeys.DependencyScope, mockDependencyScope.Object);
                        
            var cancellationToken = new CancellationToken(true);
            var authAttribute     = new OrderFulfillmentAuthenticateAttributeAttribute();

            await authAttribute.AuthenticateAsync(authContext, cancellationToken);
            authContext.Principal.Should().BeNull("because cancellation should have occured before authentication took place");

            mockDependencyScope.Verify(scope => scope.GetServices(It.Is<Type>(param => param == typeof(IAuthenticationHandler))), Times.Never, "The cancellation should have taken place before a handler was sought");
        }

        /// <summary>
        ///   Verifies the behavior of the AuthenticateAsync method.
        /// </summary>
        /// 
        [Fact]
        public async Task AuthenticateAsyncChoosesTheCorrectHandler()
        {   
            var requestBody          = "REQUEST BODY CONTENT";
            var mockLogger           = new Mock<ILogger>();         
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var mockDependencyScope  = new Mock<IDependencyScope>();
            var mockActualHandler    = new Mock<IAuthenticationHandler>();
            var mockOtherHandler     = new Mock<IAuthenticationHandler>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var authContext          = new HttpAuthenticationContext(actionContext, null);
                        
            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            mockDependencyScope.Setup(scope => scope.GetService(It.Is<Type>(type => type == typeof(ILogger))))
                               .Returns(mockLogger.Object);

            mockDependencyScope.Setup(scope => scope.GetService(It.Is<Type>(param => param == typeof(IHttpHeaderParser))))
                               .Returns(new HttpHeaderParser());

            mockDependencyScope.Setup(scope => scope.GetServices(It.Is<Type>(param => param == typeof(IAuthenticationHandler))))
                               .Returns(new [] { mockActualHandler.Object, mockOtherHandler.Object });

            mockLogger.Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                      .Returns(mockLogger.Object);
           
            mockActualHandler.SetupGet(handler => handler.Enabled).Returns(true);
            mockActualHandler.SetupGet(handler => handler.HandlerType).Returns(AuthenticationType.SharedSecret);
            mockActualHandler.SetupGet(handler => handler.Strength).Returns(AuthenticationStrength.Weak);

            mockOtherHandler.SetupGet(handler => handler.Enabled).Returns(true);
            mockOtherHandler.SetupGet(handler => handler.HandlerType).Returns(AuthenticationType.Token);
            mockOtherHandler.SetupGet(handler => handler.Strength).Returns(AuthenticationStrength.Stronger);

            mockActualHandler.Setup(handler => handler.Authenticate(It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<HttpAuthenticationContext>()))
                           .Returns(new GenericPrincipal(new GenericIdentity("fake"), new string[0]));                           

            request.Content = new StringContent(requestBody);
            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
            request.Headers.TryAddWithoutValidation(Core.Infrastructure.HttpHeaders.Authorization, $"{ AuthenticationType.SharedSecret } realm=\"localhost\"");
            request.Headers.TryAddWithoutValidation(Core.Infrastructure.HttpHeaders.ApplicationKey, "some-key");
            request.Headers.TryAddWithoutValidation(Core.Infrastructure.HttpHeaders.ApplicationSecret, "some secret!");
            request.Properties.Add(HttpPropertyKeys.DependencyScope, mockDependencyScope.Object);
                        
            var cancellationToken = new CancellationToken();
            var authAttribute     = new OrderFulfillmentAuthenticateAttributeAttribute();

            await authAttribute.AuthenticateAsync(authContext, cancellationToken);

            mockDependencyScope.Verify(scope => scope.GetServices(It.Is<Type>(param => param == typeof(IAuthenticationHandler))), Times.AtLeastOnce, "The authentication handler should have been sought");
            mockActualHandler.Verify(handler => handler.Authenticate(It.IsAny<IReadOnlyDictionary<string, string>>(), It.Is<HttpAuthenticationContext>(context => context == authContext)), Times.AtLeastOnce, "Authentication should have been attempted on the correct handler");
        }

        /// <summary>
        ///   Verifies the behavior of the AuthenticateAsync method.
        /// </summary>
        /// 
        [Fact]
        public async Task AuthenticateAsyncChoosesTheCorrectHandlerForAClientCertificate()
        {   
            var requestBody            = "REQUEST BODY CONTENT";
            var mockLogger             = new Mock<ILogger>();         
            var mockActionDescriptor   = new Mock<HttpActionDescriptor>();
            var mockDependencyScope    = new Mock<IDependencyScope>();
            var mockAuthHandler        = new Mock<IAuthenticationHandler>();
            var mockOtherHandler       = new Mock<IAuthenticationHandler>();
            var mockCertificateHandler = new Mock<IAuthenticationHandler>();
            var callerCertificate      = new X509Certificate2(Convert.FromBase64String(ClientCertificateAuthenticationHandlerTests.Base64Certificate), ClientCertificateAuthenticationHandlerTests.CertificatePassword);
            var httpConfiguration      = new HttpConfiguration();
            var routeData              = new HttpRouteData(new HttpRoute());
            var requestContext         = new HttpRequestContext();
            var request                = new HttpRequestMessage();
            var controllerDescriptor   = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext      = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext          = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var authContext            = new HttpAuthenticationContext(actionContext, null);
                        
            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            mockDependencyScope.Setup(scope => scope.GetService(It.Is<Type>(type => type == typeof(ILogger))))
                               .Returns(mockLogger.Object);

            mockDependencyScope.Setup(scope => scope.GetService(It.Is<Type>(param => param == typeof(IHttpHeaderParser))))
                               .Returns(new HttpHeaderParser());

            mockDependencyScope.Setup(scope => scope.GetServices(It.Is<Type>(param => param == typeof(IAuthenticationHandler))))
                               .Returns(new [] { mockAuthHandler.Object, mockCertificateHandler.Object, mockOtherHandler.Object });

            mockLogger.Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                      .Returns(mockLogger.Object);
           
            mockAuthHandler.SetupGet(handler => handler.Enabled).Returns(true);
            mockAuthHandler.SetupGet(handler => handler.HandlerType).Returns(AuthenticationType.SharedSecret);
            mockAuthHandler.SetupGet(handler => handler.Strength).Returns(AuthenticationStrength.Weak);
            mockAuthHandler.SetupGet(handler => handler.CanGenerateChallenge).Returns(true);

            mockOtherHandler.SetupGet(handler => handler.Enabled).Returns(true);
            mockOtherHandler.SetupGet(handler => handler.HandlerType).Returns(AuthenticationType.Token);
            mockOtherHandler.SetupGet(handler => handler.Strength).Returns(AuthenticationStrength.Stronger);
            mockOtherHandler.SetupGet(handler => handler.CanGenerateChallenge).Returns(true);
            
            mockCertificateHandler.SetupGet(handler => handler.Enabled).Returns(true);
            mockCertificateHandler.SetupGet(handler => handler.HandlerType).Returns(AuthenticationType.ClientCertificate);
            mockCertificateHandler.SetupGet(handler => handler.Strength).Returns(AuthenticationStrength.Medium);
            mockCertificateHandler.SetupGet(handler => handler.CanGenerateChallenge).Returns(true);
            
            mockCertificateHandler.Setup(handler => handler.Authenticate(It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<HttpAuthenticationContext>()))
                                  .Returns(new GenericPrincipal(new GenericIdentity("fake"), new string[0]));     
                           
            requestContext.ClientCertificate = callerCertificate;
            controllerContext.RequestContext = requestContext;
            controllerContext.Request        = request;
                        
            request.Properties.Add(HttpPropertyKeys.DependencyScope, mockDependencyScope.Object);
            request.Properties.Add(HttpPropertyKeys.RequestContextKey, requestContext);
            request.Properties.Add(HttpPropertyKeys.ClientCertificateKey, requestContext.ClientCertificate);       

            request.Content = new StringContent(requestBody);
            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData); 
                        
            var cancellationToken = new CancellationToken();
            var authAttribute     = new OrderFulfillmentAuthenticateAttributeAttribute();

            await authAttribute.AuthenticateAsync(authContext, cancellationToken);

            mockDependencyScope.Verify(scope => scope.GetServices(It.Is<Type>(param => param == typeof(IAuthenticationHandler))), Times.AtLeastOnce, "The authentication handler should have been sought");
            mockCertificateHandler.Verify(handler => handler.Authenticate(It.IsAny<IReadOnlyDictionary<string, string>>(), It.Is<HttpAuthenticationContext>(context => context == authContext)), Times.AtLeastOnce, "Authentication should have been attempted on the correct handler");
            mockAuthHandler.Verify(handler => handler.Authenticate(It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<HttpAuthenticationContext>()), Times.Never, "Authentication should not have have been attempted on an incorrect handler");
            mockOtherHandler.Verify(handler => handler.Authenticate(It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<HttpAuthenticationContext>()), Times.Never, "Authentication should not have have been attempted on an incorrect handler");
        }

        /// <summary>
        ///   Verifies the behavior of the AuthenticateAsync method.
        /// </summary>
        /// 
        [Fact]
        public async Task AuthenticateAsyncChoosesPrioritizesAClientCertificateOverOtherSchemes()
        {   
            var requestBody            = "REQUEST BODY CONTENT";
            var mockLogger             = new Mock<ILogger>();         
            var mockActionDescriptor   = new Mock<HttpActionDescriptor>();
            var mockDependencyScope    = new Mock<IDependencyScope>();
            var mockAuthHandler        = new Mock<IAuthenticationHandler>();
            var mockOtherHandler       = new Mock<IAuthenticationHandler>();
            var mockCertificateHandler = new Mock<IAuthenticationHandler>();
            var callerCertificate      = new X509Certificate2(Convert.FromBase64String(ClientCertificateAuthenticationHandlerTests.Base64Certificate), ClientCertificateAuthenticationHandlerTests.CertificatePassword);
            var httpConfiguration      = new HttpConfiguration();
            var routeData              = new HttpRouteData(new HttpRoute());
            var requestContext         = new HttpRequestContext();
            var request                = new HttpRequestMessage();
            var controllerDescriptor   = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext      = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext          = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var authContext            = new HttpAuthenticationContext(actionContext, null);
                        
            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            mockDependencyScope.Setup(scope => scope.GetService(It.Is<Type>(type => type == typeof(ILogger))))
                               .Returns(mockLogger.Object);

            mockDependencyScope.Setup(scope => scope.GetService(It.Is<Type>(param => param == typeof(IHttpHeaderParser))))
                               .Returns(new HttpHeaderParser());

            mockDependencyScope.Setup(scope => scope.GetServices(It.Is<Type>(param => param == typeof(IAuthenticationHandler))))
                               .Returns(new [] { mockAuthHandler.Object, mockCertificateHandler.Object, mockOtherHandler.Object });

            mockLogger.Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                      .Returns(mockLogger.Object);
           
            mockAuthHandler.SetupGet(handler => handler.Enabled).Returns(true);
            mockAuthHandler.SetupGet(handler => handler.HandlerType).Returns(AuthenticationType.SharedSecret);
            mockAuthHandler.SetupGet(handler => handler.Strength).Returns(AuthenticationStrength.Weak);
            mockAuthHandler.SetupGet(handler => handler.CanGenerateChallenge).Returns(true);

            mockOtherHandler.SetupGet(handler => handler.Enabled).Returns(true);
            mockOtherHandler.SetupGet(handler => handler.HandlerType).Returns(AuthenticationType.Token);
            mockOtherHandler.SetupGet(handler => handler.Strength).Returns(AuthenticationStrength.Stronger);
            mockOtherHandler.SetupGet(handler => handler.CanGenerateChallenge).Returns(true);
            
            mockCertificateHandler.SetupGet(handler => handler.Enabled).Returns(true);
            mockCertificateHandler.SetupGet(handler => handler.HandlerType).Returns(AuthenticationType.ClientCertificate);
            mockCertificateHandler.SetupGet(handler => handler.Strength).Returns(AuthenticationStrength.Medium);
            mockCertificateHandler.SetupGet(handler => handler.CanGenerateChallenge).Returns(true);
            
            mockCertificateHandler.Setup(handler => handler.Authenticate(It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<HttpAuthenticationContext>()))
                                  .Returns(new GenericPrincipal(new GenericIdentity("fake"), new string[0]));     
                           
            requestContext.ClientCertificate = callerCertificate;
            controllerContext.RequestContext = requestContext;
            controllerContext.Request        = request;
                        
            request.Properties.Add(HttpPropertyKeys.DependencyScope, mockDependencyScope.Object);
            request.Properties.Add(HttpPropertyKeys.RequestContextKey, requestContext);
            request.Properties.Add(HttpPropertyKeys.ClientCertificateKey, requestContext.ClientCertificate);        

            request.Content = new StringContent(requestBody);
            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
            request.Headers.TryAddWithoutValidation(Core.Infrastructure.HttpHeaders.Authorization, $"{ AuthenticationType.SharedSecret } realm=\"localhost\"");
            request.Headers.TryAddWithoutValidation(Core.Infrastructure.HttpHeaders.ApplicationKey, "some-key");
            request.Headers.TryAddWithoutValidation(Core.Infrastructure.HttpHeaders.ApplicationSecret, "some secret!");
                        
            var cancellationToken = new CancellationToken();
            var authAttribute     = new OrderFulfillmentAuthenticateAttributeAttribute();

            await authAttribute.AuthenticateAsync(authContext, cancellationToken);

            mockDependencyScope.Verify(scope => scope.GetServices(It.Is<Type>(param => param == typeof(IAuthenticationHandler))), Times.AtLeastOnce, "The authentication handler should have been sought");
            mockCertificateHandler.Verify(handler => handler.Authenticate(It.IsAny<IReadOnlyDictionary<string, string>>(), It.Is<HttpAuthenticationContext>(context => context == authContext)), Times.AtLeastOnce, "Authentication should have been attempted on the correct handler");
            mockAuthHandler.Verify(handler => handler.Authenticate(It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<HttpAuthenticationContext>()), Times.Never, "Authentication should not have have been attempted on an incorrect handler");
            mockOtherHandler.Verify(handler => handler.Authenticate(It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<HttpAuthenticationContext>()), Times.Never, "Authentication should not have have been attempted on an incorrect handler");
        }

        /// <summary>
        ///   Verifies the behavior of the AuthenticateAsync method.
        /// </summary>
        /// 
        [Fact]
        public async Task AuthenticateAsyncUpdatesTheContext()
        {   
            var requestBody          = "REQUEST BODY CONTENT";
            var mockLogger           = new Mock<ILogger>();               
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var mockDependencyScope  = new Mock<IDependencyScope>();
            var mockAuthHandler      = new Mock<IAuthenticationHandler>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var authContext          = new HttpAuthenticationContext(actionContext, null);
            var expectedPrincipal    = new GenericPrincipal(new GenericIdentity("fake"), new string[0]);
                        
            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            mockDependencyScope.Setup(scope => scope.GetService(It.Is<Type>(type => type == typeof(ILogger))))
                               .Returns(mockLogger.Object);

            mockDependencyScope.Setup(scope => scope.GetService(It.Is<Type>(param => param == typeof(IHttpHeaderParser))))
                               .Returns(new HttpHeaderParser());

            mockDependencyScope.Setup(scope => scope.GetServices(It.Is<Type>(param => param == typeof(IAuthenticationHandler))))
                               .Returns(new [] { mockAuthHandler.Object, Mock.Of<IAuthenticationHandler>() });
           
            mockLogger.Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                      .Returns(mockLogger.Object);

            mockAuthHandler.SetupGet(handler => handler.Enabled).Returns(true);
            mockAuthHandler.SetupGet(handler => handler.HandlerType).Returns(AuthenticationType.SharedSecret);
            mockAuthHandler.SetupGet(handler => handler.Strength).Returns(AuthenticationStrength.Weak);

            mockAuthHandler.Setup(handler => handler.Authenticate(It.IsAny<IReadOnlyDictionary<string, string>>(), It.Is<HttpAuthenticationContext>(context => context == authContext)))
                           .Returns(expectedPrincipal);                           

            request.Content = new StringContent(requestBody);
            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
            request.Headers.TryAddWithoutValidation(Core.Infrastructure.HttpHeaders.Authorization, $"{ AuthenticationType.SharedSecret } realm=\"localhost\"");
            request.Headers.TryAddWithoutValidation(Core.Infrastructure.HttpHeaders.ApplicationKey, "some-key");
            request.Headers.TryAddWithoutValidation(Core.Infrastructure.HttpHeaders.ApplicationSecret, "some secret!");
            request.Properties.Add(HttpPropertyKeys.DependencyScope, mockDependencyScope.Object);
                        
            var cancellationToken = new CancellationToken();
            var authAttribute     = new OrderFulfillmentAuthenticateAttributeAttribute();

            await authAttribute.AuthenticateAsync(authContext, cancellationToken);
            authContext.Principal.Should().Be(expectedPrincipal, "because the principal should have been set on the context for a successful authentication");
        }

        /// <summary>
        ///   Verifies the behavior of the AuthenticateAsync method.
        /// </summary>
        /// 
        [Fact]
        public async Task AuthenticateAsyncIgnoresDisabledHandlers()
        {   
            var requestBody          = "REQUEST BODY CONTENT";
            var mockLogger           = new Mock<ILogger>();         
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var mockDependencyScope  = new Mock<IDependencyScope>();
            var mockActualHandler    = new Mock<IAuthenticationHandler>();
            var mockOtherHandler     = new Mock<IAuthenticationHandler>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var authContext          = new HttpAuthenticationContext(actionContext, null);
                        
            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            mockDependencyScope.Setup(scope => scope.GetService(It.Is<Type>(param => param == typeof(IHttpHeaderParser))))
                               .Returns(new HttpHeaderParser());
                                
            mockDependencyScope.Setup(scope => scope.GetService(It.Is<Type>(type => type == typeof(ILogger))))
                               .Returns(mockLogger.Object);

            mockDependencyScope.Setup(scope => scope.GetServices(It.Is<Type>(param => param == typeof(IAuthenticationHandler))))
                               .Returns(new [] { mockActualHandler.Object, Mock.Of<IAuthenticationHandler>() });

            mockLogger.Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                      .Returns(mockLogger.Object);
           
            mockActualHandler.SetupGet(handler => handler.Enabled).Returns(false);
            mockActualHandler.SetupGet(handler => handler.HandlerType).Returns(AuthenticationType.SharedSecret);
            mockActualHandler.SetupGet(handler => handler.Strength).Returns(AuthenticationStrength.Weak);

            mockOtherHandler.SetupGet(handler => handler.Enabled).Returns(true);
            mockOtherHandler.SetupGet(handler => handler.HandlerType).Returns(AuthenticationType.Token);
            mockOtherHandler.SetupGet(handler => handler.Strength).Returns(AuthenticationStrength.Stronger);

            mockActualHandler.Setup(handler => handler.Authenticate(It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<HttpAuthenticationContext>()))
                             .Returns(new GenericPrincipal(new GenericIdentity("fake"), new string[0]));                           

            request.Content = new StringContent(requestBody);
            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
            request.Headers.TryAddWithoutValidation(Core.Infrastructure.HttpHeaders.Authorization, $"{ AuthenticationType.SharedSecret } realm=\"localhost\"");
            request.Headers.TryAddWithoutValidation(Core.Infrastructure.HttpHeaders.ApplicationKey, "some-key");
            request.Headers.TryAddWithoutValidation(Core.Infrastructure.HttpHeaders.ApplicationSecret, "some secret!");
            request.Properties.Add(HttpPropertyKeys.DependencyScope, mockDependencyScope.Object);
                        
            var cancellationToken = new CancellationToken();
            var authAttribute     = new OrderFulfillmentAuthenticateAttributeAttribute();

            await authAttribute.AuthenticateAsync(authContext, cancellationToken);

            mockDependencyScope.Verify(scope => scope.GetServices(It.Is<Type>(param => param == typeof(IAuthenticationHandler))), Times.AtLeastOnce, "The authentication handler should have been sought");
            mockActualHandler.Verify(handler => handler.Authenticate(It.IsAny<IReadOnlyDictionary<string, string>>(), It.Is<HttpAuthenticationContext>(context => context == authContext)), Times.Never, "Authentication should not have been attempted due to the disabled handler");
        }

        /// <summary>
        ///   Verifies the behavior of the AuthenticateAsync method.
        /// </summary>
        /// 
        [Fact]
        public async Task AuthenticateAsyncLogsWhenNoHandlerIsFound()
        {   
            var requestBody          = "REQUEST BODY CONTENT";
            var mockLogger           = new Mock<ILogger>();               
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var mockDependencyScope  = new Mock<IDependencyScope>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var authContext          = new HttpAuthenticationContext(actionContext, null);
            var expectedPrincipal    = new GenericPrincipal(new GenericIdentity("fake"), new string[0]);
                        
            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            mockDependencyScope.Setup(scope => scope.GetService(It.Is<Type>(type => type == typeof(ILogger))))
                               .Returns(mockLogger.Object);

            mockDependencyScope.Setup(scope => scope.GetService(It.Is<Type>(param => param == typeof(IHttpHeaderParser))))
                               .Returns(new HttpHeaderParser());

            mockDependencyScope.Setup(scope => scope.GetServices(It.Is<Type>(param => param == typeof(IAuthenticationHandler))))
                               .Returns(Enumerable.Empty<IAuthenticationHandler>());
           
            mockLogger.Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                      .Returns(mockLogger.Object);

            request.Content = new StringContent(requestBody);
            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
            request.Headers.TryAddWithoutValidation(Core.Infrastructure.HttpHeaders.Authorization, $"{ AuthenticationType.SharedSecret } realm=\"localhost\"");
            request.Headers.TryAddWithoutValidation(Core.Infrastructure.HttpHeaders.ApplicationKey, "some-key");
            request.Headers.TryAddWithoutValidation(Core.Infrastructure.HttpHeaders.ApplicationSecret, "some secret!");
            request.Properties.Add(HttpPropertyKeys.DependencyScope, mockDependencyScope.Object);

            await request.Content.ReadAsStringAsync();
                        
            var cancellationToken = new CancellationToken();
            var authAttribute     = new OrderFulfillmentAuthenticateAttributeAttribute();

            await authAttribute.AuthenticateAsync(authContext, cancellationToken);
            
            mockLogger.Verify(logger => logger.Information(It.IsAny<string>(), 
                                                           It.Is<Uri>(uri => uri == request.RequestUri), 
                                                           It.Is<HttpRequestHeaders>(headers => headers == request.Headers), 
                                                           It.Is<string>(cert => cert == null)), 
                                            Times.Once, "The authentication result should have been logged");
        }        

        /// <summary>
        ///   Verifies the behavior of the AuthenticateAsync method.
        /// </summary>
        /// 
        [Fact]
        public async Task AuthenticateAsyncLogsWhenNotAuthenticated()
        {   
            var requestBody          = "REQUEST BODY CONTENT";
            var mockLogger           = new Mock<ILogger>();               
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var mockDependencyScope  = new Mock<IDependencyScope>();
            var mockAuthHandler      = new Mock<IAuthenticationHandler>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var authContext          = new HttpAuthenticationContext(actionContext, null);
            var expectedPrincipal    = new GenericPrincipal(new GenericIdentity("fake"), new string[0]);
                        
            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            mockDependencyScope.Setup(scope => scope.GetService(It.Is<Type>(type => type == typeof(ILogger))))
                               .Returns(mockLogger.Object);

            mockDependencyScope.Setup(scope => scope.GetService(It.Is<Type>(param => param == typeof(IHttpHeaderParser))))
                               .Returns(new HttpHeaderParser());

            mockDependencyScope.Setup(scope => scope.GetServices(It.Is<Type>(param => param == typeof(IAuthenticationHandler))))
                               .Returns(new [] { mockAuthHandler.Object, Mock.Of<IAuthenticationHandler>() });
           
            mockLogger.Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                      .Returns(mockLogger.Object);

            mockAuthHandler.SetupGet(handler => handler.Enabled).Returns(true);
            mockAuthHandler.SetupGet(handler => handler.HandlerType).Returns(AuthenticationType.SharedSecret);
            mockAuthHandler.SetupGet(handler => handler.Strength).Returns(AuthenticationStrength.Weak);

            mockAuthHandler.Setup(handler => handler.Authenticate(It.IsAny<IReadOnlyDictionary<string, string>>(), It.Is<HttpAuthenticationContext>(context => context == authContext)))
                           .Returns((IPrincipal)null);                           

            request.Content = new StringContent(requestBody);
            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
            request.Headers.TryAddWithoutValidation(Core.Infrastructure.HttpHeaders.Authorization, $"{ AuthenticationType.SharedSecret } realm=\"localhost\"");
            request.Headers.TryAddWithoutValidation(Core.Infrastructure.HttpHeaders.ApplicationKey, "some-key");
            request.Headers.TryAddWithoutValidation(Core.Infrastructure.HttpHeaders.ApplicationSecret, "some secret!");
            request.Properties.Add(HttpPropertyKeys.DependencyScope, mockDependencyScope.Object);

            await request.Content.ReadAsStringAsync();
                        
            var cancellationToken = new CancellationToken();
            var authAttribute     = new OrderFulfillmentAuthenticateAttributeAttribute();

            await authAttribute.AuthenticateAsync(authContext, cancellationToken);
            
            mockLogger.Verify(logger => logger.Information(It.IsAny<string>(), 
                                                           It.Is<Uri>(uri => uri == request.RequestUri), 
                                                           It.Is<HttpRequestHeaders>(headers => headers == request.Headers), 
                                                           It.Is<string>(cert => cert == null)), 
                                            Times.Once, "The authentication result should have been logged");
        }

        /// <summary>
        ///   Verifies the behavior of the AuthenticateAsync method.
        /// </summary>
        /// 
        [Fact]
        public void ChallengeAsyncValidatesTheContext()
        {
            Action actionUnderTest = () => new OrderFulfillmentAuthenticateAttributeAttribute().ChallengeAsync(null, CancellationToken.None).GetAwaiter().GetResult();
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the challenge context should be validated");
        }

        /// <summary>
        ///   Verifies the behavior of the ChallengeAsync method.
        /// </summary>
        /// 
        [Fact]
        public async Task ChallengeAsyncHonorsTheCancellationToken()
        {            
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var mockDependencyScope  = new Mock<IDependencyScope>();
            var mockAuthHandler      = new Mock<IAuthenticationHandler>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var expectedResult       = new UnauthorizedResult(new AuthenticationHeaderValue[0], request);
            var challengeContext     = new HttpAuthenticationChallengeContext(actionContext, expectedResult);
                        
            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            mockDependencyScope.Setup(scope => scope.GetServices(It.Is<Type>(param => param == typeof(IAuthenticationHandler))))
                               .Returns(new [] { mockAuthHandler.Object });
           
            mockAuthHandler.SetupGet(handler => handler.HandlerType).Returns(AuthenticationType.SharedSecret);
            mockAuthHandler.SetupGet(handler => handler.Strength).Returns(AuthenticationStrength.Weak);                       

            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
            request.Headers.TryAddWithoutValidation(Core.Infrastructure.HttpHeaders.Authorization, "NotARealScheme realm=\"localhost\"");
            request.Properties.Add(HttpPropertyKeys.DependencyScope, mockDependencyScope.Object);
                        
            var cancellationToken = new CancellationToken(true);
            var authAttribute     = new OrderFulfillmentAuthenticateAttributeAttribute();

            await authAttribute.ChallengeAsync(challengeContext, cancellationToken);
            challengeContext.Result.Should().Be(expectedResult, "Because cancellation should have taken place before a challenge result was set");

            mockDependencyScope.Verify(scope => scope.GetServices(It.Is<Type>(param => param == typeof(IAuthenticationHandler))), Times.Never, "The cancellation should have taken place before a handler was sought");
        }

        /// <summary>
        ///   Verifies the behavior of the ChallengeAsync method.
        /// </summary>
        /// 
        [Fact]
        public async Task ChallengeAsyncChoosesTheCorrectHandlerForRequestedScheme()
        {            
            var mockActionDescriptor       = new Mock<HttpActionDescriptor>();
            var mockDependencyScope        = new Mock<IDependencyScope>();
            var mockWeakChallengeHandler   = new Mock<IAuthenticationHandler>();
            var mockStrongChallengeHandler = new Mock<IAuthenticationHandler>();
            var httpConfiguration          = new HttpConfiguration();
            var routeData                  = new HttpRouteData(new HttpRoute());
            var request                    = new HttpRequestMessage();
            var controllerDescriptor       = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext          = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext              = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var expectedResult             = new UnauthorizedResult(new AuthenticationHeaderValue[0], request);
            var challengeContext           = new HttpAuthenticationChallengeContext(actionContext, expectedResult);
                        
            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            mockDependencyScope.Setup(scope => scope.GetService(It.Is<Type>(param => param == typeof(IHttpHeaderParser))))
                               .Returns(new HttpHeaderParser());

            mockDependencyScope.Setup(scope => scope.GetServices(It.Is<Type>(param => param == typeof(IAuthenticationHandler))))
                               .Returns(new [] { mockWeakChallengeHandler.Object, mockStrongChallengeHandler.Object });
           
            mockWeakChallengeHandler.SetupGet(handler => handler.Enabled).Returns(true);
            mockWeakChallengeHandler.SetupGet(handler => handler.HandlerType).Returns(AuthenticationType.SharedSecret);
            mockWeakChallengeHandler.SetupGet(handler => handler.Strength).Returns(AuthenticationStrength.Weak);
            mockWeakChallengeHandler.SetupGet(handler => handler.CanGenerateChallenge).Returns(true);

            mockStrongChallengeHandler.SetupGet(handler => handler.Enabled).Returns(true);
            mockStrongChallengeHandler.SetupGet(handler => handler.HandlerType).Returns(AuthenticationType.Token);
            mockStrongChallengeHandler.SetupGet(handler => handler.Strength).Returns(AuthenticationStrength.Stronger);                  
            mockStrongChallengeHandler.SetupGet(handler => handler.CanGenerateChallenge).Returns(true);

            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
            request.Headers.TryAddWithoutValidation(Core.Infrastructure.HttpHeaders.Authorization, $"{ AuthenticationType.SharedSecret } realm=\"localhost\"");
            request.Headers.TryAddWithoutValidation(Core.Infrastructure.HttpHeaders.ApplicationKey, "some-key");
            request.Headers.TryAddWithoutValidation(Core.Infrastructure.HttpHeaders.ApplicationSecret, "some secret!");
            request.Properties.Add(HttpPropertyKeys.DependencyScope, mockDependencyScope.Object);
                        
            var cancellationToken = new CancellationToken();
            var authAttribute     = new OrderFulfillmentAuthenticateAttributeAttribute();

            await authAttribute.ChallengeAsync(challengeContext, cancellationToken);

            mockDependencyScope.Verify(scope => scope.GetServices(It.Is<Type>(param => param == typeof(IAuthenticationHandler))), Times.AtLeastOnce, "The authentication handler should have been sought");
            mockWeakChallengeHandler.Verify(handler => handler.GenerateChallenge(It.IsAny<IReadOnlyDictionary<string, string>>(), It.Is<HttpAuthenticationChallengeContext>(context => context == challengeContext)), Times.AtLeastOnce, "because the challenge should match the value provided by the authentication handler");
        }

        /// <summary>
        ///   Verifies the behavior of the ChallengeAsync method.
        /// </summary>
        /// 
        [Fact]
        public async Task ChallengeAsyncIgnoresDisabledHandlers()
        {            
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var mockDependencyScope  = new Mock<IDependencyScope>();
            var mockActualHandler    = new Mock<IAuthenticationHandler>();
            var mockOtherHandler     = new Mock<IAuthenticationHandler>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var expectedResult       = new UnauthorizedResult(new AuthenticationHeaderValue[0], request);
            var challengeContext     = new HttpAuthenticationChallengeContext(actionContext, expectedResult);
                        
            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            mockDependencyScope.Setup(scope => scope.GetService(It.Is<Type>(param => param == typeof(IHttpHeaderParser))))
                               .Returns(new HttpHeaderParser());

            mockDependencyScope.Setup(scope => scope.GetServices(It.Is<Type>(param => param == typeof(IAuthenticationHandler))))
                               .Returns(new [] { mockActualHandler.Object, Mock.Of<IAuthenticationHandler>() });
           
            mockActualHandler.SetupGet(handler => handler.Enabled).Returns(false);
            mockActualHandler.SetupGet(handler => handler.HandlerType).Returns(AuthenticationType.SharedSecret);
            mockActualHandler.SetupGet(handler => handler.Strength).Returns(AuthenticationStrength.Weak);

            mockOtherHandler.SetupGet(handler => handler.Enabled).Returns(false);
            mockOtherHandler.SetupGet(handler => handler.HandlerType).Returns(AuthenticationType.Token);
            mockOtherHandler.SetupGet(handler => handler.Strength).Returns(AuthenticationStrength.Stronger);                  

            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
            request.Headers.TryAddWithoutValidation(Core.Infrastructure.HttpHeaders.Authorization, $"{ AuthenticationType.SharedSecret } realm=\"localhost\"");
            request.Headers.TryAddWithoutValidation(Core.Infrastructure.HttpHeaders.ApplicationKey, "some-key");
            request.Headers.TryAddWithoutValidation(Core.Infrastructure.HttpHeaders.ApplicationSecret, "some secret!");
            request.Properties.Add(HttpPropertyKeys.DependencyScope, mockDependencyScope.Object);
                        
            var cancellationToken = new CancellationToken();
            var authAttribute     = new OrderFulfillmentAuthenticateAttributeAttribute();

            await authAttribute.ChallengeAsync(challengeContext, cancellationToken);

            mockDependencyScope.Verify(scope => scope.GetServices(It.Is<Type>(param => param == typeof(IAuthenticationHandler))), Times.AtLeastOnce, "The authentication handler should have been sought");
            mockActualHandler.Verify(handler => handler.GenerateChallenge(It.IsAny<IReadOnlyDictionary<string, string>>(), It.Is<HttpAuthenticationChallengeContext>(context => context == challengeContext)), Times.Never, "Chalenge generation should not have been attempted due to the disabled handler");
        }

        /// <summary>
        ///   Verifies the behavior of the ChallengeAsync method.
        /// </summary>
        /// 
        [Fact]
        public async Task ChallengeAsyncSetsTheResult()
        {            
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var mockDependencyScope  = new Mock<IDependencyScope>();
            var mockAuthHandler      = new Mock<IAuthenticationHandler>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var defaultResult        = new UnauthorizedResult(new AuthenticationHeaderValue[0], request);
            var challengeContext     = new HttpAuthenticationChallengeContext(actionContext, defaultResult);
            var expectedChallenge    = new AuthenticationHeaderValue("DUMMY");
                        
            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            mockDependencyScope.Setup(scope => scope.GetService(It.Is<Type>(param => param == typeof(IHttpHeaderParser))))
                               .Returns(new HttpHeaderParser());

            mockDependencyScope.Setup(scope => scope.GetServices(It.Is<Type>(param => param == typeof(IAuthenticationHandler))))
                               .Returns(new [] { mockAuthHandler.Object, Mock.Of<IAuthenticationHandler>() });
           
            mockAuthHandler.SetupGet(handler => handler.Enabled).Returns(true);
            mockAuthHandler.SetupGet(handler => handler.HandlerType).Returns(AuthenticationType.SharedSecret);
            mockAuthHandler.SetupGet(handler => handler.Strength).Returns(AuthenticationStrength.Weak);
            mockAuthHandler.SetupGet(handler => handler.CanGenerateChallenge).Returns(true);
            
            mockAuthHandler.Setup(handler => handler.GenerateChallenge(It.IsAny<IReadOnlyDictionary<string, string>>(), It.Is<HttpAuthenticationChallengeContext>(context => context == challengeContext)))
                           .Returns(expectedChallenge);

            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
            request.Headers.TryAddWithoutValidation(Core.Infrastructure.HttpHeaders.Authorization, $"{ AuthenticationType.SharedSecret } realm=\"localhost\"");
            request.Headers.TryAddWithoutValidation(Core.Infrastructure.HttpHeaders.ApplicationKey, "some-key");
            request.Headers.TryAddWithoutValidation(Core.Infrastructure.HttpHeaders.ApplicationSecret, "some secret!");
            request.Properties.Add(HttpPropertyKeys.DependencyScope, mockDependencyScope.Object);
                        
            var cancellationToken = new CancellationToken();
            var authAttribute     = new OrderFulfillmentAuthenticateAttributeAttribute();

            await authAttribute.ChallengeAsync(challengeContext, cancellationToken);
            challengeContext.ActionContext.Response.Should().BeNull("because the response message should have been cleared in favor of the action result.");
            challengeContext.Result.Should().BeOfType<UnauthorizedResult>("because the challenge should force an Unauthorized result");
            ((UnauthorizedResult)challengeContext.Result).Challenges.Single().Should().Be(expectedChallenge, "becaues the challene should match the value provided by the authentication handler");
        }
        
        /// <summary>
        ///   Verifies the behavior of the ChallengeAsync method.
        /// </summary>
        /// 
        [Fact]
        public async Task ChallengeAsyncRespectsASetPrincipal()
        {            
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var mockDependencyScope  = new Mock<IDependencyScope>();
            var mockAuthHandler      = new Mock<IAuthenticationHandler>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var expectedResult       = new UnauthorizedResult(new AuthenticationHeaderValue[0], request);
            var challengeContext     = new HttpAuthenticationChallengeContext(actionContext, expectedResult);
            var expectedChallenge    = new AuthenticationHeaderValue("DUMMY");
                        
            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            mockDependencyScope.Setup(scope => scope.GetService(It.Is<Type>(param => param == typeof(IHttpHeaderParser))))
                               .Returns(new HttpHeaderParser());

            mockDependencyScope.Setup(scope => scope.GetServices(It.Is<Type>(param => param == typeof(IAuthenticationHandler))))
                               .Returns(new [] { mockAuthHandler.Object, Mock.Of<IAuthenticationHandler>() });
           
            mockAuthHandler.SetupGet(handler => handler.Enabled).Returns(true);
            mockAuthHandler.SetupGet(handler => handler.HandlerType).Returns(AuthenticationType.SharedSecret);
            mockAuthHandler.SetupGet(handler => handler.Strength).Returns(AuthenticationStrength.Weak);  
            
            mockAuthHandler.Setup(handler => handler.GenerateChallenge(It.IsAny<IReadOnlyDictionary<string, string>>(), It.Is<HttpAuthenticationChallengeContext>(context => context == challengeContext)))
                           .Returns(expectedChallenge);

            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
            request.Headers.TryAddWithoutValidation(Core.Infrastructure.HttpHeaders.Authorization, $"{ AuthenticationType.SharedSecret } realm=\"localhost\"");
            request.Headers.TryAddWithoutValidation(Core.Infrastructure.HttpHeaders.ApplicationKey, "some-key");
            request.Headers.TryAddWithoutValidation(Core.Infrastructure.HttpHeaders.ApplicationSecret, "some secret!");
            request.Properties.Add(HttpPropertyKeys.DependencyScope, mockDependencyScope.Object);
            request.SetRequestContext(new HttpRequestContext());
            request.GetRequestContext().Principal = new GenericPrincipal(new GenericIdentity("someguy"), new string[0]);
                        
            var cancellationToken = new CancellationToken();
            var authAttribute     = new OrderFulfillmentAuthenticateAttributeAttribute();

            await authAttribute.ChallengeAsync(challengeContext, cancellationToken);
            challengeContext.Result.Should().Be(expectedResult, "because the challenge should not set a result when a Principal is present");
        }

        /// <summary>
        ///   Verifies the behavior of the ChallengeAsync method.
        /// </summary>
        /// 
        [Fact]
        public async Task ChallengeAsyncRespectsAnExistingResult()
        {            
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var mockDependencyScope  = new Mock<IDependencyScope>();
            var mockAuthHandler      = new Mock<IAuthenticationHandler>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var challengeContext     = new HttpAuthenticationChallengeContext(actionContext, new OkResult(request));
            var expectedChallenge    = new AuthenticationHeaderValue("DUMMY");
            var expectedResult       = challengeContext.Result;
            
                        
            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            mockDependencyScope.Setup(scope => scope.GetService(It.Is<Type>(param => param == typeof(IHttpHeaderParser))))
                               .Returns(new HttpHeaderParser());

            mockDependencyScope.Setup(scope => scope.GetServices(It.Is<Type>(param => param == typeof(IAuthenticationHandler))))
                               .Returns(new [] { mockAuthHandler.Object, Mock.Of<IAuthenticationHandler>() });
           
            mockAuthHandler.SetupGet(handler => handler.Enabled).Returns(true);
            mockAuthHandler.SetupGet(handler => handler.HandlerType).Returns(AuthenticationType.SharedSecret);
            mockAuthHandler.SetupGet(handler => handler.Strength).Returns(AuthenticationStrength.Weak);  
            
            mockAuthHandler.Setup(handler => handler.GenerateChallenge(It.IsAny<IReadOnlyDictionary<string, string>>(), It.Is<HttpAuthenticationChallengeContext>(context => context == challengeContext)))
                           .Returns(expectedChallenge);

            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
            request.Headers.TryAddWithoutValidation(Core.Infrastructure.HttpHeaders.Authorization, $"{ AuthenticationType.SharedSecret } realm=\"localhost\" secret=\"NotReal\"");
            request.Properties.Add(HttpPropertyKeys.DependencyScope, mockDependencyScope.Object);
                        
            var cancellationToken = new CancellationToken();
            var authAttribute     = new OrderFulfillmentAuthenticateAttributeAttribute();

            await authAttribute.ChallengeAsync(challengeContext, cancellationToken);
            challengeContext.Result.Should().Be(expectedResult, "because the challenge should respect an existing result");
        }

        /// <summary>
        ///   Verifies the behavior of the ChallengeAsync method.
        /// </summary>
        /// 
        [Fact]
        public async Task ChallengeAsyncRespectsAnExistingSuccessResponse()
        {            
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var mockDependencyScope  = new Mock<IDependencyScope>();
            var mockAuthHandler      = new Mock<IAuthenticationHandler>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object) { Response = request.CreateResponse(System.Net.HttpStatusCode.OK) };
            var challengeContext     = new HttpAuthenticationChallengeContext(actionContext, new StatusCodeResult(System.Net.HttpStatusCode.ServiceUnavailable, request));
            var expectedChallenge    = new AuthenticationHeaderValue("DUMMY");
            var expectedResult       = challengeContext.Result;
            var expectedResponse     = actionContext.Response;
                        
            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            mockDependencyScope.Setup(scope => scope.GetService(It.Is<Type>(param => param == typeof(IHttpHeaderParser))))
                               .Returns(new HttpHeaderParser());

            mockDependencyScope.Setup(scope => scope.GetServices(It.Is<Type>(param => param == typeof(IAuthenticationHandler))))
                               .Returns(new [] { mockAuthHandler.Object, Mock.Of<IAuthenticationHandler>() });
           
            mockAuthHandler.SetupGet(handler => handler.Enabled).Returns(true);
            mockAuthHandler.SetupGet(handler => handler.HandlerType).Returns(AuthenticationType.SharedSecret);
            mockAuthHandler.SetupGet(handler => handler.Strength).Returns(AuthenticationStrength.Weak);  
            
            mockAuthHandler.Setup(handler => handler.GenerateChallenge(It.IsAny<IReadOnlyDictionary<string, string>>(), It.Is<HttpAuthenticationChallengeContext>(context => context == challengeContext)))
                           .Returns(expectedChallenge);

            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
            request.Headers.TryAddWithoutValidation(Core.Infrastructure.HttpHeaders.Authorization, $"{ AuthenticationType.SharedSecret } realm=\"localhost\" secret=\"NotReal\"");
            request.Properties.Add(HttpPropertyKeys.DependencyScope, mockDependencyScope.Object);
                        
            var cancellationToken = new CancellationToken();
            var authAttribute     = new OrderFulfillmentAuthenticateAttributeAttribute();

            await authAttribute.ChallengeAsync(challengeContext, cancellationToken);
            challengeContext.Result.Should().Be(expectedResult, "because the challenge should respect an existing response");
            actionContext.Response.Should().Be(expectedResponse, "because the challenge should respect an existing response");
        }

        /// <summary>
        ///   Verifies the behavior of the ChallengeAsync method.
        /// </summary>
        /// 
        [Fact]
        public async Task ChallengeAsyncRespectsAnExistingForbiddenResponse()
        {            
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var mockDependencyScope  = new Mock<IDependencyScope>();
            var mockAuthHandler      = new Mock<IAuthenticationHandler>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object) { Response = request.CreateResponse(System.Net.HttpStatusCode.Forbidden) };
            var challengeContext     = new HttpAuthenticationChallengeContext(actionContext, new StatusCodeResult(System.Net.HttpStatusCode.ServiceUnavailable, request));
            var expectedChallenge    = new AuthenticationHeaderValue("DUMMY");
            var expectedResult       = challengeContext.Result;
            var expectedResponse     = actionContext.Response;
                        
            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            mockDependencyScope.Setup(scope => scope.GetService(It.Is<Type>(param => param == typeof(IHttpHeaderParser))))
                               .Returns(new HttpHeaderParser());

            mockDependencyScope.Setup(scope => scope.GetServices(It.Is<Type>(param => param == typeof(IAuthenticationHandler))))
                               .Returns(new [] { mockAuthHandler.Object, Mock.Of<IAuthenticationHandler>() });
           
            mockAuthHandler.SetupGet(handler => handler.Enabled).Returns(true);
            mockAuthHandler.SetupGet(handler => handler.HandlerType).Returns(AuthenticationType.SharedSecret);
            mockAuthHandler.SetupGet(handler => handler.Strength).Returns(AuthenticationStrength.Weak);  
            
            mockAuthHandler.Setup(handler => handler.GenerateChallenge(It.IsAny<IReadOnlyDictionary<string, string>>(), It.Is<HttpAuthenticationChallengeContext>(context => context == challengeContext)))
                           .Returns(expectedChallenge);

            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
            request.Headers.TryAddWithoutValidation(Core.Infrastructure.HttpHeaders.Authorization, $"{ AuthenticationType.SharedSecret } realm=\"localhost\"");
            request.Headers.TryAddWithoutValidation(Core.Infrastructure.HttpHeaders.ApplicationKey, "some-key");
            request.Headers.TryAddWithoutValidation(Core.Infrastructure.HttpHeaders.ApplicationSecret, "some secret!");
            request.Properties.Add(HttpPropertyKeys.DependencyScope, mockDependencyScope.Object);
                        
            var cancellationToken = new CancellationToken();
            var authAttribute     = new OrderFulfillmentAuthenticateAttributeAttribute();

            await authAttribute.ChallengeAsync(challengeContext, cancellationToken);
            challengeContext.Result.Should().Be(expectedResult, "because the challenge should respect an existing response");
            actionContext.Response.Should().Be(expectedResponse, "because the challenge should respect an existing response");
        }

        /// <summary>
        ///   Verifies the behavior of the ChallengeAsync method.
        /// </summary>
        /// 
        [Fact]
        public async Task ChallengeAsyncSetsTheChallengeForAnExistingUnauthorizedResult()
        {            
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var mockDependencyScope  = new Mock<IDependencyScope>();
            var mockAuthHandler      = new Mock<IAuthenticationHandler>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var challengeContext     = new HttpAuthenticationChallengeContext(actionContext, new UnauthorizedResult(new AuthenticationHeaderValue[0], request));
            var expectedChallenge    = new AuthenticationHeaderValue("DUMMY");
                        
            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            mockDependencyScope.Setup(scope => scope.GetService(It.Is<Type>(param => param == typeof(IHttpHeaderParser))))
                               .Returns(new HttpHeaderParser());

            mockDependencyScope.Setup(scope => scope.GetServices(It.Is<Type>(param => param == typeof(IAuthenticationHandler))))
                               .Returns(new [] { mockAuthHandler.Object, Mock.Of<IAuthenticationHandler>() });
           
            mockAuthHandler.SetupGet(handler => handler.Enabled).Returns(true);
            mockAuthHandler.SetupGet(handler => handler.HandlerType).Returns(AuthenticationType.SharedSecret);
            mockAuthHandler.SetupGet(handler => handler.Strength).Returns(AuthenticationStrength.Weak);
            mockAuthHandler.SetupGet(handler => handler.CanGenerateChallenge).Returns(true);
            
            mockAuthHandler.Setup(handler => handler.GenerateChallenge(It.IsAny<IReadOnlyDictionary<string, string>>(), It.Is<HttpAuthenticationChallengeContext>(context => context == challengeContext)))
                           .Returns(expectedChallenge);

            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
            request.Headers.TryAddWithoutValidation(Core.Infrastructure.HttpHeaders.Authorization, $"{ AuthenticationType.SharedSecret } realm=\"localhost\" secret=\"NotReal\"");
            request.Properties.Add(HttpPropertyKeys.DependencyScope, mockDependencyScope.Object);
                        
            var cancellationToken = new CancellationToken();
            var authAttribute     = new OrderFulfillmentAuthenticateAttributeAttribute();

            await authAttribute.ChallengeAsync(challengeContext, cancellationToken);
            challengeContext.ActionContext.Response.Should().BeNull("because the response message should have been cleared in favor of the action result.");
            challengeContext.Result.Should().BeOfType<UnauthorizedResult>("because the challenge should force an Unauthorized result");
            ((UnauthorizedResult)challengeContext.Result).Challenges.Single().Should().Be(expectedChallenge, "becaues the challene should match the value provided by the authentication handler");
        }

        /// <summary>
        ///   Verifies the behavior of the ChallengeAsync method.
        /// </summary>
        /// 
        [Fact]
        public async Task ChallengeAsyncSetsTheChallengeForAnExistingStatusUnauthorizedResult()
        {            
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var mockDependencyScope  = new Mock<IDependencyScope>();
            var mockAuthHandler      = new Mock<IAuthenticationHandler>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var challengeContext     = new HttpAuthenticationChallengeContext(actionContext, new StatusCodeResult(System.Net.HttpStatusCode.Unauthorized, request));
            var expectedChallenge    = new AuthenticationHeaderValue("DUMMY");
                        
            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            mockDependencyScope.Setup(scope => scope.GetService(It.Is<Type>(param => param == typeof(IHttpHeaderParser))))
                               .Returns(new HttpHeaderParser());

            mockDependencyScope.Setup(scope => scope.GetServices(It.Is<Type>(param => param == typeof(IAuthenticationHandler))))
                               .Returns(new [] { mockAuthHandler.Object, Mock.Of<IAuthenticationHandler>() });
           
            mockAuthHandler.SetupGet(handler => handler.Enabled).Returns(true);
            mockAuthHandler.SetupGet(handler => handler.HandlerType).Returns(AuthenticationType.SharedSecret);
            mockAuthHandler.SetupGet(handler => handler.Strength).Returns(AuthenticationStrength.Weak);
            mockAuthHandler.SetupGet(handler => handler.CanGenerateChallenge).Returns(true);  
            
            mockAuthHandler.Setup(handler => handler.GenerateChallenge(It.IsAny<IReadOnlyDictionary<string, string>>(), It.Is<HttpAuthenticationChallengeContext>(context => context == challengeContext)))
                           .Returns(expectedChallenge);

            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
            request.Properties.Add(HttpPropertyKeys.DependencyScope, mockDependencyScope.Object);
                        
            var cancellationToken = new CancellationToken();
            var authAttribute     = new OrderFulfillmentAuthenticateAttributeAttribute();

            await authAttribute.ChallengeAsync(challengeContext, cancellationToken);
            challengeContext.ActionContext.Response.Should().BeNull("because the response message should have been cleared in favor of the action result.");
            challengeContext.Result.Should().BeOfType<UnauthorizedResult>("because the challenge should force an Unauthorized result");
            ((UnauthorizedResult)challengeContext.Result).Challenges.Single().Should().Be(expectedChallenge, "becaues the challene should match the value provided by the authentication handler");
        }

        /// <summary>
        ///   Verifies the behavior of the ChallengeAsync method.
        /// </summary>
        /// 
        [Fact]
        public async Task ChallengeAsyncSetsTheChallengeForAnExistingStatusUnauthorizedResponse()
        {            
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var mockDependencyScope  = new Mock<IDependencyScope>();
            var mockAuthHandler      = new Mock<IAuthenticationHandler>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var challengeContext     = new HttpAuthenticationChallengeContext(actionContext, new StatusCodeResult(System.Net.HttpStatusCode.Forbidden, request));
            var expectedChallenge    = new AuthenticationHeaderValue("DUMMY");
                        
            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            mockDependencyScope.Setup(scope => scope.GetService(It.Is<Type>(param => param == typeof(IHttpHeaderParser))))
                               .Returns(new HttpHeaderParser());

            mockDependencyScope.Setup(scope => scope.GetServices(It.Is<Type>(param => param == typeof(IAuthenticationHandler))))
                               .Returns(new [] { mockAuthHandler.Object, Mock.Of<IAuthenticationHandler>() });
           
            mockAuthHandler.SetupGet(handler => handler.Enabled).Returns(true);
            mockAuthHandler.SetupGet(handler => handler.HandlerType).Returns(AuthenticationType.SharedSecret);
            mockAuthHandler.SetupGet(handler => handler.Strength).Returns(AuthenticationStrength.Weak);  
            mockAuthHandler.SetupGet(handler => handler.CanGenerateChallenge).Returns(true);
            
            mockAuthHandler.Setup(handler => handler.GenerateChallenge(It.IsAny<IReadOnlyDictionary<string, string>>(), It.Is<HttpAuthenticationChallengeContext>(context => context == challengeContext)))
                           .Returns(expectedChallenge);

            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);            
            request.Properties.Add(HttpPropertyKeys.DependencyScope, mockDependencyScope.Object);

            actionContext.Response = request.CreateResponse(System.Net.HttpStatusCode.Unauthorized);

            var cancellationToken = new CancellationToken();
            var authAttribute     = new OrderFulfillmentAuthenticateAttributeAttribute();

            await authAttribute.ChallengeAsync(challengeContext, cancellationToken);
            challengeContext.ActionContext.Response.Should().BeNull("because the response message should have been cleared in favor of the action result.");
            challengeContext.Result.Should().BeOfType<UnauthorizedResult>("because the challenge should force an Unauthorized result");
            ((UnauthorizedResult)challengeContext.Result).Challenges.Single().Should().Be(expectedChallenge, "becaues the challene should match the value provided by the authentication handler");
        }

        /// <summary>
        ///   Verifies the behavior of the ChallengeAsync method.
        /// </summary>
        /// 
        [Fact]
        public async Task ChallengeAsyncSetsAResultWhenNoAuthHeaderWasPassed()
        {            
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var mockDependencyScope  = new Mock<IDependencyScope>();
            var mockAuthHandler      = new Mock<IAuthenticationHandler>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var challengeContext     = new HttpAuthenticationChallengeContext(actionContext, new UnauthorizedResult(new AuthenticationHeaderValue[0], request));
            var expectedChallenge    = new AuthenticationHeaderValue("DUMMY");
                        
            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            mockDependencyScope.Setup(scope => scope.GetService(It.Is<Type>(param => param == typeof(IHttpHeaderParser))))
                               .Returns(new HttpHeaderParser());

            mockDependencyScope.Setup(scope => scope.GetServices(It.Is<Type>(param => param == typeof(IAuthenticationHandler))))
                               .Returns(new [] { mockAuthHandler.Object, Mock.Of<IAuthenticationHandler>() });
           
            mockAuthHandler.SetupGet(handler => handler.Enabled).Returns(true);
            mockAuthHandler.SetupGet(handler => handler.HandlerType).Returns(AuthenticationType.SharedSecret);
            mockAuthHandler.SetupGet(handler => handler.Strength).Returns(AuthenticationStrength.Weak);  
            mockAuthHandler.SetupGet(handler => handler.CanGenerateChallenge).Returns(true);
            
            mockAuthHandler.Setup(handler => handler.GenerateChallenge(It.IsAny<IReadOnlyDictionary<string, string>>(), It.Is<HttpAuthenticationChallengeContext>(context => context == challengeContext)))
                           .Returns(expectedChallenge);

            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
            request.Properties.Add(HttpPropertyKeys.DependencyScope, mockDependencyScope.Object);
                        
            var cancellationToken = new CancellationToken();
            var authAttribute     = new OrderFulfillmentAuthenticateAttributeAttribute();

            await authAttribute.ChallengeAsync(challengeContext, cancellationToken);
            challengeContext.ActionContext.Response.Should().BeNull("because the response message should have been cleared in favor of the action result.");
            challengeContext.Result.Should().BeOfType<UnauthorizedResult>("because the challenge should force an Unauthorized result");
            ((UnauthorizedResult)challengeContext.Result).Challenges.Single().Should().Be(expectedChallenge, "becaues the challene should match the value provided by the authentication handler");
        }

        /// <summary>
        ///   Verifies the behavior of the ChallengeAsync method.
        /// </summary>
        /// 
        [Fact]
        public async Task ChallengeAsyncSetsAResultWhenAMalformedAuthHeaderWasPassed()
        {            
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var mockDependencyScope  = new Mock<IDependencyScope>();
            var mockAuthHandler      = new Mock<IAuthenticationHandler>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var challengeContext     = new HttpAuthenticationChallengeContext(actionContext, new UnauthorizedResult(new AuthenticationHeaderValue[0], request));
            var expectedChallenge    = new AuthenticationHeaderValue("DUMMY");
                        
            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            mockDependencyScope.Setup(scope => scope.GetService(It.Is<Type>(param => param == typeof(IHttpHeaderParser))))
                               .Returns(new HttpHeaderParser());

            mockDependencyScope.Setup(scope => scope.GetServices(It.Is<Type>(param => param == typeof(IAuthenticationHandler))))
                               .Returns(new [] { mockAuthHandler.Object, Mock.Of<IAuthenticationHandler>() });
           
            mockAuthHandler.SetupGet(handler => handler.Enabled).Returns(true);
            mockAuthHandler.SetupGet(handler => handler.HandlerType).Returns(AuthenticationType.SharedSecret);
            mockAuthHandler.SetupGet(handler => handler.Strength).Returns(AuthenticationStrength.Weak);  
            mockAuthHandler.SetupGet(handler => handler.CanGenerateChallenge).Returns(true);
            
            mockAuthHandler.Setup(handler => handler.GenerateChallenge(It.IsAny<IReadOnlyDictionary<string, string>>(), It.Is<HttpAuthenticationChallengeContext>(context => context == challengeContext)))
                           .Returns(expectedChallenge);

            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
            request.Headers.TryAddWithoutValidation(Core.Infrastructure.HttpHeaders.Authorization, String.Empty);
            request.Properties.Add(HttpPropertyKeys.DependencyScope, mockDependencyScope.Object);
                        
            var cancellationToken = new CancellationToken();
            var authAttribute     = new OrderFulfillmentAuthenticateAttributeAttribute();

            await authAttribute.ChallengeAsync(challengeContext, cancellationToken);
            challengeContext.ActionContext.Response.Should().BeNull("because the response message should have been cleared in favor of the action result.");
            challengeContext.Result.Should().BeOfType<UnauthorizedResult>("because the challenge should force an Unauthorized result");
            ((UnauthorizedResult)challengeContext.Result).Challenges.Single().Should().Be(expectedChallenge, "becaues the challene should match the value provided by the authentication handler");
        }

        /// <summary>
        ///   Verifies the behavior of the ChallengeAsync method.
        /// </summary>
        /// 
        [Fact]
        public async Task ChallengeAsyncUsesTheStrongestHandlerThatCanGenerateAsDefault()
        {            
            var mockActionDescriptor       = new Mock<HttpActionDescriptor>();
            var mockDependencyScope        = new Mock<IDependencyScope>();
            var mockWeakChallengeHandler   = new Mock<IAuthenticationHandler>();
            var mockStrongChallengeHandler = new Mock<IAuthenticationHandler>();
            var mockStrongHandler          = new Mock<IAuthenticationHandler>();
            var httpConfiguration          = new HttpConfiguration();
            var routeData                  = new HttpRouteData(new HttpRoute());
            var request                    = new HttpRequestMessage();
            var controllerDescriptor       = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext          = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext              = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var challengeContext           = new HttpAuthenticationChallengeContext(actionContext, new UnauthorizedResult(new AuthenticationHeaderValue[0], request));
            var expectedChallenge          = new AuthenticationHeaderValue("DUMMY");
                        
            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            mockDependencyScope.Setup(scope => scope.GetService(It.Is<Type>(param => param == typeof(IHttpHeaderParser))))
                               .Returns(new HttpHeaderParser());

            mockDependencyScope.Setup(scope => scope.GetServices(It.Is<Type>(param => param == typeof(IAuthenticationHandler))))
                               .Returns(new [] { mockWeakChallengeHandler.Object, mockStrongChallengeHandler.Object,  mockStrongHandler.Object });

            mockWeakChallengeHandler.SetupGet(handler => handler.Enabled).Returns(true);
            mockWeakChallengeHandler.SetupGet(handler => handler.HandlerType).Returns(AuthenticationType.SharedSecret);
            mockWeakChallengeHandler.SetupGet(handler => handler.Strength).Returns(AuthenticationStrength.Weak);
            mockWeakChallengeHandler.SetupGet(handler => handler.CanGenerateChallenge).Returns(true);

            mockWeakChallengeHandler.Setup(handler => handler.GenerateChallenge(It.IsAny<IReadOnlyDictionary<string, string>>(), It.Is<HttpAuthenticationChallengeContext>(context => context == challengeContext)))
                                    .Returns(new AuthenticationHeaderValue("I-AM-Wrong"));

            mockStrongChallengeHandler.SetupGet(handler => handler.Enabled).Returns(true);
            mockStrongChallengeHandler.SetupGet(handler => handler.HandlerType).Returns(AuthenticationType.Token);
            mockStrongChallengeHandler.SetupGet(handler => handler.Strength).Returns(AuthenticationStrength.Stronger);                  
            mockStrongChallengeHandler.SetupGet(handler => handler.CanGenerateChallenge).Returns(true);
            
            mockStrongChallengeHandler.Setup(handler => handler.GenerateChallenge(It.IsAny<IReadOnlyDictionary<string, string>>(), It.Is<HttpAuthenticationChallengeContext>(context => context == challengeContext)))
                                      .Returns(expectedChallenge);
                                      
            mockStrongHandler.SetupGet(handler => handler.Enabled).Returns(true);
            mockStrongHandler.SetupGet(handler => handler.HandlerType).Returns(AuthenticationType.ClientCertificate);
            mockStrongHandler.SetupGet(handler => handler.Strength).Returns(AuthenticationStrength.Strongest);                  
            mockStrongHandler.SetupGet(handler => handler.CanGenerateChallenge).Returns(false);

            mockStrongHandler.Setup(handler => handler.GenerateChallenge(It.IsAny<IReadOnlyDictionary<string, string>>(), It.Is<HttpAuthenticationChallengeContext>(context => context == challengeContext)))
                             .Returns(new AuthenticationHeaderValue("I-AM-Wrong"));
                           
            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
            request.Properties.Add(HttpPropertyKeys.DependencyScope, mockDependencyScope.Object);
                        
            var cancellationToken = new CancellationToken();
            var authAttribute     = new OrderFulfillmentAuthenticateAttributeAttribute();

            await authAttribute.ChallengeAsync(challengeContext, cancellationToken);
            challengeContext.Result.Should().BeOfType<UnauthorizedResult>("because the challenge should force an Unauthorized result");            
            ((UnauthorizedResult)challengeContext.Result).Challenges.Single().Should().Be(expectedChallenge, "Challenge generation should have been attempted on the strongest handler that can generate a challenge");
        }

        /// <summary>
        ///   Verifies the behavior of the ChallengeAsync method.
        /// </summary>
        /// 
        [Fact]
        public async Task ChallengeAsyncDoesNotSelectAHandlerThatCannotGenerate()
        {            
            var mockActionDescriptor       = new Mock<HttpActionDescriptor>();
            var mockDependencyScope        = new Mock<IDependencyScope>();
            var mockWeakChallengeHandler   = new Mock<IAuthenticationHandler>();
            var mockStrongChallengeHandler = new Mock<IAuthenticationHandler>();
            var mockStrongHandler          = new Mock<IAuthenticationHandler>();
            var httpConfiguration          = new HttpConfiguration();
            var routeData                  = new HttpRouteData(new HttpRoute());
            var request                    = new HttpRequestMessage();
            var controllerDescriptor       = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext          = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext              = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var expectedResult             = new UnauthorizedResult(new AuthenticationHeaderValue[0], request);
            var challengeContext           = new HttpAuthenticationChallengeContext(actionContext, expectedResult);
                        
            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            mockDependencyScope.Setup(scope => scope.GetService(It.Is<Type>(param => param == typeof(IHttpHeaderParser))))
                               .Returns(new HttpHeaderParser());

            mockDependencyScope.Setup(scope => scope.GetServices(It.Is<Type>(param => param == typeof(IAuthenticationHandler))))
                               .Returns(new [] { mockWeakChallengeHandler.Object, mockStrongChallengeHandler.Object, mockStrongHandler.Object });
           
            mockWeakChallengeHandler.SetupGet(handler => handler.Enabled).Returns(true);
            mockWeakChallengeHandler.SetupGet(handler => handler.HandlerType).Returns(AuthenticationType.SharedSecret);
            mockWeakChallengeHandler.SetupGet(handler => handler.Strength).Returns(AuthenticationStrength.Weak);
            mockWeakChallengeHandler.SetupGet(handler => handler.CanGenerateChallenge).Returns(true);

            mockWeakChallengeHandler.Setup(handler => handler.GenerateChallenge(It.IsAny<IReadOnlyDictionary<string, string>>(), It.Is<HttpAuthenticationChallengeContext>(context => context == challengeContext)))
                                    .Returns(new AuthenticationHeaderValue("I-AM-Wrong"));

            mockStrongChallengeHandler.SetupGet(handler => handler.Enabled).Returns(true);
            mockStrongChallengeHandler.SetupGet(handler => handler.HandlerType).Returns(AuthenticationType.Token);
            mockStrongChallengeHandler.SetupGet(handler => handler.Strength).Returns(AuthenticationStrength.Stronger);                  
            mockStrongChallengeHandler.SetupGet(handler => handler.CanGenerateChallenge).Returns(true);

            mockStrongChallengeHandler.Setup(handler => handler.GenerateChallenge(It.IsAny<IReadOnlyDictionary<string, string>>(), It.Is<HttpAuthenticationChallengeContext>(context => context == challengeContext)))
                                      .Returns(new AuthenticationHeaderValue("I-AM-Wrong"));

            mockStrongHandler.SetupGet(handler => handler.Enabled).Returns(true);
            mockStrongHandler.SetupGet(handler => handler.HandlerType).Returns(AuthenticationType.ClientCertificate);
            mockStrongHandler.SetupGet(handler => handler.Strength).Returns(AuthenticationStrength.Strongest);                  
            mockStrongHandler.SetupGet(handler => handler.CanGenerateChallenge).Returns(false);

            mockStrongHandler.Setup(handler => handler.GenerateChallenge(It.IsAny<IReadOnlyDictionary<string, string>>(), It.Is<HttpAuthenticationChallengeContext>(context => context == challengeContext)))
                             .Returns(new AuthenticationHeaderValue("I-AM-Wrong"));

            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
            request.Headers.TryAddWithoutValidation(Core.Infrastructure.HttpHeaders.Authorization, $"not-real realm=\"localhost\"");
            request.Headers.TryAddWithoutValidation(Core.Infrastructure.HttpHeaders.ApplicationKey, "some-key");
            request.Headers.TryAddWithoutValidation(Core.Infrastructure.HttpHeaders.ApplicationSecret, "some secret!");
            request.Properties.Add(HttpPropertyKeys.DependencyScope, mockDependencyScope.Object);
                        
            var cancellationToken = new CancellationToken();
            var authAttribute     = new OrderFulfillmentAuthenticateAttributeAttribute();

            await authAttribute.ChallengeAsync(challengeContext, cancellationToken);

            mockDependencyScope.Verify(scope => scope.GetServices(It.Is<Type>(param => param == typeof(IAuthenticationHandler))), Times.AtLeastOnce, "The authentication handler should have been sought");
            mockStrongChallengeHandler.Verify(handler => handler.GenerateChallenge(It.IsAny<IReadOnlyDictionary<string, string>>(), It.Is<HttpAuthenticationChallengeContext>(context => context == challengeContext)), Times.AtLeastOnce, "Chalenge generation should have been attempted on the strongest handler that can generate a challenge");
        }

        /// <summary>
        ///   Verifies the behavior of the ChallengeAsync method.
        /// </summary>
        /// 
        [Fact]
        public async Task ChallengeAsyncLogsWhenAChallengeWasSet()
        {         
            var requestBody          = "REQUEST BODY CONTENT";   
            var mockLogger           = new Mock<ILogger>();
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var mockDependencyScope  = new Mock<IDependencyScope>();
            var mockAuthHandler      = new Mock<IAuthenticationHandler>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var challengeContext     = new HttpAuthenticationChallengeContext(actionContext, new UnauthorizedResult(new AuthenticationHeaderValue[0], request));
            var expectedChallenge    = new AuthenticationHeaderValue("DUMMY");
                        
            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            mockDependencyScope.Setup(scope => scope.GetService(It.Is<Type>(type => type == typeof(ILogger))))
                               .Returns(mockLogger.Object);

            mockDependencyScope.Setup(scope => scope.GetService(It.Is<Type>(param => param == typeof(IHttpHeaderParser))))
                               .Returns(new HttpHeaderParser());

            mockDependencyScope.Setup(scope => scope.GetServices(It.Is<Type>(param => param == typeof(IAuthenticationHandler))))
                               .Returns(new [] { mockAuthHandler.Object, Mock.Of<IAuthenticationHandler>() });
           
            mockAuthHandler.SetupGet(handler => handler.Enabled).Returns(true);
            mockAuthHandler.SetupGet(handler => handler.HandlerType).Returns(AuthenticationType.SharedSecret);
            mockAuthHandler.SetupGet(handler => handler.Strength).Returns(AuthenticationStrength.Weak); 
            mockAuthHandler.SetupGet(handler => handler.CanGenerateChallenge).Returns(true); 
            
            mockAuthHandler.Setup(handler => handler.GenerateChallenge(It.IsAny<IReadOnlyDictionary<string, string>>(), It.Is<HttpAuthenticationChallengeContext>(context => context == challengeContext)))
                           .Returns(expectedChallenge);

            mockLogger.Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                      .Returns(mockLogger.Object);
            
            request.Content = new StringContent(requestBody);
            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
            request.Properties.Add(HttpPropertyKeys.DependencyScope, mockDependencyScope.Object);

            await request.Content.ReadAsStringAsync();
                        
            var cancellationToken = new CancellationToken();
            var authAttribute     = new OrderFulfillmentAuthenticateAttributeAttribute();

            await authAttribute.ChallengeAsync(challengeContext, cancellationToken);

            mockLogger.Verify(logger => logger.Information(It.IsAny<string>(),
                                                           It.Is<HttpStatusCode>(code => code == HttpStatusCode.Unauthorized),
                                                           It.Is<Uri>(uri => uri == request.RequestUri), 
                                                           It.Is<HttpRequestHeaders>(headers => headers == request.Headers)), 
                                            Times.Once, "The challenge issuance should have been logged");
        }

        /// <summary>
        ///   Verifies the behavior of the ChallengeAsync method.
        /// </summary>
        /// 
        [Fact]
        public async Task ChallengeAsyncDoesNotLogWhenAChallengeWasNotSet()
        {         
            var requestBody          = "REQUEST BODY CONTENT";   
            var mockLogger           = new Mock<ILogger>();
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var mockDependencyScope  = new Mock<IDependencyScope>();
            var mockAuthHandler      = new Mock<IAuthenticationHandler>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var challengeContext     = new HttpAuthenticationChallengeContext(actionContext, new OkResult(request));
            var expectedChallenge    = new AuthenticationHeaderValue("DUMMY");
                        
            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            mockDependencyScope.Setup(scope => scope.GetService(It.Is<Type>(type => type == typeof(ILogger))))
                               .Returns(mockLogger.Object);

            mockDependencyScope.Setup(scope => scope.GetService(It.Is<Type>(param => param == typeof(IHttpHeaderParser))))
                               .Returns(new HttpHeaderParser());

            mockDependencyScope.Setup(scope => scope.GetServices(It.Is<Type>(param => param == typeof(IAuthenticationHandler))))
                               .Returns(new [] { mockAuthHandler.Object, Mock.Of<IAuthenticationHandler>() });
           
            mockAuthHandler.SetupGet(handler => handler.Enabled).Returns(true);
            mockAuthHandler.SetupGet(handler => handler.HandlerType).Returns(AuthenticationType.SharedSecret);
            mockAuthHandler.SetupGet(handler => handler.Strength).Returns(AuthenticationStrength.Weak);  
            
            mockAuthHandler.Setup(handler => handler.GenerateChallenge(It.IsAny<IReadOnlyDictionary<string, string>>(), It.Is<HttpAuthenticationChallengeContext>(context => context == challengeContext)))
                           .Returns(expectedChallenge);

            mockLogger.Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                      .Returns(mockLogger.Object);
            
            request.Content = new StringContent(requestBody);
            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
            request.Properties.Add(HttpPropertyKeys.DependencyScope, mockDependencyScope.Object);
                        
            var cancellationToken = new CancellationToken();
            var authAttribute     = new OrderFulfillmentAuthenticateAttributeAttribute();

            await authAttribute.ChallengeAsync(challengeContext, cancellationToken);

            mockLogger.Verify(logger => logger.Information(It.IsAny<string>(), 
                                                           It.IsAny<Uri>(), 
                                                           It.IsAny<HttpRequestHeaders>(), 
                                                           It.IsAny<string>()), 
                                            Times.Never, "No challenge was issued, no log should have been written");
        }
    }
}
