using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dependencies;
using System.Web.Http.Hosting;
using System.Web.Http.Routing;
using FluentAssertions;
using OrderFulfillment.Api.Filters;
using OrderFulfillment.Api.Security;
using OrderFulfillment.Core.Infrastructure;
using Moq;
using Serilog;
using Xunit;

namespace OrderFulfillment.Api.Tests.Filters
{
    /// <summary>
    ///   The suite of tests for the <see cref="OrderFulfillment.Api.Common.Filters.OrderfulfillmentAuthorizeAttribute"/>
    ///   class.
    /// </summary> 
    /// 
    public class OrderFulfillmentAuthorizeAttributeTests
    {
        /// <summary>
        ///   Verifies the behavior of the OnAuthorizationAsync method.
        /// </summary>
        /// 
        [Fact]
        public void AuthorizeAsyncValidatesTheContext()
        {
            Action actionUnderTest = () => new OrderFulfillmentAuthorizeAttribute().OnAuthorizationAsync(null, CancellationToken.None).GetAwaiter().GetResult();
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the authorization context should be validated");
        }

        /// <summary>
        ///   Verifies the behavior of the OnAuthorizationAsync method.
        /// </summary>
        /// 
        [Fact]
        public async Task AuthorizeAsyncHonorsTheCancellationToken()
        {            
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var mockDependencyScope  = new Mock<IDependencyScope>();
            var mockAuthPolicy       = new Mock<IAuthorizationPolicy>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
                        
            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            mockDependencyScope.Setup(scope => scope.GetServices(It.Is<Type>(param => param == typeof(IAuthorizationPolicy))))
                               .Returns(new [] { mockAuthPolicy.Object });
           
            mockAuthPolicy.SetupGet(policy => policy.Enabled).Returns(true);
            mockAuthPolicy.SetupGet(policy => policy.Policy).Returns(AuthorizationPolicy.AuthenticatedPrincipal);
            mockAuthPolicy.Setup(policy => policy.Evaluate(It.Is<HttpActionContext>(context => context == actionContext))).Returns(HttpStatusCode.Forbidden);

            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
            request.Properties.Add(HttpPropertyKeys.DependencyScope, mockDependencyScope.Object);
                        
            var cancellationToken = new CancellationToken(true);
            var authAttribute     = new OrderFulfillmentAuthorizeAttribute();

            await authAttribute.OnAuthorizationAsync(actionContext, cancellationToken);
            actionContext.Response.Should().BeNull("because cancellation should have occured before authorization took place");

            mockDependencyScope.Verify(scope => scope.GetServices(It.Is<Type>(param => param == typeof(IAuthorizationPolicy))), Times.Never, "The cancellation should have taken place before a policy was sought");
        }

        /// <summary>
        ///   Verifies the behavior of the OnAuthorizationAsync method.
        /// </summary>
        /// 
        [Fact]
        public async Task AuthorizeAsyncRespectsAFailedRequest()
        {            
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var mockDependencyScope  = new Mock<IDependencyScope>();
            var mockAuthPolicy       = new Mock<IAuthorizationPolicy>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();            
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var response             = request.CreateResponse(HttpStatusCode.ServiceUnavailable);
                        
            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            mockDependencyScope.Setup(scope => scope.GetServices(It.Is<Type>(param => param == typeof(IAuthorizationPolicy))))
                               .Returns(new [] { mockAuthPolicy.Object });
           
            mockAuthPolicy.SetupGet(policy => policy.Enabled).Returns(true);
            mockAuthPolicy.SetupGet(policy => policy.Policy).Returns(AuthorizationPolicy.AuthenticatedPrincipal);
            mockAuthPolicy.Setup(policy => policy.Evaluate(It.Is<HttpActionContext>(context => context == actionContext))).Returns(HttpStatusCode.Unauthorized);
                       
            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
            request.Properties.Add(HttpPropertyKeys.DependencyScope, mockDependencyScope.Object);

            actionContext.Response = response;
                        
            var authAttribute = new OrderFulfillmentAuthorizeAttribute(AuthorizationPolicy.AuthenticatedPrincipal);

            await authAttribute.OnAuthorizationAsync(actionContext, CancellationToken.None);
            actionContext.Response.Should().Be(response, "because the existing failure result should have been detected before authorization took place");

            mockDependencyScope.Verify(scope => scope.GetServices(It.Is<Type>(param => param == typeof(IAuthorizationPolicy))), Times.Never, "The cancellation should have taken place before a policy was sought");
        }

        /// <summary>
        ///   Verifies the behavior of the OnAuthorizationAsync method.
        /// </summary>
        /// 
        [Fact]
        public async Task AuthorizeAsyncFailsWhenTheSinglePolicyIsUnsatisfied()
        {            
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var mockDependencyScope  = new Mock<IDependencyScope>();
            var mockAuthPolicy       = new Mock<IAuthorizationPolicy>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var expectedStatus       = HttpStatusCode.Unauthorized;
                        
            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            mockDependencyScope.Setup(scope => scope.GetServices(It.Is<Type>(param => param == typeof(IAuthorizationPolicy))))
                               .Returns(new [] { mockAuthPolicy.Object });
           
            mockAuthPolicy.SetupGet(policy => policy.Enabled).Returns(true);
            mockAuthPolicy.SetupGet(policy => policy.Policy).Returns(AuthorizationPolicy.AuthenticatedPrincipal);
            mockAuthPolicy.Setup(policy => policy.Evaluate(It.Is<HttpActionContext>(context => context == actionContext))).Returns(expectedStatus);

            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
            request.Properties.Add(HttpPropertyKeys.DependencyScope, mockDependencyScope.Object);
            
            var authAttribute = new OrderFulfillmentAuthorizeAttribute(AuthorizationPolicy.AuthenticatedPrincipal);

            await authAttribute.OnAuthorizationAsync(actionContext, CancellationToken.None);
            actionContext.Response.Should().NotBeNull("because a failed authorization should set the result");
            actionContext.Response.StatusCode.Should().Be(expectedStatus, "because callers are forbidden when authorization fails.");
        }

        /// <summary>
        ///   Verifies the behavior of the OnAuthorizationAsync method.
        /// </summary>
        /// 
        [Fact]
        public async Task AuthorizeAsyncFailsWhenAnyPolicyIsUnsatisfied()
        {            
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var mockDependencyScope  = new Mock<IDependencyScope>();
            var mockPrincipalPolicy  = new Mock<IAuthorizationPolicy>();
            var mockSslPolicy        = new Mock<IAuthorizationPolicy>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var expectedStatus       = HttpStatusCode.Forbidden;
                        
            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            mockDependencyScope.Setup(scope => scope.GetServices(It.Is<Type>(param => param == typeof(IAuthorizationPolicy))))
                               .Returns(new [] { mockPrincipalPolicy.Object, mockSslPolicy.Object });
           
            mockPrincipalPolicy.SetupGet(policy => policy.Enabled).Returns(true);
            mockPrincipalPolicy.SetupGet(policy => policy.Policy).Returns(AuthorizationPolicy.AuthenticatedPrincipal);
            mockPrincipalPolicy.Setup(policy => policy.Evaluate(It.Is<HttpActionContext>(context => context == actionContext))).Returns(expectedStatus);

            mockSslPolicy.SetupGet(policy => policy.Enabled).Returns(true);
            mockSslPolicy.SetupGet(policy => policy.Policy).Returns(AuthorizationPolicy.RequireSsl);
            mockSslPolicy.Setup(policy => policy.Evaluate(It.Is<HttpActionContext>(context => context == actionContext))).Returns((HttpStatusCode?)null);

            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
            request.Properties.Add(HttpPropertyKeys.DependencyScope, mockDependencyScope.Object);
            
            var authAttribute = new OrderFulfillmentAuthorizeAttribute();

            await authAttribute.OnAuthorizationAsync(actionContext, CancellationToken.None);
            actionContext.Response.Should().NotBeNull("because a failed authorization should set the result");
            actionContext.Response.StatusCode.Should().Be(expectedStatus, "because callers are forbidden when authorization fails.");
        }

        /// <summary>
        ///   Verifies the behavior of the OnAuthorizationAsync method.
        /// </summary>
        /// 
        [Fact]
        public async Task AuthorizeAsyncLogsWhenAnyPolicyIsUnsatisfied()
        {   
            var requestBody          = "REQUEST BODY CONTENT";
            var mockLogger           = new Mock<ILogger>();                   
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var mockDependencyScope  = new Mock<IDependencyScope>();
            var mockPrincipalPolicy  = new Mock<IAuthorizationPolicy>();
            var mockSslPolicy        = new Mock<IAuthorizationPolicy>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var expectedStatus       = HttpStatusCode.Forbidden;
                        
            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            mockDependencyScope.Setup(scope => scope.GetServices(It.Is<Type>(param => param == typeof(IAuthorizationPolicy))))
                               .Returns(new [] { mockPrincipalPolicy.Object, mockSslPolicy.Object });

            mockDependencyScope.Setup(scope => scope.GetService(It.Is<Type>(type => type == typeof(ILogger))))
                               .Returns(mockLogger.Object);
           
            mockPrincipalPolicy.SetupGet(policy => policy.Enabled).Returns(true);
            mockPrincipalPolicy.SetupGet(policy => policy.Policy).Returns(AuthorizationPolicy.AuthenticatedPrincipal);
            mockPrincipalPolicy.Setup(policy => policy.Evaluate(It.Is<HttpActionContext>(context => context == actionContext))).Returns(expectedStatus);

            mockSslPolicy.SetupGet(policy => policy.Enabled).Returns(true);
            mockSslPolicy.SetupGet(policy => policy.Policy).Returns(AuthorizationPolicy.RequireSsl);
            mockSslPolicy.Setup(policy => policy.Evaluate(It.Is<HttpActionContext>(context => context == actionContext))).Returns((HttpStatusCode?)null);

            mockLogger.Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                      .Returns(mockLogger.Object);

            request.Content = new StringContent(requestBody);
            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
            request.Properties.Add(HttpPropertyKeys.DependencyScope, mockDependencyScope.Object);

            await request.Content.ReadAsStringAsync();
            await (new OrderFulfillmentAuthorizeAttribute().OnAuthorizationAsync(actionContext, CancellationToken.None));

            mockLogger.Verify(logger => logger.Information(It.IsAny<string>(), 
                                                           It.Is<HttpStatusCode>(status => status == expectedStatus),
                                                           It.Is<string>(policy => policy == mockPrincipalPolicy.Object.GetType().Name),                                                            
                                                           It.Is<Uri>(uri => uri == request.RequestUri), 
                                                           It.Is<HttpRequestHeaders>(headers => headers == request.Headers)), 
                                            Times.Once, "The authentication result should have been logged");
        }
    
        /// <summary>
        ///   Verifies the behavior of the OnAuthorizationAsync method.
        /// </summary>
        /// 
        [Fact]
        public async Task AuthorizeAsyncIgnoresDisabledPolicies()
        {            
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var mockDependencyScope  = new Mock<IDependencyScope>();
            var mockPrincipalPolicy  = new Mock<IAuthorizationPolicy>();
            var mockSslPolicy        = new Mock<IAuthorizationPolicy>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
                        
            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            mockDependencyScope.Setup(scope => scope.GetServices(It.Is<Type>(param => param == typeof(IAuthorizationPolicy))))
                               .Returns(new [] { mockPrincipalPolicy.Object, mockSslPolicy.Object });
           
            mockPrincipalPolicy.SetupGet(policy => policy.Enabled).Returns(false);
            mockPrincipalPolicy.SetupGet(policy => policy.Policy).Returns(AuthorizationPolicy.AuthenticatedPrincipal);
            mockPrincipalPolicy.Setup(policy => policy.Evaluate(It.Is<HttpActionContext>(context => context == actionContext))).Returns(HttpStatusCode.Unauthorized);

            mockSslPolicy.SetupGet(policy => policy.Enabled).Returns(true);
            mockSslPolicy.SetupGet(policy => policy.Policy).Returns(AuthorizationPolicy.RequireSsl);
            mockSslPolicy.Setup(policy => policy.Evaluate(It.Is<HttpActionContext>(context => context == actionContext))).Returns((HttpStatusCode?)null);

            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
            request.Properties.Add(HttpPropertyKeys.DependencyScope, mockDependencyScope.Object);
            
            var authAttribute = new OrderFulfillmentAuthorizeAttribute();

            await authAttribute.OnAuthorizationAsync(actionContext, CancellationToken.None);
            actionContext.Response.Should().BeNull("because a disabled policy should not be considered");
        }

        /// <summary>
        ///   Verifies the behavior of the OnAuthorizationAsync method.
        /// </summary>
        /// 
        [Fact]
        public async Task AuthorizeAsyncDoesNotFailWhenPoliciesAreSatisfied()
        {            
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var mockDependencyScope  = new Mock<IDependencyScope>();
            var mockPrincipalPolicy  = new Mock<IAuthorizationPolicy>();
            var mockSslPolicy        = new Mock<IAuthorizationPolicy>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
                        
            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            mockDependencyScope.Setup(scope => scope.GetServices(It.Is<Type>(param => param == typeof(IAuthorizationPolicy))))
                               .Returns(new [] { mockPrincipalPolicy.Object, mockSslPolicy.Object });
           
            mockPrincipalPolicy.SetupGet(policy => policy.Enabled).Returns(true);
            mockPrincipalPolicy.SetupGet(policy => policy.Policy).Returns(AuthorizationPolicy.AuthenticatedPrincipal);
            mockPrincipalPolicy.Setup(policy => policy.Evaluate(It.Is<HttpActionContext>(context => context == actionContext))).Returns((HttpStatusCode?)null);

            mockSslPolicy.SetupGet(policy => policy.Enabled).Returns(true);
            mockSslPolicy.SetupGet(policy => policy.Policy).Returns(AuthorizationPolicy.RequireSsl);
            mockSslPolicy.Setup(policy => policy.Evaluate(It.Is<HttpActionContext>(context => context == actionContext))).Returns((HttpStatusCode?)null);

            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
            request.Properties.Add(HttpPropertyKeys.DependencyScope, mockDependencyScope.Object);
            
            var authAttribute = new OrderFulfillmentAuthorizeAttribute();

            await authAttribute.OnAuthorizationAsync(actionContext, CancellationToken.None);
            actionContext.Response.Should().BeNull("because a result should only be set on failure");
        }

        /// <summary>
        ///   Verifies the behavior of the OnAuthorizationAsync method.
        /// </summary>
        /// 
        [Fact]
        public async Task AuthorizeAsyncIgnoresMissingPolicies()
        {            
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var mockDependencyScope  = new Mock<IDependencyScope>();
            var mockPrincipalPolicy  = new Mock<IAuthorizationPolicy>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
                        
            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            mockDependencyScope.Setup(scope => scope.GetServices(It.Is<Type>(param => param == typeof(IAuthorizationPolicy))))
                               .Returns(new [] { mockPrincipalPolicy.Object });
           
            mockPrincipalPolicy.SetupGet(policy => policy.Enabled).Returns(true);
            mockPrincipalPolicy.SetupGet(policy => policy.Policy).Returns(AuthorizationPolicy.AuthenticatedPrincipal);
            mockPrincipalPolicy.Setup(policy => policy.Evaluate(It.Is<HttpActionContext>(context => context == actionContext))).Returns((HttpStatusCode?)null);

            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
            request.Properties.Add(HttpPropertyKeys.DependencyScope, mockDependencyScope.Object);
            
            var authAttribute = new OrderFulfillmentAuthorizeAttribute(AuthorizationPolicy.AuthenticatedPrincipal, AuthorizationPolicy.RequireSsl);

            await authAttribute.OnAuthorizationAsync(actionContext, CancellationToken.None);
            actionContext.Response.Should().BeNull("because a result should only be set on failure");
        }

        /// <summary>
        ///   Verifies the behavior of the OnAuthorizationAsync method.
        /// </summary>
        /// 
        [Fact]
        public async Task AuthorizeAsyncWhenNoPoliciesAreActive()
        {            
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var mockDependencyScope  = new Mock<IDependencyScope>();
            var mockPrincipalPolicy  = new Mock<IAuthorizationPolicy>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
                        
            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            mockDependencyScope.Setup(scope => scope.GetServices(It.Is<Type>(param => param == typeof(IAuthorizationPolicy))))
                               .Returns(new [] { mockPrincipalPolicy.Object });
           
            mockPrincipalPolicy.SetupGet(policy => policy.Enabled).Returns(true);
            mockPrincipalPolicy.SetupGet(policy => policy.Policy).Returns(AuthorizationPolicy.AuthenticatedPrincipal);
            mockPrincipalPolicy.Setup(policy => policy.Evaluate(It.Is<HttpActionContext>(context => context == actionContext))).Returns(HttpStatusCode.Unauthorized);

            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
            request.Properties.Add(HttpPropertyKeys.DependencyScope, mockDependencyScope.Object);
            
            var authAttribute = new OrderFulfillmentAuthorizeAttribute(new AuthorizationPolicy[0]);

            await authAttribute.OnAuthorizationAsync(actionContext, CancellationToken.None);
            actionContext.Response.Should().NotBeNull("because there should have been a policy failure");
            actionContext.Response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "because an enabled policy should have failed");
        }

        /// <summary>
        ///   Verifies the behavior of the OnAuthorizationAsync method.
        /// </summary>
        /// 
        [Fact]
        public async Task AuthorizeAsyncOverridesASuccessResultWhenNotAuthorized()
        {            
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var mockDependencyScope  = new Mock<IDependencyScope>();
            var mockPrincipalPolicy  = new Mock<IAuthorizationPolicy>();
            var mockSslPolicy        = new Mock<IAuthorizationPolicy>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var expectedStatus       = HttpStatusCode.Unauthorized;
                        
            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            mockDependencyScope.Setup(scope => scope.GetServices(It.Is<Type>(param => param == typeof(IAuthorizationPolicy))))
                               .Returns(new [] { mockPrincipalPolicy.Object, mockSslPolicy.Object });
           
            mockPrincipalPolicy.SetupGet(policy => policy.Enabled).Returns(true);
            mockPrincipalPolicy.SetupGet(policy => policy.Policy).Returns(AuthorizationPolicy.AuthenticatedPrincipal);
            mockPrincipalPolicy.Setup(policy => policy.Evaluate(It.Is<HttpActionContext>(context => context == actionContext))).Returns(expectedStatus);

            mockSslPolicy.SetupGet(policy => policy.Enabled).Returns(true);
            mockSslPolicy.SetupGet(policy => policy.Policy).Returns(AuthorizationPolicy.RequireSsl);
            mockSslPolicy.Setup(policy => policy.Evaluate(It.Is<HttpActionContext>(context => context == actionContext))).Returns((HttpStatusCode?)null);

            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
            request.Properties.Add(HttpPropertyKeys.DependencyScope, mockDependencyScope.Object);

            actionContext.Response = request.CreateResponse(HttpStatusCode.OK, "This was sucessful");
            
            var authAttribute = new OrderFulfillmentAuthorizeAttribute();

            await authAttribute.OnAuthorizationAsync(actionContext, CancellationToken.None);
            actionContext.Response.Should().NotBeNull("because a failed authorization should set the result");
            actionContext.Response.StatusCode.Should().Be(expectedStatus, "because callers are forbidden when authorization fails.");
        }

        /// <summary>
        ///   Verifies the behavior of the OnAuthorizationAsync method.
        /// </summary>
        /// 
        [Fact]
        public async Task AuthorizeAsyncEvaluatesPoliciesInPriorityOrder()
        {            
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var mockDependencyScope  = new Mock<IDependencyScope>();
            var mockPrincipalPolicy  = new Mock<IAuthorizationPolicy>();
            var mockSslPolicy        = new Mock<IAuthorizationPolicy>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
                        
            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            mockDependencyScope.Setup(scope => scope.GetServices(It.Is<Type>(param => param == typeof(IAuthorizationPolicy))))
                               .Returns(new [] { mockPrincipalPolicy.Object, mockSslPolicy.Object });
           
            mockPrincipalPolicy.SetupGet(policy => policy.Enabled).Returns(true);
            mockPrincipalPolicy.SetupGet(policy => policy.Priority).Returns(Priority.Low);
            mockPrincipalPolicy.SetupGet(policy => policy.Policy).Returns(AuthorizationPolicy.AuthenticatedPrincipal);
            mockPrincipalPolicy.Setup(policy => policy.Evaluate(It.Is<HttpActionContext>(context => context == actionContext))).Returns(HttpStatusCode.Unauthorized);

            mockSslPolicy.SetupGet(policy => policy.Enabled).Returns(true);
            mockSslPolicy.SetupGet(policy => policy.Priority).Returns(Priority.High);
            mockSslPolicy.SetupGet(policy => policy.Policy).Returns(AuthorizationPolicy.RequireSsl);
            mockSslPolicy.Setup(policy => policy.Evaluate(It.Is<HttpActionContext>(context => context == actionContext))).Returns(HttpStatusCode.Forbidden);

            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
            request.Properties.Add(HttpPropertyKeys.DependencyScope, mockDependencyScope.Object);

            actionContext.Response = request.CreateResponse(HttpStatusCode.OK, "This was sucessful");
            
            var authAttribute = new OrderFulfillmentAuthorizeAttribute();

            await authAttribute.OnAuthorizationAsync(actionContext, CancellationToken.None);
            mockSslPolicy.Verify(policy => policy.Evaluate(It.IsAny<HttpActionContext>()), Times.Once, "The SSL policy should have been evaluated because it is higher priority");
            mockPrincipalPolicy.Verify(policy => policy.Evaluate(It.IsAny<HttpActionContext>()), Times.Never, "The principal  policy should not have been evaluated because it a higher priority policy already failed");
        }
    }
}
