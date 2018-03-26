using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using FluentAssertions;
using OrderFulfillment.Api.ActionResults;
using OrderFulfillment.Api.Extensions;
using OrderFulfillment.Core.Models.Errors;
using Xunit;


namespace OrderFulfillment.Api.Tests.Extensions
{
    /// <summary>
    ///   The suite of tests for the <see cref="ApiControllerExtensions" /> class.
    /// </summary>
    /// 
    public class ApiControllerExtensionsTests
    {
        /// <summary>
        ///   Gets the set of data to use for validating that the "Accepted" extension validates 
        ///   its target.
        /// </summary>
        /// 
        public static IEnumerable<object[]> AcceptedValidationData
        {
            get
            {
                Action        underTest;
                ApiController controller = null;

                underTest = () => controller.Accepted("bleh", TimeSpan.FromSeconds(5));
                yield return new object[] { underTest, "Accepted with content and retry should validate the target" };

                underTest = () => controller.Accepted(12);
                yield return new object[] { underTest, "Accepted with content only should validate the target" };                

                underTest = () => controller.Accepted(new Dictionary<string, string>(), TimeSpan.FromSeconds(4));
                yield return new object[] { underTest, "Accepted with the dictionary and retry should validate the target" };

                yield break;               
            }
        }

        /// <summary>
        ///   Gets the set of data to use for validating that the "BadRequest" extension validates 
        ///   its target.
        /// </summary>
        /// 
        public static IEnumerable<object[]> BadRequestValidationData
        {
            get
            {
                Action underTest;
                ApiController controller = null;

                underTest = () => controller.BadRequest(new ErrorSet(), "some content language");
                yield return new object[] { underTest, "Bad request with the error set only should validate the target" };

                underTest = () => controller.BadRequest(Enumerable.Empty<Error>(), "some content language");
                yield return new object[] { underTest, "Bad request with enumerable error should validate the target" };

                underTest = () => controller.BadRequest(new Error("test", "test", "This is a test"), "some content language");
                yield return new object[] { underTest, "Bad request with error should validate the target" };

                yield break;
            }
        }

        /// <summary>
        ///   Gets the set of data to use for validating that the "ServiceUnavailable" extension validates 
        ///   its target.
        /// </summary>
        /// 
        public static IEnumerable<object[]> ServiceUnavailableValidationData
        {
            get
            {
                Action underTest;
                ApiController controller = null;

                underTest = () => controller.ServiceUnavailable(new TimeSpan());
                yield return new object[] { underTest, "Service unavailable with retry after set should validate the target" };
                
                yield break;
            }
        }

        /// <summary>
        ///   Validates that the "Accepted" extension validates its target.
        /// </summary>
        /// 
        /// <param name="underTest">The ation under test.</param>
        /// <param name="reason">The reason to pass when validating.</param>
        /// 
        [Theory]
        [MemberData(nameof(AcceptedValidationData))]
        public void AcceptedValidatesTarget(Action underTest,
                                            string reason)
        {
            underTest.ShouldThrow<ArgumentNullException>(reason);
        }

        /// <summary>
        ///   Verifies that the "Accepted" extension passes the proper information
        ///   to the AcceptedWithLinksResult.
        /// </summary>
        /// 
        [Fact]
        public void AcceptedWithContentAndRetry()
        {
            var controller = new DummyController
            {
                Configuration = new HttpConfiguration(),
                Request       = new HttpRequestMessage()
            };

            var retrySeconds = TimeSpan.FromSeconds(30);
            var content      = 12.0f;
            var result       = controller.Accepted(content, retrySeconds);

            result.Should().BeOfType<AcceptedResult<float>>().And.Should().NotBeNull("because a result should have been generated");

            result.ResponseContent.Should().Be(content, "because the content have been returned.");
            result.RetryAfter.Should().Be(retrySeconds, "because the duration should match the provided TimeSpan"); 
        }

        /// <summary>
        ///   Verifies that the "Accepted" extension passes the proper information
        ///   to the AcceptedWithLinksResult.
        /// </summary>
        /// 
        [Fact]
        public void AcceptedWithContentOnly()
        {
            var controller = new DummyController
            {
                Configuration = new HttpConfiguration(),
                Request       = new HttpRequestMessage()
            };

            var content = "yay!";
            var result  = controller.Accepted(content);

            result.Should().BeOfType<AcceptedResult<string>>().And.Should().NotBeNull("because a result should have been generated");

            result.ResponseContent.Should().Be(content, "because the content have been returned.");
            result.RetryAfter.Should().Be(TimeSpan.Zero, "because the duration not have been set"); 
        }

        /// <summary>
        ///   Validates that the "BadRequest" extension validates its target.
        /// </summary>
        /// 
        /// <param name="underTest">The action under test.</param>
        /// <param name="error">The error to pass when validating.</param>
        /// 
        [Theory]
        [MemberData(nameof(BadRequestValidationData))]
        public void BadRequestValidatesTarget(Action underTest,
                                              string error)
        {
            underTest.ShouldThrow<ArgumentNullException>(error);
        }

        /// <summary>
        ///   Verifies that the "BadRequest" extension passes the proper information
        ///   to the BadRequestWithErrorSetResult.
        /// </summary>
        /// 
        [Fact]
        public void BadRquestWithErrorSet()
        {
            var controller = new DummyController
            {
                Configuration = new HttpConfiguration(),
                Request      = new HttpRequestMessage()
            };

            var error    = new Error("test-errorCode", "test-memberPath", "test-description");
            var errorSet = new ErrorSet(error);

            var result = controller.BadRequest(errorSet);

            result.Should().BeOfType<BadRequestWithErrorSetResult>().And.Should().NotBeNull("because a result should have been generated");
            result.Content.Errors.Should().NotBeNull("because errors should have been returned.");
            result.Content.Errors.Should().HaveCount(1);
            result.Content.Errors.First().ShouldBeEquivalentTo(error); 
        }

        /// <summary>
        ///   Verifies that the "BadRequest" extension passes the proper information
        ///   to the BadRequestWithErrorSetResult.
        /// </summary>
        /// 
        [Fact]
        public void BadRequestWithEnumerable()
        {
            var controller = new DummyController
            {
                Configuration = new HttpConfiguration(),
                Request       = new HttpRequestMessage()
            };

            var error1          = new Error("test-errorCode1", "test-memberPath1", "test-description1");
            var error2          = new Error("test-errorCode2", "test-memberPath2", "test-description2");
            var enumerableError = new [] { error1, error2 };

            var result = controller.BadRequest(enumerableError);

            result.Should().BeOfType<BadRequestWithErrorSetResult>().And.Should().NotBeNull("because a result should have been generated");
            result.Content.Errors.Should().NotBeNull("because an enumeration of errors should have been returned.");
            result.Content.Errors.Should().HaveCount(2);
            result.Content.Errors.First().ShouldBeEquivalentTo(error1);
            result.Content.Errors.Last().ShouldBeEquivalentTo(error2);
        }

        /// <summary>
        ///   Verifies that the "BadRequest" extension passes the proper information
        ///   to the BadRequestWithErrorSetResult.
        /// </summary>
        /// 
        [Fact]
        public void BadRequestWithEmptyErrorInformation()
        {
            var controller = new DummyController
            {
                Configuration = new HttpConfiguration(),
                Request       = new HttpRequestMessage()
            };

            var errorInformation = new Error[0];

            var result = controller.BadRequest(errorInformation);

            result.Should().BeOfType<BadRequestWithErrorSetResult>().And.Should().NotBeNull("because a result should have been generated");
            result.Content.Errors.Should().NotBeNull("because an enumeration of errors should have been returned.");
            result.Content.Errors.Should().HaveCount(0);
        }

        /// <summary>
        ///   Validates that the "ServiceUnavailable" extension validates its target.
        /// </summary>
        /// 
        /// <param name="underTest">The action under test.</param>
        /// <param name="error">The error to pass when validating.</param>
        /// 
        [Theory]
        [MemberData(nameof(ServiceUnavailableValidationData))]
        public void ServiceUnavailableValidatesTarget(Action underTest,
                                              string error)
        {
            underTest.ShouldThrow<ArgumentNullException>(error);
        }
        /// <summary>
        /// Verifies that the "ServiceUnavailable" extension passes the proper information to the ServiceUnavailableResult.
        /// </summary>
        [Fact]
        public void ServiceUnavailable()
        {
            var controller = new DummyController
            {
                Configuration = new HttpConfiguration(),
                Request       = new HttpRequestMessage()
            };

            var retryAfter = TimeSpan.FromSeconds(5);

            var result = controller.ServiceUnavailable(retryAfter);

            result.Should().BeOfType<ServiceUnavailableResult>().And.Should().NotBeNull("because a result should have been generated");

            result.Request.Should().NotBeNull("Retry-After", "because the response should specify the Retry-After.");
            result.RetryAfter.Should().Be(retryAfter, "because the duration should match the provided TimeSpan");
        }

        #region Nested Classes
                
            private class DummyController : ApiController
            {
            }

        #endregion
    }
}
