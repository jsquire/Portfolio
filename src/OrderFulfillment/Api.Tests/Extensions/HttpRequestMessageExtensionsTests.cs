using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Routing;
using FluentAssertions;
using OrderFulfillment.Api.Extensions;
using OrderFulfillment.Core.Infrastructure;
using Moq;
using Xunit;

namespace OrderFulfillment.Api.Tests.Extensions
{
    /// <summary>
    ///   The suite of tests for the <see cref="HttpRequestMessageExtensions" />
    ///   class
    /// </summary>
    /// 
    public class HttpRequestMessageExtensionsTests
    {
        /// <summary>
        ///   Verifies behavior of the <see cref="HttpRequestMessageExtensions.GetOrderFulfillmentCorrelationId" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void GetOrderFulfillmentCorrelationIdWithANullRequest()
        {
            HttpRequestMessage request = null;

            Action act = () => request.GetOrderFulfillmentCorrelationId();

            act.ShouldThrow<ArgumentNullException>();
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="HttpRequestMessageExtensions.GetOrderFulfillmentCorrelationId" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void RequestWithNoHeadersReturnsAspCorrelationId()
        {
            var request = new HttpRequestMessage();
            var expected = request.GetCorrelationId().ToString();

            var actual = request.GetOrderFulfillmentCorrelationId();

            actual.Should().Be(expected);
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="HttpRequestMessageExtensions.GetOrderFulfillmentCorrelationId" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void RequestWithCorrelationIdHeaderReturnsGivenValue()
        {
            var expected = "test-correlation-id";
            var request = new HttpRequestMessage
            {
                Headers =
                {
                    { HttpHeaders.CorrelationId, expected }
                }
            };

            var actual = request.GetOrderFulfillmentCorrelationId();

            actual.Should().Be(expected);
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="HttpRequestMessageExtensions.ReadContentAsStringAsync" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public async Task ReadContentAsStringWithANullRequestMessage()
        {
            (await ((HttpRequestMessage)null).ReadContentAsStringAsync()).Should().Be(String.Empty, "because a null request message has no content");
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="HttpRequestMessageExtensions.ReadContentAsStringAsync" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public async Task ReadContentAsStringWithNullContent()
        {
            var httpConfiguration = new HttpConfiguration();
            var routeData         = new HttpRouteData(new HttpRoute());
            var request           = new HttpRequestMessage();
                                
            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);

            (await request.ReadContentAsStringAsync()).Should().Be(String.Empty, "because a request with no content has no content");
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="HttpRequestMessageExtensions.ReadContentAsStringAsync" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public async Task FirstReadOfContent()
        {
                
            var requestBody        = "REQUEST BODY CONTENT";
            var httpConfiguration  = new HttpConfiguration();
            var routeData          = new HttpRouteData(new HttpRoute());
            var request            = new HttpRequestMessage();

            request.Content = new StringContent(requestBody);
            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);

            (await request.ReadContentAsStringAsync()).Should().Be(requestBody, "because the content should have been read");
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="HttpRequestMessageExtensions.ReadContentAsStringAsync" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public async Task RepeatedReadOfContent()
        {
                
            var requestBody        = "REQUEST BODY CONTENT";
            var httpConfiguration  = new HttpConfiguration();
            var routeData          = new HttpRouteData(new HttpRoute());
            var request            = new HttpRequestMessage();

            request.Content = new StringContent(requestBody);
            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);

            (await request.Content.ReadAsStringAsync()).Should().Be(requestBody, "because the initial read of the content should be consistent with the value");
            (await request.ReadContentAsStringAsync()).Should().Be(requestBody, "because the content should have been able to be read a second time");
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="HttpRequestMessageExtensions.SafeReadContentAsStringAsync" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public async Task SafeReadContentAsStringWithANullRequestMessage()
        {
            (await ((HttpRequestMessage)null).SafeReadContentAsStringAsync()).Should().Be(String.Empty, "because a null request message has no content");
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="HttpRequestMessageExtensions.SafeReadContentAsStringAsync" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public async Task SafeReadContentAsStringWithNullContent()
        {
            var httpConfiguration = new HttpConfiguration();
            var routeData         = new HttpRouteData(new HttpRoute());
            var request           = new HttpRequestMessage();
                                
            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);

            (await request.SafeReadContentAsStringAsync()).Should().Be(String.Empty, "because a request with no content has no content");
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="HttpRequestMessageExtensions.SafeReadContentAsStringAsync" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public async Task FirstSafeReadOfContent()
        {
                
            var requestBody        = "REQUEST BODY CONTENT";
            var httpConfiguration  = new HttpConfiguration();
            var routeData          = new HttpRouteData(new HttpRoute());
            var request            = new HttpRequestMessage();

            request.Content = new StringContent(requestBody);
            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);

            (await request.SafeReadContentAsStringAsync()).Should().Be(requestBody, "because the content should have been read");
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="HttpRequestMessageExtensions.SafeReadContentAsStringAsync" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public async Task RepeatedSafeReadOfContent()
        {
                
            var requestBody        = "REQUEST BODY CONTENT";
            var httpConfiguration  = new HttpConfiguration();
            var routeData          = new HttpRouteData(new HttpRoute());
            var request            = new HttpRequestMessage();

            request.Content = new StringContent(requestBody);
            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);

            (await request.Content.ReadAsStringAsync()).Should().Be(requestBody, "because the initial read of the content should be consistent with the value");
            (await request.SafeReadContentAsStringAsync()).Should().Be(requestBody, "because the content should have been able to be read a second time");
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="HttpRequestMessageExtensions.SafeReadContentAsStringAsync" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public async Task SafeReadOfUnreadableContent()
        {
                
            var expected           = "OMG, this is a default";
            var httpConfiguration  = new HttpConfiguration();
            var routeData          = new HttpRouteData(new HttpRoute());
            var request            = new HttpRequestMessage();
            var mockStream         = new Mock<Stream>();

            mockStream.SetupGet(stream => stream.CanRead)
                        .Returns(false);

            mockStream.SetupGet(stream => stream.CanSeek)
                        .Returns(false);
                                         
            request.Content = new StreamContent(mockStream.Object);
            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);

            (await request.SafeReadContentAsStringAsync(expected)).Should().Be(expected, "because stream is not readable and should have used the default value");
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="HttpRequestMessageExtensions.SafeReadContentAsStringAsync" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public async Task SafeReadOfContentWithException()
        {
                
            var expected           = "OMG, this is a default";
            var httpConfiguration  = new HttpConfiguration();
            var routeData          = new HttpRouteData(new HttpRoute());
            var request            = new HttpRequestMessage();
            var mockStream         = new Mock<Stream>();

            mockStream.SetupGet(stream => stream.CanRead)
                        .Throws(new InvalidOperationException());

            mockStream.Setup(stream => stream.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                        .Throws(new InvalidOperationException());
                                         
            request.Content = new StreamContent(mockStream.Object);
            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);

            (await request.SafeReadContentAsStringAsync(expected)).Should().Be(expected, "because stream is not readable and should have used the default value");
        }
    }
}
