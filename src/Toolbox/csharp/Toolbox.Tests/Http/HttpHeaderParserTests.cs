using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Squire.Toolbox;
using Xunit;

namespace Squire.Toolbox.Tests
{
    /// <summary>
    ///   The suite of tests for the <see cref="HttpHeaderParser" />
    ///   class.
    /// </summary>
    ///
    public class HttpHeaderParserTests
    {
        /// <summary>
        ///   Verifies the behavior of the <see cref="HttpHeaderParser.ParseAuthorization" /> method.
        /// </summary>
        ///
        /// <param name="headerName">The value to use as the header name when parsing.</param>
        ///
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [TestCategory(Category.BuildVerification)]
        public void ParseAuthorizationValidatesHeaderName(string headerName)
        {
            var headerValue  = @"Digest qop=""chap"", realm=""someplace@host.com""";
            var parser       = new HttpHeaderParser();

            Action actionUnderTest = () => parser.ParseAuthorization(headerValue, headerName);

            actionUnderTest.Should().Throw<ArgumentNullException>("because parsing an authorization header should require a header name");
        }

        /// <summary>
        ///   Verifies the behavior of the <see cref="HttpHeaderParser.ParseAuthorization" /> method.
        /// </summary>
        ///
        /// <param name="headerValue">The value to use as the header value when parsing.</param>
        ///
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [TestCategory(Category.BuildVerification)]
        public void ParseAuthorizationWithEmptyHeaderValue(string headerValue)
        {
            var parser   = new HttpHeaderParser();
            var expected = (IReadOnlyDictionary<string, string>)new Dictionary<string, string>();
            var actual   = parser.ParseAuthorization(headerValue);

            actual.Should().BeEquivalentTo(expected, "because an empty header value should result in an empty parse result");
        }

        /// <summary>
        ///   Verifies the behavior of the <see cref="HttpHeaderParser.<see cref="HttpHeaderParser.ParseAuthorization" />" /> method.
        /// </summary>
        ///
        [Fact]
        [TestCategory(Category.BuildVerification)]
        public void ParseAuthorizationIncludesHeaderInResponse()
        {
            var headerValue = @"Digest qop=""chap"", realm=""someplace@host.com""";
            var parser      = new HttpHeaderParser();
            var result      = parser.ParseAuthorization(headerValue);

            result.ContainsKey(HttpHeaderParser.DefaultAuthorizationHeaderName).Should().BeTrue("because there should be an entry in the result for the header name");
        }

        /// <summary>
        ///   Verifies the behavior of the <see cref="HttpHeaderParser.ParseAuthorization" /> method.
        /// </summary>
        ///
        [Fact]
        [TestCategory(Category.BuildVerification)]
        public void ParseAuthorizationRespectsCustomHeaderName()
        {
            var headerName  = "CUST-ACustomHeaderName-LULZ";
            var headerValue = @"Digest qop=""chap"", realm=""someplace@host.com""";
            var parser      = new HttpHeaderParser();
            var result      = parser.ParseAuthorization(headerValue, headerName);

            result.ContainsKey(headerName).Should().BeTrue("because there should be an entry in the result for the custom header name");
            result.ContainsKey(HttpHeaderParser.DefaultAuthorizationHeaderName).Should().BeFalse("because the custom header name should have been used");
        }

        /// <summary>
        ///   Verifies the behavior of the <see cref="HttpHeaderParser.ParseAuthorization" /> method.
        /// </summary>
        ///
        [Fact]
        [TestCategory(Category.BuildVerification)]
        public void ParseAuthorizationAllowsQuotedValues()
        {
            var headerValues = new Dictionary<string, string>
            {
               { "key",        "value"      },
               { "other",      "otherValue" },
               { "more-stuff", "omg-value"  }
            };

            var headerValue = $"Digest { String.Join(", ", headerValues.Select(pair => $@"{ pair.Key }=""{ pair.Value }""")) }";
            var parser      = new HttpHeaderParser();
            var result      = parser.ParseAuthorization(headerValue);

            result.ContainsKey(HttpHeaderParser.DefaultAuthorizationHeaderName).Should().BeTrue("because the header name should have been used");

            foreach (var pair in headerValues)
            {
                result.ContainsKey(pair.Key).Should().BeTrue("because {0} was one of the header values", pair.Key);
                result[pair.Key].Should().Be(pair.Value, "because the value for {0} should have been populated", pair.Key);
            }
        }

        /// <summary>
        ///   Verifies the behavior of the <see cref="HttpHeaderParser.ParseAuthorization" /> method.
        /// </summary>
        ///
        [Fact]
        [TestCategory(Category.BuildVerification)]
        public void ParseAuthorizationAllowsUnquotedValues()
        {
            var headerValues = new Dictionary<string, string>
            {
               { "key",        "value"      },
               { "other",      "otherValue" },
               { "more-stuff", "omg-value"  }
            };

            var headerValue = $"Digest { String.Join(", ", headerValues.Select(pair => $@"{ pair.Key }={ pair.Value }")) }";
            var parser      = new HttpHeaderParser();
            var result      = parser.ParseAuthorization(headerValue);

            result.ContainsKey(HttpHeaderParser.DefaultAuthorizationHeaderName).Should().BeTrue("because the header name should have been used");

            foreach (var pair in headerValues)
            {
                result.ContainsKey(pair.Key).Should().BeTrue("because {0} was one of the header values", pair.Key);
                result[pair.Key].Should().Be(pair.Value, "because the value for {0} should have been populated", pair.Key);
            }
        }

        /// <summary>
        ///   Verifies the behavior of the <see cref="HttpHeaderParser.ParseAuthorization" /> method.
        /// </summary>
        ///
        [Fact]
        [TestCategory(Category.BuildVerification)]
        public void ParseAuthorizationAllowsMixedQuotesInValues()
        {
            var headerValues = new Dictionary<string, string>
            {
               { "key",        "value"           },
               { "other",      @"""otherValue""" },
               { "more-stuff", @"""omg-value"    },
               { "yetanother", @"omg-value"""    },
            };

            var headerValue = $"Digest { String.Join(", ", headerValues.Select(pair => $@"{ pair.Key }={ pair.Value }")) }";
            var parser      = new HttpHeaderParser();
            var result      = parser.ParseAuthorization(headerValue);

            result.ContainsKey(HttpHeaderParser.DefaultAuthorizationHeaderName).Should().BeTrue("because the header name should have been used");

            foreach (var pair in headerValues)
            {
                result.ContainsKey(pair.Key).Should().BeTrue("because {0} was one of the header values", pair.Key);
                result[pair.Key].Should().Be(pair.Value.Replace(@"""", String.Empty), "because the value for {0} should have been populated", pair.Key);
            }
        }

        /// <summary>
        ///   Verifies the behavior of the <see cref="HttpHeaderParser.ParseAuthorization" /> method.
        /// </summary>
        ///
        [Theory]
        [InlineData(",")]
        [InlineData(" ")]
        [TestCategory(Category.BuildVerification)]
        public void ParseAuthorizationSupportsValueDelimiters(string delimiter)
        {
            var headerValues = new Dictionary<string, string>
            {
               { "key",        "value"           },
               { "other",      @"""otherValue""" },
               { "more-stuff", @"""omg-value"    },
               { "yetanother", @"omg-value"""    },
            };

            var headerValue = $"Digest { String.Join(delimiter, headerValues.Select(pair => $@"{ pair.Key }={ pair.Value }")) }";
            var parser      = new HttpHeaderParser();
            var result      = parser.ParseAuthorization(headerValue);

            result.ContainsKey(HttpHeaderParser.DefaultAuthorizationHeaderName).Should().BeTrue("because the header name should have been used");

            foreach (var pair in headerValues)
            {
                result.ContainsKey(pair.Key).Should().BeTrue("because {0} was one of the header values", pair.Key);
                result[pair.Key].Should().Be(pair.Value.Replace(@"""", String.Empty), "because the value for {0} should have been populated", pair.Key);
            }
        }
    }
}
