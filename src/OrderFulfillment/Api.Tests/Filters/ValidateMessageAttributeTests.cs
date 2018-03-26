using System;
using System.Linq;
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
using OrderFulfillment.Api.Models.Requests;
using OrderFulfillment.Core.Models.Errors;
using OrderFulfillment.Core.Validators;
using Moq;
using Serilog;
using Xunit;

namespace OrderFulfillment.Api.Tests.Filters
{
    /// <summary>
    ///   The suite of tests for verifying the <see cref="MessageValidatorAttribute"/>
    ///   class.
    /// </summary>
    /// 
    public class ValidateMessageAttributeTests
    {
        /// <summary>
        ///   Verifies that the proper message validators are requested.
        /// </summary>
        ///         
        [Fact]
        public async Task MessageValidatorIsRequestedForASingleParameter()
        {
            var parameterName        = "testName";
            var parameterValue       = new object();
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var mockDependencyScope  = new Mock<IDependencyScope>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var messageValidator     = new ValidateMessageAttribute();

            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            mockDependencyScope.Setup(scope => scope.GetService(It.Is<Type>(param => param == parameterValue.GetType())))
                               .Returns(null)
                               .Verifiable("the dependency scope should have requested a validator for the parameter");
                               
            actionContext.ActionArguments.Add(parameterName, parameterValue);

            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
            request.Properties.Add(HttpPropertyKeys.DependencyScope, mockDependencyScope.Object);
            
            await messageValidator.OnActionExecutingAsync(actionContext, new CancellationToken());            
        }

        [Fact]
        public void MessageValidationOfNullParameterIsIgnored()
        {
            var parameterName        = "testName";
            var parameterValue       = (object)null;
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var mockDependencyScope  = new Mock<IDependencyScope>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var messageValidator     = new ValidateMessageAttribute();

            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            actionContext.ActionArguments.Add(parameterName, parameterValue);

            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
            request.Properties.Add(HttpPropertyKeys.DependencyScope, mockDependencyScope.Object);

            Func<Task> act = async () => await messageValidator.OnActionExecutingAsync(actionContext, new CancellationToken());
            act.ShouldNotThrow();
        }

        /// <summary>
        ///   Verifies that the proper message validators are requested.
        /// </summary>
        ///         
        [Fact]
        public async Task MessageValidatorIsRequestedForEachParameter()
        {
            var parameterNames       = new [] { "one", "two" };
            var parameterValues      = new [] { new object(), "stringVlalue" };
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var mockDependencyScope  = new Mock<IDependencyScope>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var messageValidator     = new ValidateMessageAttribute();

            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");
            mockDependencyScope.Setup(scope => scope.GetService(It.IsAny<Type>())).Returns(null);
            
            for (var index = 0; index < parameterNames.Length; ++index)
            {                   
              actionContext.ActionArguments.Add(parameterNames[index], parameterValues[index]);
            }

            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
            request.Properties.Add(HttpPropertyKeys.DependencyScope, mockDependencyScope.Object);
            
            await messageValidator.OnActionExecutingAsync(actionContext, new CancellationToken());
            
            mockDependencyScope.Verify(scope => scope.GetService(It.IsAny<Type>()), Times.Exactly(2), "each parameter should have had a corresponding validator request"); 
        }

        /// <summary>
        ///   Verifies that the message validators is invoked.
        /// </summary>
        ///         
        [Fact]
        public async Task MessageValidatorIsInvoked()
        {
            var parameterName        ="one";
            var parameterValue       = new object();
            var mockValidator        = new Mock<IValidator>();
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var mockDependencyScope  = new Mock<IDependencyScope>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var messageValidator     = new ValidateMessageAttribute();

            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            mockDependencyScope.Setup(scope => scope.GetService(It.IsAny<Type>()))
                               .Returns(null)
                               .Verifiable("Tthe validator should have been requested.");

            mockValidator.Setup(validator => validator.Validate(It.Is<object>(param => param == parameterValue)))
                         .Returns(Enumerable.Empty<Error>())
                         .Verifiable("The validator should have been used to validate the parameter.");
            
            actionContext.ActionArguments.Add(parameterName, parameterValue);

            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
            request.Properties.Add(HttpPropertyKeys.DependencyScope, mockDependencyScope.Object);
            
            await messageValidator.OnActionExecutingAsync(actionContext, new CancellationToken());
        }

        /// <summary>
        ///   Verifies that the error response is sent appropriately.
        /// </summary>
        ///         
        [Fact]
        public async Task NoResponseIsSetForValidParameters()
        {
            var parameterName        ="one";
            var parameterValue       = new object();
            var mockValidator        = new Mock<IValidator>();
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var mockDependencyScope  = new Mock<IDependencyScope>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var messageValidator     = new ValidateMessageAttribute();

            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            mockDependencyScope.Setup(scope => scope.GetService(It.IsAny<Type>()))
                               .Returns(mockValidator.Object)
                               .Verifiable("Tthe validator should have been requested.");

            mockValidator.Setup(validator => validator.Validate(It.Is<object>(param => param == parameterValue)))
                         .Returns(Enumerable.Empty<Error>())
                         .Verifiable("The validator should have been used to validate the parameter.");
            
            actionContext.ActionArguments.Add(parameterName, parameterValue);

            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
            request.Properties.Add(HttpPropertyKeys.DependencyScope, mockDependencyScope.Object);
            
            await messageValidator.OnActionExecutingAsync(actionContext, new CancellationToken());

            actionContext.Response.Should().BeNull("because no response should be set when there are no failures");
        }

        /// <summary>
        ///   Verifies that failed requests are logged appopriately.
        /// </summary>
        ///         
        [Fact]
        public async Task NoLogIsWrittenForValidParameters()
        {
            var parameterName        = "one";
            var parameterValue       = new object();
            var mockLogger           = new Mock<ILogger>();
            var mockValidator        = new Mock<IValidator>();
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var mockDependencyScope  = new Mock<IDependencyScope>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var messageValidator     = new ValidateMessageAttribute();

            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            mockDependencyScope.Setup(scope => scope.GetService(It.Is<Type>(type => type == typeof(ILogger))))
                               .Returns(mockLogger.Object);

            mockDependencyScope.Setup(scope => scope.GetService(It.Is<Type>(type => type != typeof(ILogger))))
                               .Returns(mockValidator.Object);

            mockValidator.Setup(validator => validator.Validate(It.Is<object>(param => param == parameterValue)))
                         .Returns(Enumerable.Empty<Error>());

            mockLogger.Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                      .Returns(mockLogger.Object);

            actionContext.ActionArguments.Add(parameterName, parameterValue);

            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
            request.Properties.Add(HttpPropertyKeys.DependencyScope, mockDependencyScope.Object);

            await messageValidator.OnActionExecutingAsync(actionContext, new CancellationToken());

            actionContext.Response.Should().BeNull("because no response should be set when there are no failures");
                        
            mockDependencyScope.Verify(scope => scope.GetService(It.Is<Type>(type => type == typeof(ILogger))), Times.Never, "The logger should only be requested when validation fails");
            mockLogger.Verify(logger => logger.Information(It.IsAny<string>()), Times.Never, "A log should only be written when validation fails");
        }

        /// <summary>
        ///   Verifies that the error response is sent appropriately.
        /// </summary>
        ///         
        [Fact]
        public async Task ResponseIsSetForFailedValidations()
        {
            var parameterName        ="one";
            var parameterValue       = new object();
            var mockValidator        = new Mock<IValidator>();
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var mockDependencyScope  = new Mock<IDependencyScope>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var messageValidator     = new ValidateMessageAttribute();

            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            mockDependencyScope.Setup(scope => scope.GetService(It.IsAny<Type>()))
                               .Returns(mockValidator.Object)
                               .Verifiable("Tthe validator should have been requested.");

            mockValidator.Setup(validator => validator.Validate(It.Is<object>(param => param == parameterValue)))
                         .Returns(new [] { new Error(ErrorCode.LengthIsInvalid, "someThing.Path", "Dummy description") })
                         .Verifiable("The validator should have been used to validate the parameter.");
            
            actionContext.ActionArguments.Add(parameterName, parameterValue);

            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
            request.Properties.Add(HttpPropertyKeys.DependencyScope, mockDependencyScope.Object);
            
            await messageValidator.OnActionExecutingAsync(actionContext, new CancellationToken());

            actionContext.Response.Should().NotBeNull("because an invalid response should contain response");
            actionContext.Response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "because a validation failure is a Bad Request");

            var errorSet = await actionContext.Response.Content.ReadAsAsync<ErrorSet>();
            errorSet.Should().NotBeNull("because an error set should be present for a failed validation");
            errorSet.Errors.Any().Should().BeTrue("because at least one error should be present for a failed validation");
        }

        /// <summary>
        ///   Verifies that failed requests are logged appopriately.
        /// </summary>
        ///         
        [Fact]
        public async Task LogIsWrittenForFailedValidations()
        {
            var parameterName        = "one";
            var parameterValue       = new object();
            var requestBody          = "REQUEST BODY CONTENT";
            var mockLogger           = new Mock<ILogger>();
            var mockValidator        = new Mock<IValidator>();
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var mockDependencyScope  = new Mock<IDependencyScope>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var messageValidator     = new ValidateMessageAttribute();

            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            mockDependencyScope.Setup(scope => scope.GetService(It.Is<Type>(type => type == typeof(ILogger))))
                               .Returns(mockLogger.Object)
                               .Verifiable("The logger should have been requested");

            mockDependencyScope.Setup(scope => scope.GetService(It.Is<Type>(type => type != typeof(ILogger))))
                               .Returns(mockValidator.Object)
                               .Verifiable("Tthe validator should have been requested.");

            mockValidator.Setup(validator => validator.Validate(It.Is<object>(param => param == parameterValue)))
                         .Returns(new[] { new Error(ErrorCode.LengthIsInvalid, "someThing.Path", "Dummy description") })
                         .Verifiable("The validator should have been used to validate the parameter.");

            mockLogger.Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                      .Returns(mockLogger.Object);

            actionContext.ActionArguments.Add(parameterName, parameterValue);

            request.Content = new StringContent(requestBody);
            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
            request.Properties.Add(HttpPropertyKeys.DependencyScope, mockDependencyScope.Object);

            await request.Content.ReadAsStringAsync();
            await messageValidator.OnActionExecutingAsync(actionContext, new CancellationToken());

            actionContext.Response.Should().NotBeNull("because an invalid response should contain response");
            actionContext.Response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "because a validation failure is a Bad Request");

            var errorSet = await actionContext.Response.Content.ReadAsAsync<ErrorSet>();
            errorSet.Should().NotBeNull("because an error set should be present for a failed validation");
            errorSet.Errors.Any().Should().BeTrue("because at least one error should be present for a failed validation");

            mockDependencyScope.VerifyAll();
            mockLogger.Verify(logger => logger.Information(It.IsAny<string>(), 
                                                           It.Is<HttpStatusCode>(code => code == HttpStatusCode.BadRequest),
                                                           It.Is<Uri>(uri => uri == request.RequestUri), 
                                                           It.Is<HttpRequestHeaders>(headers => headers == request.Headers), 
                                                           It.Is<string>(body => body == requestBody), 
                                                           It.Is<ErrorSet>(set => set == errorSet)), 
                                            Times.Once, "The message validation failure should have been logged");
        }

        /// <summary>
        ///   Verifies that the error response is sent appropriately.
        /// </summary>
        ///         
        [Fact]
        public async Task MemberPathIsTransformedForASingleObject()
        {
            var parameterName        ="one";
            var parameterValue       = new object();
            var mockValidator        = new Mock<IValidator>();
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var mockDependencyScope  = new Mock<IDependencyScope>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var messageValidator     = new ValidateMessageAttribute();

            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            mockDependencyScope.Setup(scope => scope.GetService(It.IsAny<Type>()))
                               .Returns(mockValidator.Object)
                               .Verifiable("Tthe validator should have been requested.");

            mockValidator.Setup(validator => validator.Validate(It.Is<object>(param => param == parameterValue)))
                         .Returns(new [] { new Error(ErrorCode.LengthIsInvalid, "someThing.Path", "Dummy description") })
                         .Verifiable("The validator should have been used to validate the parameter.");
            
            actionContext.ActionArguments.Add(parameterName, parameterValue);

            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
            request.Properties.Add(HttpPropertyKeys.DependencyScope, mockDependencyScope.Object);
            
            await messageValidator.OnActionExecutingAsync(actionContext, new CancellationToken());

            actionContext.Response.Should().NotBeNull("because an invalid response should contain response");
            actionContext.Response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "because a validation failure is a Bad Request");

            var errorSet = await actionContext.Response.Content.ReadAsAsync<ErrorSet>();
            errorSet.Should().NotBeNull("because an error set should be present for a failed validation");
            errorSet.Errors.Count().Should().Be(1, "because a single failure was returned by validation");

            errorSet.Errors.FirstOrDefault()?.MemberPath.Should().StartWith($"{ parameterName }::", "because the member path should have been rewritten");
        }

        /// <summary>
        ///   Verifies that the error response is sent appropriately.
        /// </summary>
        ///         
        [Fact]
        public async Task MemberPathIsTransformedForMultipleObjects()
        {
            var firstParameterName   ="one";
            var firstParameterValue  = new object();
            var secondParameterName  ="two";
            var secondParameterValue = new OrderFulfillmentMessage();            
            var mockValidator        = new Mock<IValidator>();
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var mockDependencyScope  = new Mock<IDependencyScope>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var messageValidator     = new ValidateMessageAttribute();

            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            mockDependencyScope.Setup(scope => scope.GetService(It.IsAny<Type>()))
                               .Returns(mockValidator.Object)
                               .Verifiable("Tthe validator should have been requested.");

            mockValidator.Setup(validator => validator.Validate(It.Is<object>(param => param == firstParameterValue)))
                         .Returns(new [] { new Error(ErrorCode.LengthIsInvalid, "someThing.Path", "Dummy description") })
                         .Verifiable("The validator should have been used to validate the parameter.");

            mockValidator.Setup(validator => validator.Validate(It.Is<object>(param => param == secondParameterValue)))
                         .Returns(new [] { new Error(ErrorCode.LengthIsInvalid, "order.OrderId", "Dummy description") })
                         .Verifiable("The validator should have been used to validate the parameter.");
            
            actionContext.ActionArguments.Add(firstParameterName, firstParameterValue);
            actionContext.ActionArguments.Add(secondParameterName, secondParameterValue);

            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
            request.Properties.Add(HttpPropertyKeys.DependencyScope, mockDependencyScope.Object);
            
            await messageValidator.OnActionExecutingAsync(actionContext, new CancellationToken());

            actionContext.Response.Should().NotBeNull("because an invalid response should contain response");
            actionContext.Response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "because a validation failure is a Bad Request");

            var errorSet = await actionContext.Response.Content.ReadAsAsync<ErrorSet>();
            errorSet.Should().NotBeNull("because an error set should be present for a failed validation");
            errorSet.Errors.Count().Should().Be(2, "because two failures were returned by validation");

            errorSet.Errors.Count(error => error.MemberPath.StartsWith($"{ firstParameterName }::")).Should().Be(1, "because a single error should have been mapped to the firstParameter");
            errorSet.Errors.Count(error => error.MemberPath.StartsWith($"{ secondParameterName }::")).Should().Be(1, "because a single error should have been mapped to the secondParameter");
        }
    }
}
