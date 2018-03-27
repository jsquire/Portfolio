using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using FluentAssertions;
using OrderFulfillment.Api.ActionResults;
using Moq;
using Xunit;

namespace OrderFulfillment.Api.Tests.ActionResults
{
    /// <summary>
    ///   The set of tests for the <see cref="AcceptedResult"/> class.
    /// </summary>
    ///
    public class AcceptedResultTests
    {        

        /// <summary>
        ///   Verifies that the AcceptedWithLinksResult allows an empty link set.
        /// </summary>
        ///
        [Fact]
        public void NullContentIsAllowed()
        {
            Action actionUnderTest = () => new AcceptedResult<object>(Mock.Of<ApiController>(), (object)null);
        
            actionUnderTest.ShouldNotThrow("because a null content instance is allowed.");
        }
    
        /// <summary>
        ///   Verifies that the HTTP status code for the result is correctly set.
        /// </summary>
        ///
        [Fact]
        public async void HttpStatusCodeIsCorrect()
        {
            var controller = new DummyController
            {
                Configuration = new HttpConfiguration(),
                Request       = new HttpRequestMessage()
            };
            
            var actionResult = new AcceptedResult<DummyResponse>(controller, new DummyResponse());
            var response     = await actionResult.ExecuteAsync(new CancellationToken());
            
            response.StatusCode.Should().Be(HttpStatusCode.Accepted, "because the result should set the provided status code");
        }
    
        /// <summary>
        ///   Verifies that the Retry-After header is set on the response when a  non-zero time span is passed in.
        /// </summary>
        ///
        [Fact]
        public async Task RetryAfterIsSetOnResponse()
        {
            var controller = new DummyController
            {
                Configuration = new HttpConfiguration(),
                Request       = new HttpRequestMessage()
            };
            
            var retrySeconds = TimeSpan.FromSeconds(30);
            var actionResult = new AcceptedResult<DummyResponse>(controller, new DummyResponse(), retrySeconds);
            var response     = await actionResult.ExecuteAsync(new CancellationToken());
            
            response.Headers.RetryAfter.Should().NotBeNull("Retry-After", "because the response should specify the Retry-After.");
            response.Headers.RetryAfter.Delta.Should().Be(retrySeconds, "because the duration should match the provided TimeSpan"); 
        }
                
        [Fact]
        public async Task RetryAfterIsNotSetForTimeSpanZero()
        {
            var controller = new DummyController
            {
                Configuration = new HttpConfiguration(),
                Request       = new HttpRequestMessage()
            };
            
            var actionResult = new AcceptedResult<DummyResponse>(controller, new DummyResponse(), TimeSpan.Zero);
            var response     = await actionResult.ExecuteAsync(new CancellationToken());
            
            response.Headers.RetryAfter.Should().BeNull("because the response should not specify the Retry-After.");
        }

        /// <summary>
        ///   Verifies that the Retry-After header is not set on the response when a zero time span is passed in.
        /// </summary>
        ///
        [Fact]
        public async Task RetryAfterIsNotSetWhenNoTimeSpanIsProvided()
        {
            var controller = new DummyController
            {
                Configuration = new HttpConfiguration(),
                Request       = new HttpRequestMessage()
            };
            
            var actionResult = new AcceptedResult<DummyResponse>(controller, new DummyResponse());
            var response     = await actionResult.ExecuteAsync(new CancellationToken());
            
            response.Content.Headers.Should().NotContain("Retry-After", "because the response should not specify the Retry-After.");
        }

        /// <summary>
        ///   Verifies that the response content is set.
        /// </summary>
        ///
        [Fact]
        public async Task ContentIsSet()
        {
            var controller = new DummyController
            {
                Configuration = new HttpConfiguration(),
                Request       = new HttpRequestMessage()
            };

            controller.Request.Headers.Accept.Clear();
            controller.Request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            
            var expectedContent = new DummyResponse { Name = "Test", Age = 50 };
            var actionResult    = new AcceptedResult<DummyResponse>(controller, expectedContent);
            var response        = await actionResult.ExecuteAsync(new CancellationToken());
            var responsContent  = await response.Content.ReadAsAsync<DummyResponse>();
            
            responsContent.Should().NotBeNull("because the content set should be present in the response");
            responsContent.ShouldBeEquivalentTo(expectedContent, "because the content should be returned in the response body");
        }

        #region Nested Classes
                    
            private class DummyController : ApiController
            {
            }

            private class DummyResponse
            {
                public string Name { get;  set; }
                public int Age { get;  set; }
            }
        
        #endregion
    }
}
