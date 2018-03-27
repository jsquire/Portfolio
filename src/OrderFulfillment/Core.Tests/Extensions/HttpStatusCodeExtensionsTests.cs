using System.Collections.Generic;
using System.Linq;
using System.Net;
using FluentAssertions;
using OrderFulfillment.Core.Extensions;
using OrderFulfillment.Core.Models.Operations;
using Xunit;

namespace OrderFulfillment.Core.Tests.Extensions
{
    /// <summary>
    ///   The suite of tests for the <see cref="HttpStatusCodeExtensions" />
    ///   class.
    /// </summary>
    /// 
    public class HttpStatusCodeExtensionsTests
    {
        /// <summary>
        ///   The set of test data for the non-retriable codes.
        /// </summary>
        /// 
        /// <returns>The object array structure of retriable codes expected by XUnit.</returns>
        /// 
        public static IEnumerable<object[]> GetRetriableCodes()
        {
            yield return new object[] { HttpStatusCode.Unauthorized };
            yield return new object[] { HttpStatusCode.RequestTimeout };
            yield return new object[] { HttpStatusCode.InternalServerError };
            yield return new object[] { HttpStatusCode.ServiceUnavailable };
            yield return new object[] { HttpStatusCode.GatewayTimeout };
            yield return new object[] { HttpStatusCodeExtensions.RateLimitExceeded };
            yield break;
        }

        /// <summary>
        ///   The set of test data for the non-retriable codes.
        /// </summary>
        /// 
        /// <returns>The object array structure of retriable codes expected by XUnit.</returns>
        /// 
        public static IEnumerable<object[]> GetNoRetryCodes()
        {
            var retriableCodes = HttpStatusCodeExtensionsTests.GetRetriableCodes()
                .Select(wrapper => wrapper[0])
                .ToList();

            return
                ((IEnumerable<int>)typeof(HttpStatusCode).GetEnumValues())		
                .Select(value => (HttpStatusCode)value)
                .Where(value => !retriableCodes.Contains(value))
                .Select(value => new object[] { value });
        }

        /// <summary>
        ///   Verifies functionality for the <see cref="HttpStatusCodeExtensions.RetriableCodesShouldAllowRetry" />
        ///   method.
        /// </summary>
        /// 
        /// <param name="statusCode">The status code to consider.</param>
        /// 
        [Theory]
        [MemberData(nameof(GetRetriableCodes))]
        public void RetriableCodesShouldAllowRetry(HttpStatusCode statusCode)
        {
            statusCode.IsRetryEncouraged().Should().Be(Recoverability.Retriable, "because when the status code is encountered, retry is encouraged");
        }

        /// <summary>
        ///   Verifies functionality for the <see cref="HttpStatusCodeExtensions.RetriableCodesShouldAllowRetry" />
        ///   method.
        /// </summary>
        /// 
        /// <param name="statusCode">The status code to consider.</param>
        /// 
        [Theory]
        [MemberData(nameof(GetNoRetryCodes))]
        public void NonRetriableCodesShouldBeFinal(HttpStatusCode statusCode)
        {
            statusCode.IsRetryEncouraged().Should().Be(Recoverability.Final, "because when the status code is encountered, retry is encouraged");
        }
    }
}
