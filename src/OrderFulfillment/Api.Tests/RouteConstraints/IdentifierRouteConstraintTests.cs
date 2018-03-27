using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Dependencies;
using System.Web.Http.Hosting;
using System.Web.Http.Routing;
using FluentAssertions;
using OrderFulfillment.Api.RouteConstraints;
using OrderFulfillment.Core.Models.Errors;
using Moq;
using Serilog;
using Xunit;

namespace OrderFulfillment.Api.Tests.RouteConstraints
{
    /// <summary>
    ///   The suite of tests for verifying the <see cref="OrderFulfillment.Api.RouteConstraints.IdentifierRouteConstraint"/>
    ///   class.
    /// </summary>
    /// 
    public class IdentifierRouteConstraintTests
    {
        /// <summary>
        ///   Verifies that the error description used for the attribute is validated.
        /// </summary>
        /// 
        /// <param name="errorDescription">The error description to use for construction.</param>
        /// <param name="errorCode">The error code to use for construction.</param>
        /// 
        [Theory]
        [InlineData(null, "errorCode")]
        [InlineData(null, ErrorCode.PartnerIdentifierMalformed)]
        public void ConstructorValidatesErrorDescription(string errorDescription,
                                                         object errorCode)
        {
            Action actionUnderTest;

            if (errorCode is ErrorCode)
            {
                actionUnderTest = () => new IdentifierRouteConstraint((ErrorCode)errorCode, errorDescription, 1234, 3);
            }
            else
            {
                actionUnderTest = () => new IdentifierRouteConstraint(errorCode.ToString(), errorDescription, 1234, 3);
            }

            actionUnderTest.ShouldThrow<ArgumentNullException>("because the error description must have a value");
        }

        /// <summary>
        ///   Verifies that the parameterName used for the attribute is validated.
        /// </summary>
        ///         
        /// <param name="errorCode">The error code to use for construction.</param>
        /// 
        [Theory]
        [InlineData(null)]  
        public void ConstructorValidatesErrorCode(string errorCode)
        {
            Action actionUnderTest;
            actionUnderTest = () => new IdentifierRouteConstraint(errorCode, "some description", 1234, 3);

            actionUnderTest.ShouldThrow<ArgumentNullException>("because the error code must have a value");
        }

        /// <summary>
        ///   Verifies that identifiers with bad characers fail validation.
        /// </summary>
        /// 
        /// <param name="identifier">The identifier to validate.</param>
        ///         
        [Theory]
        [InlineData("ABC$123")]
        [InlineData("ABC-123%")]
        [InlineData("_aBc_-444!.thing")]
        [InlineData("ABC 123")]
        [InlineData("ABC%20123")]
        [InlineData("ABC+123")]
        [InlineData("ABC%20123.")]
        [InlineData("ABC%20123..")]
        [InlineData("ABC%20123...-Hello")]
        public async Task IdentifiersWithBadCharactersFailValidationAndAreLogged(string identifier)
        {
            var parameter            = "someIdentity";
            var errorCode            = "someError";
            var errorDescription     = "error description";
            var requestBody          = "REQUEST BODY CONTENT";
            var mockLogger           = new Mock<ILogger>();
            var mockDependencyScope  = new Mock<IDependencyScope>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var values               = new Dictionary<string, object> { { parameter, identifier } };
            var identityValidator    = new IdentifierRouteConstraint(errorCode, errorDescription);

            mockDependencyScope.Setup(scope => scope.GetService(It.Is<Type>(type => type == typeof(ILogger))))
                               .Returns(mockLogger.Object);

            mockLogger.Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                      .Returns(mockLogger.Object);

            request.Content = new StringContent(requestBody);
            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
            request.Properties.Add(HttpPropertyKeys.DependencyScope, mockDependencyScope.Object);
                        
            var response = default(HttpResponseMessage);

            Action actionUnderTest = () => 
            {
                try
                {
                    identityValidator.Match(request, Mock.Of<IHttpRoute>(), parameter, values, HttpRouteDirection.UriResolution);
                }
                
                catch (HttpResponseException ex)
                {
                    response = ex.Response;
                    throw;
                }               
             };

            actionUnderTest.ShouldThrow<HttpResponseException>("because the constraint was violated");
                         
            response.Should().NotBeNull("because the failed validation should set a response");
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "because a failed validation should be considered a BadRequest");
            response.Content.Should().NotBeNull("because there should have been an error set returned for context");
            
            var responseContent = await response.Content.ReadAsAsync<ErrorSet>();
            responseContent.Should().NotBeNull("because the content body should be an error set");
            responseContent.Errors.Should().NotBeNull("because the error set should contain errors");
            
            var errors = responseContent.Errors.ToList();
            errors.Count.Should().Be(1, "because a single error should be present");
            errors[0].Code.Should().Be(errorCode, "because the provided error code should be used for the failure response");
            errors[0].Description.Should().Be(errorDescription, "because the provided error description should be used for the failure response");

            mockLogger.Verify(logger => logger.Information(It.IsAny<string>(), 
                                                           It.Is<HttpStatusCode>(code => code == HttpStatusCode.BadRequest),
                                                           It.Is<string>(parameterName => parameterName == parameter),
                                                           It.Is<Uri>(uri => uri == request.RequestUri), 
                                                           It.Is<ErrorSet>(set => set == responseContent)), 
                                            Times.Once, "The route parameter validation failure should have been logged");
        }

        /// <summary>
        ///   Verifies that identifiers that are too long fail validation.
        /// </summary>
        /// 
        /// <param name="identifier">The identifier to validate.</param>
        ///  
        [Theory]
        [InlineData("ABC12377777777")]
        [InlineData("ABC123")]
        [InlineData("ABC1")]        
        public async Task IdentifiersThatAreTooLongFailValidationAndAreLogged(string identifier)
        {
            var maximumLength        = 3;
            var parameter            = "someId";
            var errorCode            = "anErrorLOL";
            var errorDescription     = "OMG error description";
            var requestBody          = "REQUEST BODY CONTENT";
            var mockLogger           = new Mock<ILogger>();
            var mockDependencyScope  = new Mock<IDependencyScope>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var values               = new Dictionary<string, object> { { parameter, identifier } };
            var identityValidator    = new IdentifierRouteConstraint(errorCode, errorDescription, maximumLength);
            
            mockDependencyScope.Setup(scope => scope.GetService(It.Is<Type>(type => type == typeof(ILogger))))
                               .Returns(mockLogger.Object);

            mockLogger.Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                      .Returns(mockLogger.Object);

            request.Content = new StringContent(requestBody);
            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
            request.Properties.Add(HttpPropertyKeys.DependencyScope, mockDependencyScope.Object);
                        
            var response = default(HttpResponseMessage);

            Action actionUnderTest = () => 
            {
                try
                {
                    identityValidator.Match(request, Mock.Of<IHttpRoute>(), parameter, values, HttpRouteDirection.UriResolution);
                }
                
                catch (HttpResponseException ex)
                {
                    response = ex.Response;
                    throw;
                }               
             };

            actionUnderTest.ShouldThrow<HttpResponseException>("because the constraint was violated");
            
            response.Should().NotBeNull("because the failed validation should set a response");
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "because a failed validation should be considered a BadRequest");
            response.Content.Should().NotBeNull("because there should have been an error set returned for context");
            
            var responseContent = await response.Content.ReadAsAsync<ErrorSet>();
            responseContent.Should().NotBeNull("because the content body should be an error set");
            responseContent.Errors.Should().NotBeNull("because the error set should contain errors");
            
            var errors = responseContent.Errors.ToList();
            errors.Count.Should().Be(1, "because a single error should be present");
            errors[0].Code.Should().Be(errorCode, "because the provided error code should be used for the failure response");
            errors[0].Description.Should().Be(errorDescription, "because the provided error description should be used for the failure response");

            mockLogger.Verify(logger => logger.Information(It.IsAny<string>(), 
                                                           It.Is<HttpStatusCode>(code => code == HttpStatusCode.BadRequest),
                                                           It.Is<string>(parameterName => parameterName == parameter),
                                                           It.Is<Uri>(uri => uri == request.RequestUri), 
                                                           It.Is<ErrorSet>(set => set == responseContent)), 
                                            Times.Once, "The route parameter validation failure should have been logged");
        }

        /// <summary>
        ///   Verifies that identifiers that are too short fail validation.
        /// </summary>
        /// 
        /// <param name="identifier">The identifier to validate.</param>
        ///  
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("2")]
        [InlineData("_B")]        
        [InlineData(".B")]        
        public async Task IdentifierThatAreToShortFailValidationAndAreLogged(string identifier)
        {
            var minimumLength        = 3;
            var parameter            = "someId";
            var errorCode            = "anErrorLOL";
            var errorDescription     = "OMG error description";
            var requestBody          = "REQUEST BODY CONTENT";
            var mockLogger           = new Mock<ILogger>();
            var mockDependencyScope  = new Mock<IDependencyScope>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var values               = new Dictionary<string, object> { { parameter, identifier } };
            var identityValidator    = new IdentifierRouteConstraint(errorCode, errorDescription, minimumLength: minimumLength);
            
            mockDependencyScope.Setup(scope => scope.GetService(It.Is<Type>(type => type == typeof(ILogger))))
                               .Returns(mockLogger.Object);

            mockLogger.Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                      .Returns(mockLogger.Object);

            request.Content = new StringContent(requestBody);
            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
            request.Properties.Add(HttpPropertyKeys.DependencyScope, mockDependencyScope.Object);
                        
            var response = default(HttpResponseMessage);

            Action actionUnderTest = () => 
            {
                try
                {
                    identityValidator.Match(request, Mock.Of<IHttpRoute>(), parameter, values, HttpRouteDirection.UriResolution);
                }
                
                catch (HttpResponseException ex)
                {
                    response = ex.Response;
                    throw;
                }               
             };

            actionUnderTest.ShouldThrow<HttpResponseException>("because the constraint was violated");
            
            response.Should().NotBeNull("because the failed validation should set a response");
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "because a failed validation should be considered a BadRequest");
            response.Content.Should().NotBeNull("because there should have been an error set returned for context");
            
            var responseContent = await response.Content.ReadAsAsync<ErrorSet>();
            responseContent.Should().NotBeNull("because the content body should be an error set");
            responseContent.Errors.Should().NotBeNull("because the error set should contain errors");
            
            var errors = responseContent.Errors.ToList();
            errors.Count.Should().Be(1, "because a single error should be present");
            errors[0].Code.Should().Be(errorCode, "because the provided error code should be used for the failure response");
            errors[0].Description.Should().Be(errorDescription, "because the provided error description should be used for the failure response");

            mockLogger.Verify(logger => logger.Information(It.IsAny<string>(), 
                                                           It.Is<HttpStatusCode>(code => code == HttpStatusCode.BadRequest),
                                                           It.Is<string>(parameterName => parameterName == parameter),
                                                           It.Is<Uri>(uri => uri == request.RequestUri), 
                                                           It.Is<ErrorSet>(set => set == responseContent)), 
                                            Times.Once, "The route parameter validation failure should have been logged");
        }

        /// <summary>
        ///   Verifies that legal identifiers pass validation.
        /// </summary>
        /// 
        /// <param name="identifier">The identifier to validate.</param>
        ///         
        [Theory]
        [InlineData("ABC123")]
        [InlineData("ABC-123")]
        [InlineData("_Abc-123")]
        [InlineData("ABC.123")]
        [InlineData(".-_AB12-c4.thing")]
        [InlineData(".-_AB12-c4.thing.")]
        [InlineData(".-_AB12-c4.thing..")]
        [InlineData(".-_AB12-c4.thing..._-B")]
        [InlineData("AAAA")]
        [InlineData("12344")]
        public void LegalIdentifiersPassValidation(string identifier)
        {
            var parameter            = "someIdentity";
            var errorCode            = "someError";
            var errorDescription     = "error description";
            var requestBody          = "REQUEST BODY CONTENT";
            var mockLogger           = new Mock<ILogger>();
            var mockDependencyScope  = new Mock<IDependencyScope>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var values               = new Dictionary<string, object> { { parameter, identifier } };
            var identityValidator    = new IdentifierRouteConstraint(errorCode, errorDescription);
            
            mockDependencyScope.Setup(scope => scope.GetService(It.Is<Type>(type => type == typeof(ILogger))))
                               .Returns(mockLogger.Object);

            mockLogger.Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                      .Returns(mockLogger.Object);

            request.Content = new StringContent(requestBody);
            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
            request.Properties.Add(HttpPropertyKeys.DependencyScope, mockDependencyScope.Object);

            Action actionUnderTest = () => identityValidator.Match(request, Mock.Of<IHttpRoute>(), parameter, values, HttpRouteDirection.UriResolution);
            actionUnderTest.ShouldNotThrow("because the constraint was not violated");
        }

        /// <summary>
        ///   Verifies that there is no validation if the identifier is not present.
        /// </summary>
        ///       
        [Fact]
        public void ValidationIsNotPerformedWhenParameterIsNotPresent()
        {
            var parameter         = "someIdentity";
            var errorCode         = "someError";
            var errorDescription  = "error description";
            var httpConfiguration = new HttpConfiguration();
            var routeData         = new HttpRouteData(new HttpRoute());
            var request           = new HttpRequestMessage();
            var values            = new Dictionary<string, object> { { "Not My Parameter", "Not My Value" } };
            var identityValidator = new IdentifierRouteConstraint(errorCode, errorDescription);
            
            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);

            Action actionUnderTest = () => identityValidator.Match(request, Mock.Of<IHttpRoute>(), parameter, values, HttpRouteDirection.UriResolution);
            actionUnderTest.ShouldNotThrow("because the parameter to check the constraint against was not present");
        }
    }
}
