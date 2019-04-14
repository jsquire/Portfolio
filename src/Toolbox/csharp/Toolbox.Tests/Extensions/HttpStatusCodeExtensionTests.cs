using System.Collections.Generic;
using System.Linq;
using System.Net;
using FluentAssertions;
using Xunit;

namespace Squire.Toolbox.Tests.Extensions
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
                .ToHashSet();

            return
                ((IEnumerable<int>)typeof(HttpStatusCode).GetEnumValues())
                .Select(value => (HttpStatusCode)value)
                .Where(value => !retriableCodes.Contains(value))
                .Distinct()
                .Select(value => new object[] { value });
        }

        /// <summary>
        ///   Verifies functionality for the <see cref="HttpStatusCodeExtensions.ShouldRetry" />
        ///   method.
        /// </summary>
        ///
        /// <param name="statusCode">The status code to consider.</param>
        ///
        [Theory]
        [MemberData(nameof(GetRetriableCodes))]
        [TestCategory(Category.BuildVerification)]
        public void RetriableCodesShouldAllowRetry(HttpStatusCode statusCode)
        {
            statusCode.ShouldRetry().Should().BeTrue("because when the status code is encountered, retry is encouraged");
        }

        /// <summary>
        ///   Verifies functionality for the <see cref="HttpStatusCodeExtensions.ShouldRetry" />
        ///   method.
        /// </summary>
        ///
        /// <param name="statusCode">The status code to consider.</param>
        ///
        [Theory]
        [MemberData(nameof(GetNoRetryCodes))]
        [TestCategory(Category.BuildVerification)]
        public void NonRetriableCodesShouldDenyRetry(HttpStatusCode statusCode)
        {
            statusCode.ShouldRetry().Should().BeFalse("because when the status code is encountered, retry is encouraged");
        }
    }
}
