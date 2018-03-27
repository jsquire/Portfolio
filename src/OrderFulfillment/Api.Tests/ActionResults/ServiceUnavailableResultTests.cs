using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using FluentAssertions;
using OrderFulfillment.Api.ActionResults;
using Xunit;

namespace OrderFulfillment.Api.Tests.ActionResults
{
    /// <summary>
    ///   The suite of tsts for the <see cref="ServiceUnavailableResult" />
    ///   class.
    /// </summary>
    /// 
    public class ServiceUnavailableResultTests
    {
        /// <summary>
        ///   Verifies that the HTTP response code is correct.
        /// </summary>
        /// 
        [Fact]
        public async Task HttpStatusCodeIsCorrect()
        {
            var controller = new DummyController()
            {
                Configuration = new HttpConfiguration(),
                Request       = new HttpRequestMessage()
            };

            var actionResult = new ServiceUnavailableResult(controller, TimeSpan.FromSeconds(5));
            var response     = await actionResult.ExecuteAsync(new CancellationToken());

            response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable, "because the result should set the provided status code");
        }

        /// <summary>
        ///   Verifies that Retry-After header is correctt.
        /// </summary>
        /// 
        [Fact]
        public async Task RetryAfterIsSetOnResponse()
        {
            var controller = new DummyController()
            {
                Configuration = new HttpConfiguration(),
                Request       = new HttpRequestMessage()
            };

            var retryAfter   = TimeSpan.FromSeconds(5);
            var actionResult = new ServiceUnavailableResult(controller, retryAfter);
            var response     = await actionResult.ExecuteAsync(new CancellationToken());

            response.Headers.RetryAfter.Should().NotBeNull("Retry-After", "because the response should specify the Retry-After");
            response.Headers.RetryAfter.Delta.Should().Be(retryAfter, "because the duration should match the provided TimeSpan");
        }

        /// <summary>
        ///   Verifies that Retry-After header is correctt.
        /// </summary>
        /// 
        [Fact]
        public async Task RetryAfterIsNotSetForTimeSpanZero()
        {
            var controller = new DummyController()
            {
                Configuration = new HttpConfiguration(),
                Request       = new HttpRequestMessage()
            };

            var actionResult = new ServiceUnavailableResult(controller, TimeSpan.Zero);
            var response     = await actionResult.ExecuteAsync(new CancellationToken());

            response.Headers.RetryAfter.Should().BeNull("because the response should not specify the Retry-After");
        }

        /// <summary>
        ///   Verifies that Retry-After header is correctt.
        /// </summary>
        /// 
        [Fact]
        public async Task RetryAfterIsNotSetWhenTimeSpanNotSet()
        {
            var controller = new DummyController()
            {
                Configuration = new HttpConfiguration(),
                Request       = new HttpRequestMessage()
            };

            var actionResult = new ServiceUnavailableResult(controller, new TimeSpan());
            var response     = await actionResult.ExecuteAsync(new CancellationToken());

            response.Headers.RetryAfter.Should().BeNull("because the response should not specify the Retry-After");
        }

        #region Nested Classes

            private class DummyController : ApiController
            {
            }

        #endregion
    }
}
