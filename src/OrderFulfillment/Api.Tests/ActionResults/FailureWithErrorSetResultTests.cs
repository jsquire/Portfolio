using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using FluentAssertions;
using OrderFulfillment.Api.ActionResults;
using OrderFulfillment.Core.Models.Errors;
using Moq;
using Xunit;

namespace OrderFulfillment.Api.Tests.ActionResults
{
    /// <summary>
    ///   The set of tests for the <see cref="FailureWithErrorSetResult"/> class.
    /// </summary>
    ///
    public class FailureWithErrorSetResultTests
    {
        /// <summary>
        ///   The set of data for use with the testing error sets of varying sizes.
        /// </summary>
        /// 
        public static IEnumerable<object[]> ErrorSetData
        {
            get
            {
                return new []
                {
                    new object[] { new ErrorSet()                                                                },
                    new object[] { new ErrorSet(ErrorCode.OrderIdentifierMalformed, "single errror description") },
                    new object[] { new ErrorSet(FailureWithErrorSetResultTests.GenerateErrors(2))                },
                    new object[] { new ErrorSet(FailureWithErrorSetResultTests.GenerateErrors(5))                },
                    new object[] { new ErrorSet(FailureWithErrorSetResultTests.GenerateErrors(10))               }
                };
            }
        }

        /// <summary>
        ///   Verifies that the FailureWithErrorSetResult does not allow a null error set.
        /// </summary>
        ///
        [Fact]
        public void NullMessageIsRejected()
        {
            Action actionUnderTest = () =>  new FailureWithErrorSetResult(HttpStatusCode.BadRequest, Mock.Of<ApiController>(), null);
            
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the message parameter cannot be null.")
                           .Where(ex => ex.Message.Contains("errorSet"), "because only the message parameter should be invalid.");
        }
    
        /// <summary>
        ///   Verifies that the FailureWithErrorSetResult allows an empty error set.
        /// </summary>
        ///
        [Fact]
        public void EmptyErrorSetIsAllowed()
        {
            Action actionUnderTest = () => new FailureWithErrorSetResult(HttpStatusCode.Conflict, Mock.Of<ApiController>(), new ErrorSet());
        
            actionUnderTest.ShouldNotThrow("because an empty message is allowed.");
        }
    
        /// <summary>
        ///   Verifies that the HTTP status code for the result is correctly set to HTTP 409 (Conflict).
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
            
            var statusCode   = HttpStatusCode.Forbidden;
            var actionResult = new FailureWithErrorSetResult(statusCode, controller, new ErrorSet());
            var response     = await actionResult.ExecuteAsync(new CancellationToken());
            
            response.StatusCode.Should().Be(statusCode, "because the result should set the provided status code");
        }
    
        /// <summary>
        ///   Verifies that the Content-Type header is set on the response.
        /// </summary>
        ///
        [Fact]
        public async Task ContentTypeIsSetOnResponse()
        {
            var controller = new DummyController
            {
                Configuration = new HttpConfiguration(),
                Request       = new HttpRequestMessage()
            };
            
            var statusCode   = HttpStatusCode.MethodNotAllowed;
            var actionResult = new FailureWithErrorSetResult(statusCode, controller, new ErrorSet());
            var response     = await actionResult.ExecuteAsync(new CancellationToken());
            
            response.Content.Headers.ContentLanguage.Any().Should().BeTrue("because the response should specify the content type.");
        }

        /// <summary>
        ///   Verifies that the error set is returned with the result.
        /// </summary>
        ///
        /// <param name="errorSet">The expected set of errors.</param>
        ///
        [Theory]
        [MemberData(nameof(ErrorSetData))]
        public async Task ErrorSetIsReturned(ErrorSet errorSet)
        {
            var controller = new DummyController
            {
                Configuration = new HttpConfiguration(),
                Request       = new HttpRequestMessage()
            };

            controller.Request.Headers.Accept.Clear();
            controller.Request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        
            var statusCode     = HttpStatusCode.MethodNotAllowed;
            var actionResult   = new FailureWithErrorSetResult(statusCode, controller, errorSet);
            var response       = await actionResult.ExecuteAsync(new CancellationToken());
            var responseSet    = await response.Content.ReadAsAsync<ErrorSet>();
            
            responseSet.Should().NotBeNull("because an error set should be present in the response");
            responseSet.Errors.Should().NotBeNull("because there should be an error collection present in the response, even if empty");
            
            var expectedErrors = errorSet.Errors.OrderBy(item => item.Code).ToList();
            var responseErrors = responseSet.Errors.OrderBy(item => item.Code).ToList();

            responseErrors.Should().HaveSameCount(expectedErrors, "because the number of errors in the result should match the provided error set")
                          .And.Equal(expectedErrors, (left, right) => ((left.Code == right.Code) && (left.Description == right.Description)), "because the provided error set should have been used in the response");
        }

        /// <summary>
        ///   Verifies that the set message is returned with the content language.
        /// </summary>
        ///
        /// <param name="expectedContentType">The expected value of the Content-Type.</param>
        ///
        [Theory]
        [InlineData("en-US")]
        [InlineData("en-GB")]
        [InlineData("zhw")]
        [InlineData("es-SP")]
        public async Task ContentTypeIsReturned(string expectedContentType)
        {
            var controller = new DummyController
            {
                Configuration = new HttpConfiguration(),
                Request       = new HttpRequestMessage()
            };
            
            var statusCode   = HttpStatusCode.ExpectationFailed;
            var actionResult = new FailureWithErrorSetResult(statusCode, controller, new ErrorSet(), expectedContentType);
            var response     = await actionResult.ExecuteAsync(new CancellationToken());
            
            response.Content.Headers.ContentLanguage.FirstOrDefault().Should().Be(expectedContentType, "because the Content-Type header should ahve been set.");
        }
    
        /// <summary>
        ///   Verifies that the Content-Type header uses a default value of not explicitly set.
        /// </summary>
        ///
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("  ")]
        public async Task ContentTypeAssignsDefault(string contentType)
        {
            var controller = new DummyController
            {
                Configuration = new HttpConfiguration(),
                Request       = new HttpRequestMessage()
            };
            
            var statusCode   = HttpStatusCode.HttpVersionNotSupported;
            var actionResult = new FailureWithErrorSetResult(statusCode, controller, new ErrorSet(), contentType);
            var response     = await actionResult.ExecuteAsync(new CancellationToken());
            
            response.Content.Headers.ContentLanguage.Any().Should().BeTrue("because the response should specify the content type.");
            response.Content.Headers.ContentLanguage.First().Trim().Length.Should().BeGreaterOrEqualTo(1, "because the content type should have a default value.");
        }

        /// <summary>
        ///   Generates an enumerable of unique errors, using a predictable pattern for populating
        ///   the code and description.
        /// </summary>
        /// 
        /// <param name="count">The count of errors in the enumerable.  Must be in the range of 0 - 100, inclusive.</param>
        /// 
        /// <returns>The enumerable containing the errors.</returns>
        /// 
        private static IEnumerable<Error> GenerateErrors(int count = 1)
        {
            if ((count < 0) || (count > 100))
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            for (var index = 0; index < count; ++index)
            {
                yield return new Error($"ErrorCode { index }", $"This is the description for { index }");
            }

            yield break;
        }

        #region Nested Classes

            /// <summary>
            ///   Serves as a stub controller for test injection purposes, as API controllers are
            ///   resistent to mocking.
            /// </summary>
            ///
            /// <seealso cref="System.Web.Http.ApiController" />
            ///
            private class DummyController : ApiController
          {
          }
        
        #endregion
    }
}
