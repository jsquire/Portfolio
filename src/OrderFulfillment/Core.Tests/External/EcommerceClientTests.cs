using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using OrderFulfillment.Core.Configuration;
using OrderFulfillment.Core.Extensions;
using OrderFulfillment.Core.External;
using OrderFulfillment.Core.Infrastructure;
using OrderFulfillment.Core.Models.Operations;
using Newtonsoft.Json;
using Xunit;

namespace OrderFulfillment.Core.Tests.External
{
    /// <summary>
    ///   The suite of tests for the <see cref="ECommerceClient" /> 
    ///   class.
    /// </summary>
    /// 
    public class EcommerceClientTests
    {

        /// <summary>
        ///   Verifies functionality of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesTheConfiguration()
        {
            using (var response = new HttpResponseMessage())
            {
                Action actionUnderTest = () => new TestECommerceClient(null, response);
                actionUnderTest.ShouldThrow<ArgumentNullException>("because the configuration is required");
            }
        }

        /// <summary>
        ///   Verifies functionality of the constructor.
        /// </summary>
        /// 
        [Fact]
        public async Task GetOrderDetailsHonorsTheRequestTimeout()
        {            
            using (var response = new HttpResponseMessage())
            {
                var config = new EcommerceClientConfiguration 
                { 
                    RequestProtocol               = "https", 
                    ServiceHostAddress            = "google.com", 
                    GetOrderUrlTemplate           = "/orders/{order}",
                    RequestTimeoutSeconds         = 60,
                    ConnectionLeaseTimeoutSeconds = 300
                };

                var client              = new TestECommerceClient(config, response);
                var expectedCorrelation = "Hello";

                await client.GetOrderDetailsAsync("ABX", expectedCorrelation);

                client.RequestTimeout.Should().Be(TimeSpan.FromSeconds(config.RequestTimeoutSeconds), "because the configured timeout should be used");
            }

        }

        /// <summary>
        ///   Verifies functionality of the constructor.
        /// </summary>
        /// 
        [Fact]
        public async Task GetOrderDetailsAddsACorrelationId()
        {            
            using (var response = new HttpResponseMessage())
            {
                var config = new EcommerceClientConfiguration 
                { 
                    RequestProtocol               = "https", 
                    ServiceHostAddress            = "google.com", 
                    GetOrderUrlTemplate           = "/orders/{order}",
                    RequestTimeoutSeconds         = 60,
                    ConnectionLeaseTimeoutSeconds = 300
                };

                var client              = new TestECommerceClient(config, response);
                var expectedCorrelation = "Hello";

                await client.GetOrderDetailsAsync("ABX", expectedCorrelation);

                client.Request.Headers.TryGetValues(HttpHeaders.CorrelationId, out var correlationValues).Should().BeTrue("because there should be a correlation header");
                client.Request.Headers.TryGetValues(HttpHeaders.DefaultApplicationInsightsOperationId, out var operationValues).Should().BeTrue("because there should be an operation header");
                correlationValues.Should().HaveCount(1, "because there should be a single correlation header");
                operationValues.Should().HaveCount(1, "because there should be a single operation header");
                correlationValues.First().Should().Be(expectedCorrelation, "because the correlation id should have been set");
                operationValues.First().Should().Be(expectedCorrelation, "because the correlation id should have been set as the operation id");
            }
        }

        /// <summary>
        ///   Verifies functionality of the constructor.
        /// </summary>
        /// 
        [Fact]
        public async Task GetOrderDetailsDoesNotAddACorrelationWhenNotPassed()
        {            
            using (var response = new HttpResponseMessage())
            {
                var config = new EcommerceClientConfiguration 
                { 
                    RequestProtocol               = "https", 
                    ServiceHostAddress            = "google.com", 
                    GetOrderUrlTemplate           = "/orders/{order}",
                    RequestTimeoutSeconds         = 60,
                    ConnectionLeaseTimeoutSeconds = 300
                };

                var client = new TestECommerceClient(config, response);

                await client.GetOrderDetailsAsync("ABX", null);

                client.Request.Headers.TryGetValues(HttpHeaders.CorrelationId, out var correlationValues).Should().BeFalse("because there should not be a correlation header");
                client.Request.Headers.TryGetValues(HttpHeaders.DefaultApplicationInsightsOperationId, out var operationValues).Should().BeFalse("because there should not be an operation header");
            }
        }

        /// <summary>
        ///   Verifies functionality of the constructor.
        /// </summary>
        /// 
        [Fact]
        public async Task GetOrderDetailsAddsStaticHeaders()
        {            
            using (var response = new HttpResponseMessage())
            {
                var headers = new Dictionary<string, string>
                {
                    { "First",  "One" },
                    { "Second", "Two" }
                };

                var config = new EcommerceClientConfiguration 
                { 
                    RequestProtocol               = "https", 
                    ServiceHostAddress            = "google.com", 
                    GetOrderUrlTemplate           = "/orders/{order}",
                    RequestTimeoutSeconds         = 60,
                    ConnectionLeaseTimeoutSeconds = 300,
                    StaticHeadersJson             = JsonConvert.SerializeObject(headers)
                };

                var client = new TestECommerceClient(config, response);

                await client.GetOrderDetailsAsync("ABX", null);

                foreach (var pair in headers)
                {
                    client.Request.Headers.TryGetValues(pair.Key, out var values).Should().BeTrue("because there should be a {0} header", pair.Key);
                    values.Should().HaveCount(1, "because there should be a single {0} header", pair.Key);
                    values.First().Should().Be(pair.Value, "because the {0} header should have the corresponding value", pair.Key);
                }
            }
        }

        /// <summary>
        ///   Verifies functionality of the constructor.
        /// </summary>
        /// 
        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("{}")]
        public void GetOrderDetailsHandlesNoStaticHeaders(string headerJson)
        {            
            using (var response = new HttpResponseMessage())
            {
                var config = new EcommerceClientConfiguration 
                { 
                    RequestProtocol               = "https", 
                    ServiceHostAddress            = "google.com", 
                    GetOrderUrlTemplate           = "/orders/{order}",
                    RequestTimeoutSeconds         = 60,
                    ConnectionLeaseTimeoutSeconds = 300,
                    StaticHeadersJson             = headerJson
                };

                var client = new TestECommerceClient(config, response);                

                Action actionUnderTest = () => client.GetOrderDetailsAsync("ABX", null).GetAwaiter().GetResult();
                actionUnderTest.ShouldNotThrow("because missing static header configuration should be valid");
            }
        }

        /// <summary>
        ///   Verifies functionality of the constructor.
        /// </summary>
        /// 
        [Fact]
        public async Task GetOrderRequestsTheCorrectOrder()
        {            
            using (var response = new HttpResponseMessage(HttpStatusCode.OK))
            {                
                var config = new EcommerceClientConfiguration 
                { 
                    RequestProtocol               = "https", 
                    ServiceHostAddress            = "google.com", 
                    GetOrderUrlTemplate           = "/orders/{order}",
                    RequestTimeoutSeconds         = 60,
                    ConnectionLeaseTimeoutSeconds = 300
                };

                var order  = "ABX";
                var client = new TestECommerceClient(config, response);

                await client.GetOrderDetailsAsync(order, null);

                client.Request.RequestUri.OriginalString.Should().Be($"/orders/{order}", "because the correct request url should have been generated");       
            }
        }

        /// <summary>
        ///   Verifies functionality of the constructor.
        /// </summary>
        /// 
        [Fact]
        public async Task GetOrderReturnsTheCorrectResultForSuccessfulRequests()
        {            
            var responseStatus = HttpStatusCode.OK;

            using (var response = new HttpResponseMessage(responseStatus))
            {                
                var config = new EcommerceClientConfiguration 
                { 
                    RequestProtocol               = "https", 
                    ServiceHostAddress            = "google.com", 
                    GetOrderUrlTemplate           = "/orders/{order}",
                    RequestTimeoutSeconds         = 60,
                    ConnectionLeaseTimeoutSeconds = 300
                };

                var client          = new TestECommerceClient(config, response);
                var expectedContent = "Hello";

                response.Content = new StringContent(expectedContent);

                var result = await client.GetOrderDetailsAsync("ABX", null);

                result.Should().NotBeNull("because the result should have been returned");
                result.Outcome.Should().Be(Outcome.Success, "because the request was successful");
                result.Reason.Should().Be(responseStatus.ToString(), "because the status code should be used as the reason");
                result.Recoverable.Should().Be(Recoverability.Final, "because the request was successful and final");
                result.Payload.Should().Be(expectedContent, "because the payload should have been set form response content");                
            }
        }

        /// <summary>
        ///   Verifies functionality of the constructor.
        /// </summary>
        /// 
        [Fact]
        public async Task GetOrderReturnsTheCorrectResultForRecoverableFailures()
        {            
            var responseStatus = HttpStatusCodeExtensions.RateLimitExceeded;

            using (var response = new HttpResponseMessage(responseStatus))
            {                
                var config = new EcommerceClientConfiguration 
                { 
                    RequestProtocol               = "https", 
                    ServiceHostAddress            = "google.com", 
                    GetOrderUrlTemplate           = "/orders/{order}",
                    RequestTimeoutSeconds         = 60,
                    ConnectionLeaseTimeoutSeconds = 300
                };

                var client = new TestECommerceClient(config, response);

                var result = await client.GetOrderDetailsAsync("ABX", null);

                result.Should().NotBeNull("because the result should have been returned");
                result.Outcome.Should().Be(Outcome.Failure, "because the request failed");
                result.Reason.Should().Be(responseStatus.ToString(), "because the status code should be used as the reason");
                result.Recoverable.Should().Be(Recoverability.Retriable, "because the request was retriable");
                result.Payload.Should().BeNull("because there was no content from the failed request");                
            }
        }

        /// <summary>
        ///   Verifies functionality of the constructor.
        /// </summary>
        /// 
        [Fact]
        public async Task GetOrderReturnsTheCorrectResultForUnrecoverableFailures()
        {        
            var responseStatus = HttpStatusCode.BadRequest;

            using (var response = new HttpResponseMessage(responseStatus))
            {                
                var config = new EcommerceClientConfiguration 
                { 
                    RequestProtocol               = "https", 
                    ServiceHostAddress            = "google.com", 
                    GetOrderUrlTemplate           = "/orders/{order}",
                    RequestTimeoutSeconds         = 60,
                    ConnectionLeaseTimeoutSeconds = 300
                };

                var client = new TestECommerceClient(config, response);

                var result = await client.GetOrderDetailsAsync("ABX", null);

                result.Should().NotBeNull("because the result should have been returned");
                result.Outcome.Should().Be(Outcome.Failure, "because the request failed");
                result.Reason.Should().Be(responseStatus.ToString(), "because the status code should be used as the reason");
                result.Recoverable.Should().Be(Recoverability.Final, "because the response was not a retriable code");
                result.Payload.Should().BeNull("because there was no content from the failed request");                
            }
        }

        #region Nested Classes

            private class TestECommerceClient : EcommerceClient
            {
                private readonly HttpResponseMessage response;   
                
                public HttpRequestMessage Request;
                public TimeSpan RequestTimeout;

                public TestECommerceClient(EcommerceClientConfiguration config,
                                           HttpResponseMessage          response = null) : base(config, null)
                {   
                    this.response = response;
                }

                protected override Task<HttpResponseMessage> SendRequestAsync(HttpRequestMessage request, TimeSpan timeout)
                {
                    this.Request = request;
                    this.RequestTimeout = timeout;

                    return Task.FromResult(this.response);
                }

                protected override HttpClient CreateHttpClient(string requestProtocol, string hostServiceAddress, string clientCertificateThumbprint, int requestTimeoutSeconds, int connectionLeaseTimeoutSeconds) => 
                    new HttpClient();
                
            }

        #endregion
    }
}
