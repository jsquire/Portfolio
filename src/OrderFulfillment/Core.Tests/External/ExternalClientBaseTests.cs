using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using OrderFulfillment.Core.Exceptions;
using OrderFulfillment.Core.External;
using OrderFulfillment.Core.Infrastructure;
using Xunit;

namespace OrderFulfillment.Core.Tests.External
{
    /// <summary>
    ///   The suite of tests for the <see cref="ExternalClientBase" />
    ///   class.
    /// </summary>
    public class ExternalClientBaseTests
    {
        /// <summary>
        ///   Verifies behavior of the <see cref="ExternalClientBase.CreateHttpclient" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void CreateHttpClientDoesNotRequestACertificateWithoutAThumbprint()
        {
            var testBase = new CertificateTestBase();
            testBase.CreateHttpClient("http", "google.com", null, 30, 30);

            testBase.CertificateRequested.Should().BeFalse("because no thumbprint was passed");
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="ExternalClientBase.CreateHttpclient" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void CreateHttpClientCanParseAHostWithoutPort()
        {
            var host     = "dev.test.com";
            var testBase = new CertificateTestBase();
            var client   = testBase.CreateHttpClient("http", host, null, 30, 30);

            client.BaseAddress.Host.Should().Be(host, "because the host should have been used");
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="ExternalClientBase.CreateHttpclient" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void CreateHttpClientCanParseAHostWithPort()
        {
            var host     = "dev.test.com";
            var port     = 8030;
            var testBase = new CertificateTestBase();

            var client = testBase.CreateHttpClient("http", $"{ host }:{ port }", null, 30, 30);

            client.BaseAddress.Host.Should().Be(host, "because the host should have been used");
            client.BaseAddress.Port.Should().Be(port, "because the port should have been used");
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="ExternalClientBase.CreateHttpclient" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void CreateHttpClientCanParseAHostWithMalformedPort()
        {
            var host     = "dev.test.com";
            var testBase = new CertificateTestBase();

            var client = testBase.CreateHttpClient("http", $"{ host }:", null, 30, 30);

            client.BaseAddress.Host.Should().Be(host, "because the host should have been used");
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="ExternalClientBase.CreateHttpclient" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void CreateHttpClientRequestsACertificateWhenAThumbprintIsProvided()
        {
            var cert     = new X509Certificate2();
            var thumb    = "omg, hai1";
            var testBase = new CertificateTestBase(cert);
            testBase.CreateHttpClient("http", "google.com", thumb, 30, 30);

            testBase.CertificateRequested.Should().BeTrue("because the thumbprint was passed");
            testBase.RequestedThumbprint.Should().Be(thumb, "because the proper thumbprint should be used");
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="ExternalClientBase.CreateHttpclient" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void CreateHttpClientThrowsWhenNoCertificateIsFound()
        {
            var cert     = default(X509Certificate2);
            var thumb    = "omg, hai1";
            var testBase = new CertificateTestBase(cert);
            
            Action actionUnderTest = () => testBase.CreateHttpClient("http", "google.com", thumb, 30, 30);

            actionUnderTest.ShouldThrow<MissingDependencyException>("because the certificate was not found");
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="ExternalClientBase.CreateHttpclient" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void CreateHttpClientIsInitialized()
        {
            var cert     = new X509Certificate2();
            var thumb    = "omg, hai1";
            var timeout  = 30;
            var testBase = new CertificateTestBase(cert);
            
            using (var client = testBase.CreateHttpClient("http", "google.com", thumb, timeout, timeout))
            {
                client.Should().NotBeNull("because a client should have been created");
                client.BaseAddress.ShouldBeEquivalentTo(new Uri("http://google.com"), "because the protocol and base address should have been used");
                client.DefaultRequestHeaders.Accept.Should().HaveCount(1, "because only JSON should be accepted");
                client.DefaultRequestHeaders.Accept.First().MediaType.Should().Be(MimeTypes.Json, "because the JSON MIME type should be set");
                client.DefaultRequestHeaders.CacheControl?.NoStore.Should().BeTrue("because the client should not cache responses");
                client.Timeout.Should().Be(TimeSpan.FromSeconds(timeout), "because the timeout should have been set");

                var handlerField = typeof(HttpMessageInvoker).GetField("handler", BindingFlags.Instance | BindingFlags.NonPublic);
                var handler = (handlerField?.GetValue(client) as WebRequestHandler);

                handler.Should().NotBeNull("because the base handler for the client should hve been retrievable");
                handler.ClientCertificates.Should().HaveCount(1, "because only the requested client certificate should be associated with the handler");
                handler.ClientCertificates[0].Should().Be(cert, "because the found certificate should be associated with the client");
            }
        }

        #region Nested Classes

            private class CertificateTestBase : ExternalClientBase
            {
                public bool CertificateRequested;
                public string RequestedThumbprint;
                public X509Certificate2 ProvidedCertificate;

                public CertificateTestBase(X509Certificate2 testCert = null)
                {
                    this.ProvidedCertificate = testCert;
                }

                public new HttpClient CreateHttpClient(string requestProtocol, string hostServiceAddress, string clientCertificateThumbprint, int requestTimeoutSeconds, int connectionLeaseTimeoutSeconds) => 
                    base.CreateHttpClient(requestProtocol, hostServiceAddress, clientCertificateThumbprint, requestTimeoutSeconds, connectionLeaseTimeoutSeconds);

                protected override X509Certificate2 SearchForCertificate(string thumbprint, bool onlyRetrieveValidCertificate = true) 
                {
                    this.CertificateRequested = true;
                    this.RequestedThumbprint  = thumbprint;

                    return this.ProvidedCertificate;
                }
        }

        #endregion
    }
}
