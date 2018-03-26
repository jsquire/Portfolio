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
using OrderFulfillment.Core.Models.External.OrderProduction;
using OrderFulfillment.Core.Models.Operations;
using Newtonsoft.Json;
using Xunit;

namespace OrderFulfillment.Core.Tests.External
{
    /// <summary>
    ///   The suite of unit tests for the <see cref="OrderProductionClient" /> class.
    /// </summary>
    /// 
    public class orderProductionClientTests
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
                Action actionUnderTest = () => new TestOrderProductionClient(null, response);
                actionUnderTest.ShouldThrow<ArgumentNullException>("because the configuration is required");
            }
        }

        /// <summary>
        ///   Verifies functionality of the constructor.
        /// </summary>
        /// 
        [Fact]
        public async Task SubmitOrderForProductionHonorsTheRequestTimeout()
        {            
            using (var response = new HttpResponseMessage())
            {
                var config = new OrderProductionClientConfiguration 
                { 
                    RequestProtocol               = "https", 
                    ServiceHostAddress            = "google.com", 
                    CreateOrderUrlTemplate        = "/partners/{partner}",
                    RequestTimeoutSeconds         = 60,
                    ConnectionLeaseTimeoutSeconds = 300
                };

                var client              = new TestOrderProductionClient(config, response);
                var expectedCorrelation = "Hello";

                await client.SubmitOrderForProductionAsync(new CreateOrderMessage { Identity = new OrderIdentity { PartnerCode = "ABX" }}, expectedCorrelation);

                client.RequestTimeout.Should().Be(TimeSpan.FromSeconds(config.RequestTimeoutSeconds), "because the configured timeout should be used");
            }

        }

        /// <summary>
        ///   Verifies functionality of the constructor.
        /// </summary>
        /// 
        [Fact]
        public async Task SubmitOrderForProductionAddsACorrelationId()
        {            
            using (var response = new HttpResponseMessage())
            {
                var config = new OrderProductionClientConfiguration 
                { 
                    RequestProtocol               = "https", 
                    ServiceHostAddress            = "google.com", 
                    CreateOrderUrlTemplate        = "/partners/{partner}",
                    RequestTimeoutSeconds         = 60,
                    ConnectionLeaseTimeoutSeconds = 300
                };

                var client              = new TestOrderProductionClient(config, response);
                var expectedCorrelation = "Hello";

                await client.SubmitOrderForProductionAsync(new CreateOrderMessage { Identity = new OrderIdentity { PartnerCode = "ABX" }}, expectedCorrelation);

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
        public async Task SubmitOrderForProductionDoesNotAddACorrelationWhenNotPassed()
        {            
            using (var response = new HttpResponseMessage())
            {
                 var config = new OrderProductionClientConfiguration 
                { 
                    RequestProtocol               = "https", 
                    ServiceHostAddress            = "google.com", 
                    CreateOrderUrlTemplate        = "/partners/{partner}",
                    RequestTimeoutSeconds         = 60,
                    ConnectionLeaseTimeoutSeconds = 300
                };

                var client = new TestOrderProductionClient(config, response);

                await client.SubmitOrderForProductionAsync(new CreateOrderMessage { Identity = new OrderIdentity { PartnerCode = "ABX" }}, null);

                client.Request.Headers.TryGetValues(HttpHeaders.CorrelationId, out var correlationValues).Should().BeFalse("because there should not be a correlation header");
                client.Request.Headers.TryGetValues(HttpHeaders.DefaultApplicationInsightsOperationId, out var operationValues).Should().BeFalse("because there should not be an operation header");
            }
        }

        /// <summary>
        ///   Verifies functionality of the constructor.
        /// </summary>
        /// 
        [Fact]
        public async Task SubmitOrderForProductionAddsStaticHeaders()
        {            
            using (var response = new HttpResponseMessage())
            {
                var headers = new Dictionary<string, string>
                {
                    { "First",  "One" },
                    { "Second", "Two" }
                };

                var config = new OrderProductionClientConfiguration 
                { 
                    RequestProtocol               = "https", 
                    ServiceHostAddress            = "google.com", 
                    CreateOrderUrlTemplate        = "/partners/{partner}",
                    RequestTimeoutSeconds         = 60,
                    ConnectionLeaseTimeoutSeconds = 300,
                    StaticHeadersJson             = JsonConvert.SerializeObject(headers)
                };

                var client = new TestOrderProductionClient(config, response);

                await client.SubmitOrderForProductionAsync(new CreateOrderMessage { Identity = new OrderIdentity { PartnerCode = "ABX" }}, null);

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
        public void SubmitOrderForProductionHandlesNoStaticHeaders(string headerJson)
        {            
            using (var response = new HttpResponseMessage())
            {
                var config = new OrderProductionClientConfiguration 
                { 
                    RequestProtocol               = "https", 
                    ServiceHostAddress            = "google.com", 
                    CreateOrderUrlTemplate        = "/partners/{partner}",
                    RequestTimeoutSeconds         = 60,
                    ConnectionLeaseTimeoutSeconds = 300,
                    StaticHeadersJson             = headerJson
                };

                var client = new TestOrderProductionClient(config, response);

                Action actionUnderTest = () => client.SubmitOrderForProductionAsync(new CreateOrderMessage { Identity = new OrderIdentity { PartnerCode = "ABX" }}, null).GetAwaiter().GetResult();
                actionUnderTest.ShouldNotThrow("because missing static header configuration should be valid");
            }
        }

        /// <summary>
        ///   Verifies functionality of the constructor.
        /// </summary>
        /// 
        [Fact]
        public async Task SubmitOrderForProductionSubmitsForTheCorrectPartner()
        {            
            using (var response = new HttpResponseMessage())
            {
                 var config = new OrderProductionClientConfiguration 
                { 
                    RequestProtocol               = "https", 
                    ServiceHostAddress            = "google.com", 
                    CreateOrderUrlTemplate        = "/partners/{partner}/orders",
                    RequestTimeoutSeconds         = 60,
                    ConnectionLeaseTimeoutSeconds = 300
                };

                var partner = "ABC";
                var client  = new TestOrderProductionClient(config, response);

                await client.SubmitOrderForProductionAsync(new CreateOrderMessage { Identity = new OrderIdentity { PartnerCode = partner }}, null);

                client.Request.RequestUri.OriginalString.Should().Be($"/partners/{ partner }/orders", "becaues the correct request url should have been generated");
            }
        }

        /// <summary>
        ///   Verifies functionality of the constructor.
        /// </summary>
        /// 
        [Fact]
        public async Task SubmitOrderForProductionProvidesTheCorrectPayload()
        {            
            using (var response = new HttpResponseMessage())
            {
                 var config = new OrderProductionClientConfiguration 
                { 
                    RequestProtocol               = "https", 
                    ServiceHostAddress            = "google.com", 
                    CreateOrderUrlTemplate        = "/partners/{partner}/orders",
                    RequestTimeoutSeconds         = 60,
                    ConnectionLeaseTimeoutSeconds = 300
                };

                var partner = "ABC";
                var order   = new CreateOrderMessage { Identity = new OrderIdentity { PartnerCode = partner }, TransactionId = "1234" };
                var client  = new TestOrderProductionClient(config, response);

                await client.SubmitOrderForProductionAsync(order, null);

                client.Request.Content.Headers.ContentType.MediaType.Should().Be(MimeTypes.Json, "becuse the order should have been sent in the correct format");
                client.RequestContent.Should().Be(JsonConvert.SerializeObject(order), "because the correct order should have been sent");
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
                 var config = new OrderProductionClientConfiguration 
                { 
                    RequestProtocol               = "https", 
                    ServiceHostAddress            = "google.com", 
                    CreateOrderUrlTemplate        = "/partners/{partner}",
                    RequestTimeoutSeconds         = 60,
                    ConnectionLeaseTimeoutSeconds = 300
                };

                var client          = new TestOrderProductionClient(config, response);
                var expectedContent = "Hello";

                response.Content = new StringContent(expectedContent);

                var result = await client.SubmitOrderForProductionAsync(new CreateOrderMessage { Identity = new OrderIdentity { PartnerCode = "ABX" }}, null);

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
                 var config = new OrderProductionClientConfiguration 
                { 
                    RequestProtocol               = "https", 
                    ServiceHostAddress            = "google.com", 
                    CreateOrderUrlTemplate        = "/partners/{partner}",
                    RequestTimeoutSeconds         = 60,
                    ConnectionLeaseTimeoutSeconds = 300
                };

                var client = new TestOrderProductionClient(config, response);

                var result = await client.SubmitOrderForProductionAsync(new CreateOrderMessage { Identity = new OrderIdentity { PartnerCode = "ABX" }}, null);

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
                 var config = new OrderProductionClientConfiguration 
                { 
                    RequestProtocol               = "https", 
                    ServiceHostAddress            = "google.com", 
                    CreateOrderUrlTemplate        = "/partners/{partner}",
                    RequestTimeoutSeconds         = 60,
                    ConnectionLeaseTimeoutSeconds = 300
                };

                var client = new TestOrderProductionClient(config, response);
                var result = await client.SubmitOrderForProductionAsync(new CreateOrderMessage { Identity = new OrderIdentity { PartnerCode = "ABX" }}, null);

                result.Should().NotBeNull("because the result should have been returned");
                result.Outcome.Should().Be(Outcome.Failure, "because the request failed");
                result.Reason.Should().Be(responseStatus.ToString(), "because the status code should be used as the reason");
                result.Recoverable.Should().Be(Recoverability.Final, "because the response was not a retriable code");
                result.Payload.Should().BeNull("because there was no content from the failed request");                
            }
        }

        #region Nested Classes

            private class TestOrderProductionClient : OrderProductionClient
            {
                private readonly HttpResponseMessage response;   
                
                public HttpRequestMessage Request;                
                public TimeSpan RequestTimeout;
                public string RequestContent;

                public TestOrderProductionClient(OrderProductionClientConfiguration config,
                                     HttpResponseMessage         response = null) : base(config, null)
                {   
                    this.response = response;
                }

                protected override async Task<HttpResponseMessage> SendRequestAsync(HttpRequestMessage request, TimeSpan timeout)
                {
                    this.Request = request;
                    this.RequestTimeout = timeout;
                    this.RequestContent = (await request.Content.ReadAsStringAsync());

                    return this.response;
                }

                protected override HttpClient CreateHttpClient(string requestProtocol, string hostServiceAddress, string clientCertificateThumbprint, int requestTimeoutSeconds, int connectionLeaseTimeoutSeconds) => 
                    new HttpClient();
                
            }

        #endregion
    }
}
