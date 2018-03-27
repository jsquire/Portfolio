using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using System.Web.Http.Routing;
using FluentAssertions;
using OrderFulfillment.Api.Extensions;
using OrderFulfillment.Api.Filters;
using OrderFulfillment.Core.Configuration;
using OrderFulfillment.Core.Infrastructure;
using Moq;
using Serilog;
using Xunit;


namespace OrderFulfillment.Api.Tests.Filters
{
    /// <summary>
    ///   The suite of tests for the <see cref="GlobalExceptionFilter" />
    ///   class.
    /// </summary> 
    /// 
    public class GlobalExceptionFilterTests
    {
        /// <summary>
        ///   Verifies that the constructor validates its arguments.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesConfiguration()
        {
            Action actionUnderTest = () => new GlobalExceptionFilter(null, Mock.Of<ILogger>());

            actionUnderTest.ShouldThrow<ArgumentNullException>("because a null configuration should not be accepted")
                           .And.ParamName.Should().Be("configuration", "because the null configuration should have caused the failure");
        }

        /// <summary>
        ///   Verifies that the constructor validates its arguments.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesLogger()
        {
            Action actionUnderTest = () => new GlobalExceptionFilter(new ErrorHandlingConfiguration(), null);

            actionUnderTest.ShouldThrow<ArgumentNullException>("because a null logger should not be accepted")
                           .And.ParamName.Should().Be("log", "because the null logger should have caused the failure");
        }

        /// <summary>
        ///   Verifies behavior of the OnException method.
        /// </summary>
        ///
        [Fact]
        public async Task OnExceptionWritesAnErrorLogEntry()
        {
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var mockLogger           = new Mock<ILogger>();
            var exception            = new InvalidOperationException("One cannot simply walk into Mordor");
            var errorConfiguration   = new ErrorHandlingConfiguration();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage(HttpMethod.Get, "http://api.someservice.com/collection/thing");
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var errorContext         = new HttpActionExecutedContext(actionContext, exception);
                        
            mockLogger.Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(mockLogger.Object);
            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
            request.Headers.TryAddWithoutValidation(HttpHeaders.CorrelationId, "Not-A-Real-Value");
                        
            var exceptionFilter = new GlobalExceptionFilter(errorConfiguration, mockLogger.Object);
            await exceptionFilter.OnExceptionAsync(errorContext, CancellationToken.None);

            mockLogger.Verify(logger => logger.Error(It.Is<Exception>(ex => ex == exception), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once, "An error log entry should have been written");
        }

        /// <summary>
        ///   Verifies behavior of the OnException method.
        /// </summary>
        ///
        [Fact]
        public async Task OnExceptionSetsTheResultWithoutExceptionContentWhenDisabled()
        {
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var mockLogger           = new Mock<ILogger>();
            var exception            = new InvalidOperationException("One cannot simply walk into Mordor");
            var errorConfiguration   = new ErrorHandlingConfiguration { ExceptionDetailsEnabled = false };
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage(HttpMethod.Get, "http://api.someservice.com/collection/thing");
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var errorContext         = new HttpActionExecutedContext(actionContext, exception);
                        
            mockLogger.Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(mockLogger.Object);
            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
            request.Headers.TryAddWithoutValidation(HttpHeaders.CorrelationId, "Not-A-Real-Value");
                        
            var exceptionFilter = new GlobalExceptionFilter(errorConfiguration, mockLogger.Object);
            await exceptionFilter.OnExceptionAsync(errorContext, CancellationToken.None);

            errorContext.Response.Should().NotBeNull("because the response should have been set");
            errorContext.Response.StatusCode.Should().Be(HttpStatusCode.InternalServerError, "because an exception result should be set");
            errorContext.Response.ReasonPhrase.Should().NotContain(exception.ToString(), "because exception details should not appear as the reason");
            errorContext.Response.Content.Should().BeNull("because no body content should have been set");            
        }

        /// <summary>
        ///   Verifies behavior of the OnException method.
        /// </summary>
        ///ommon.Infrastructure.HttpHeaders
        [Fact]
        public async Task OnExceptionSetsTheResultWithExceptionContentWhenEnabled()
        {
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var mockLogger           = new Mock<ILogger>();
            var exception            = new InvalidOperationException("One cannot simply walk into Mordor");
            var errorConfiguration   = new ErrorHandlingConfiguration { ExceptionDetailsEnabled = true };
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage(HttpMethod.Get, "http://api.someservice.com/collection/thing");
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var errorContext         = new HttpActionExecutedContext(actionContext, exception);
                        
            mockLogger.Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(mockLogger.Object);
            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
            request.Headers.TryAddWithoutValidation(HttpHeaders.CorrelationId, "Not-A-Real-Value");
                        
            var exceptionFilter = new GlobalExceptionFilter(errorConfiguration, mockLogger.Object);
            await exceptionFilter.OnExceptionAsync(errorContext, CancellationToken.None);

            errorContext.Response.Should().NotBeNull("because the response should have been set");
            errorContext.Response.StatusCode.Should().Be(HttpStatusCode.InternalServerError, "because an exception result should be set");
            errorContext.Response.ReasonPhrase.Should().NotContain(exception.ToString(), "because exception details should not appear as the reason");
            errorContext.Response.Content.Should().NotBeNull("because body content should have been set");
            
            var exceptionContent = await errorContext.Response.Content.ReadAsAsync<Exception>();
            exceptionContent.Message.Should().Be(exception.Message, "because the exception should have been used as the content");
            exceptionContent.StackTrace.Should().Be(exception.StackTrace, "because the exception should have been used as the content");
        }

        /// <summary>
        ///   Verifies behavior of the OnException method.
        /// </summary>
        ///
        [Fact]
        public async Task OnExceptionSetsTheExceptionHeaderWhenEnabled()
        {
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var mockLogger           = new Mock<ILogger>();
            var exception            = new InvalidOperationException("One cannot simply walk into Mordor");
            var errorConfiguration   = new ErrorHandlingConfiguration { ExceptionDetailsEnabled = true };
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage(HttpMethod.Get, "http://api.someservice.com/collection/thing");
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var errorContext         = new HttpActionExecutedContext(actionContext, exception);
                        
            mockLogger.Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(mockLogger.Object);
            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
            request.Headers.TryAddWithoutValidation(HttpHeaders.CorrelationId, "Not-A-Real-Value");
                        
            var exceptionFilter = new GlobalExceptionFilter(errorConfiguration, mockLogger.Object);
            await exceptionFilter.OnExceptionAsync(errorContext, CancellationToken.None);

            errorContext.Response.Should().NotBeNull("because the response should have been set");
            
            IEnumerable<string> headers;
            errorContext.Response.Headers.TryGetValues(HttpHeaders.ExceptionDetails, out headers).Should().BeTrue("because the exception details header should have been set");
            headers.Count().Should().Be(1, "because a single exception detail header should exist");
            headers.First().Should().Be(exception.ToString(), "because the exception header should contain the relevant exception details");
        }

        /// <summary>
        ///   Verifies behavior of the OnException method.
        /// </summary>
        ///
        [Fact]
        public async Task OnExceptionDoesNotSetTheExceptionHeaderWhenDisaabled()
        {
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var mockLogger           = new Mock<ILogger>();
            var exception            = new InvalidOperationException("One cannot simply walk into Mordor");
            var errorConfiguration   = new ErrorHandlingConfiguration { ExceptionDetailsEnabled = false };
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage(HttpMethod.Get, "http://api.someservice.com/collection/thing");
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var errorContext         = new HttpActionExecutedContext(actionContext, exception);
                        
            mockLogger.Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(mockLogger.Object);
            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
            request.Headers.TryAddWithoutValidation(HttpHeaders.CorrelationId, "Not-A-Real-Value");
                        
            var exceptionFilter = new GlobalExceptionFilter(errorConfiguration, mockLogger.Object);
            await exceptionFilter.OnExceptionAsync(errorContext, CancellationToken.None);

            errorContext.Response.Should().NotBeNull("because the response should have been set");
            
            IEnumerable<string> headers;
            errorContext.Response.Headers.TryGetValues(HttpHeaders.ExceptionDetails, out headers).Should().BeFalse("because the exception details header should not have been set");
        }

        /// <summary>
        ///   Verifies behavior of the OnException method.
        /// </summary>
        ///
        [Fact]
        public async Task OnExceptionSetsTheCorrelationHeader()
        {
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var mockLogger           = new Mock<ILogger>();
            var exception            = new InvalidOperationException("One cannot simply walk into Mordor");
            var errorConfiguration   = new ErrorHandlingConfiguration { ExceptionDetailsEnabled = true };
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage(HttpMethod.Get, "http://api.someservice.com/collection/thing");
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var errorContext         = new HttpActionExecutedContext(actionContext, exception);
                        
            mockLogger.Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(mockLogger.Object);
            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
            request.Headers.TryAddWithoutValidation(HttpHeaders.CorrelationId, "Not-A-Real-Value");
                        
            var exceptionFilter = new GlobalExceptionFilter(errorConfiguration, mockLogger.Object);
            await exceptionFilter.OnExceptionAsync(errorContext, CancellationToken.None);

            errorContext.Response.Should().NotBeNull("because the response should have been set");
            
            IEnumerable<string> headers;
            errorContext.Response.Headers.TryGetValues(HttpHeaders.CorrelationId, out headers).Should().BeTrue("because the correlation header should have been set");
            headers.Count().Should().Be(1, "because a single correlation header should exist");
            headers.First().Should().Be(request.GetOrderFulfillmentCorrelationId(), "because the correlation header should contain the relevant correlation identifier");
        }

        /// <summary>
        ///   Verifies that exception details appearing in the header are formatted correctly.
        /// </summary>
        ///
        [Fact]
        public async Task ExceptionHeaderDoesNotContainUnsafeCharacters()
        {
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var mockLogger           = new Mock<ILogger>();
            var exception            = new InvalidOperationException("One cannot simply walk into Mordor");
            var errorConfiguration   = new ErrorHandlingConfiguration { ExceptionDetailsEnabled = true };
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage(HttpMethod.Get, "http://api.someservice.com/collection/thing");
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = "generic" };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var errorContext         = new HttpActionExecutedContext(actionContext, exception);
                        
            mockLogger.Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(mockLogger.Object);
            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");

            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
            request.Headers.TryAddWithoutValidation(HttpHeaders.CorrelationId, "Not-A-Real-Value");
                        
            var exceptionFilter = new GlobalExceptionFilter(errorConfiguration, mockLogger.Object);
            await exceptionFilter.OnExceptionAsync(errorContext, CancellationToken.None);

            errorContext.Response.Should().NotBeNull("because the response should have been set");
            
            IEnumerable<string> headers;
            errorContext.Response.Headers.TryGetValues(HttpHeaders.ExceptionDetails, out headers).Should().BeTrue("because the exception details header should have been set");
            headers.Count().Should().Be(1, "because a single exception detail header should exist");
            headers.First().Should().NotContain(Environment.NewLine, "because new lines are not allowed in header values");
        }
    }
}
