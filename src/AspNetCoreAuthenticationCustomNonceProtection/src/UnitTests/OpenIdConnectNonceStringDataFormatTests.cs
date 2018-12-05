using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Moq;
using Moq.Protected;
using WebApp_OpenIDConnect_DotNet;
using Xunit;

namespace WebApp_OpenIDConnect_DotNet_Tests
{
    /// <summary>
    ///   The suite of unit tests for the <see cref="WebApp_OpenIDConnect_DotNet.OpenIdConnectNonceStringDataFormat"/>
    ///   class.
    /// </summary>
    /// 
    public class OpenIdConnectNonceStringDataFormatTests
    {
        /// <summary>
        ///   Verifies functionality of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorRequiresTheSerializer()
        {
            Action underTest = () => new OpenIdConnectNonceStringDataFormat(null, Mock.Of<IDataProtector>());
            underTest.Should().Throw<ArgumentNullException>("because the serializer is required");
        }

        /// <summary>
        ///   Verifies functionality of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorRequiresTheDataProtector()
        {
            Action underTest = () => new OpenIdConnectNonceStringDataFormat(Mock.Of<IDataSerializer<string>>(), null);
            underTest.Should().Throw<ArgumentNullException>("because the serializer is required");
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="OpenIdConnectNonceStringDataFormat.Protect"/>
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void ProtectInvokesTheDataProtector()
        {
            var data          = "ABSECSOASJERC";
            var expectedBytes = new byte[] { 0x11, 0x22, 0x12, 0x21 };
            var serializer    = new Mock<IDataSerializer<string>>();
            var protector     = new Mock<IDataProtector>();
            var formatter     = new OpenIdConnectNonceStringDataFormat(serializer.Object, protector.Object);

            serializer
                .Setup(instance => instance.Serialize(It.Is<string>(val => val == data)))
                .Returns(expectedBytes);

            formatter.Protect(data);
            protector.Verify(instance => instance.Protect(It.Is<byte[]>(val => val == expectedBytes)), Times.Once);
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="OpenIdConnectNonceStringDataFormat.Protect"/>
        ///   method.
        /// </summary>
        /// 
        [Theory]
        [InlineData("something", null)]
        [InlineData("otherthing", "purpose")]
        public void ProtectInvokesTheDataProtectorWhenAPurposeIsPassed(string data,
                                                                       string purpose)
        {
            var expectedBytes = new byte[] { 0x11, 0x22, 0x12, 0x21 };
            var serializer    = new Mock<IDataSerializer<string>>();
            var protector     = new Mock<IDataProtector>();
            var formatter     = new OpenIdConnectNonceStringDataFormat(serializer.Object, protector.Object);

            serializer
                .Setup(instance => instance.Serialize(It.Is<string>(val => val == data)))
                .Returns(expectedBytes);

            protector
                .Setup(instance => instance.CreateProtector(It.Is<string>(val => val == purpose)))
                .Returns(protector.Object);

            formatter.Protect(data, purpose);
            protector.Verify(instance => instance.Protect(It.Is<byte[]>(val => val == expectedBytes)), Times.Once);
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="OpenIdConnectNonceStringDataFormat.Protect"/>
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void ProtectInvokesTheSerializer()
        {
            var data          = "ABSECSOASJERC";
            var expectedBytes = new byte[] { 0x11, 0x22, 0x12, 0x21 };
            var serializer    = new Mock<IDataSerializer<string>>();
            var protector     = new Mock<IDataProtector>();
            var formatter     = new OpenIdConnectNonceStringDataFormat(serializer.Object, protector.Object);

            serializer
                .Setup(instance => instance.Serialize(It.Is<string>(val => val == data)))
                .Returns(expectedBytes);            

            formatter.Protect(data);
            serializer.Verify(instance => instance.Serialize(It.Is<string>(val => val == data)), Times.Once);
        }


        /// <summary>
        ///   Verifies functionality of the <see cref="OpenIdConnectNonceStringDataFormat.Protect"/>
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void ProtectInvokesTheEncoder()
        {
            var data       = "ABSECSOASJERC";
            var byteData   = new byte[] { 0x11, 0x22, 0x12, 0x21 };
            var serializer = new Mock<IDataSerializer<string>>();
            var protector  = new Mock<IDataProtector>();
            var formatter  = new Mock<OpenIdConnectNonceStringDataFormat>( serializer.Object, protector.Object) { CallBase = true };

            serializer
                .Setup(instance => instance.Serialize(It.Is<string>(val => val == data)))
                .Returns(byteData);     
                
            protector
                .Setup(instance => instance.Protect(It.Is<byte[]>(val => val == byteData)))
                .Returns(byteData);

            formatter.Object.Protect(data);
            formatter.Protected().Verify("EncodeProtectedData", Times.Once(), byteData);
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="OpenIdConnectNonceStringDataFormat.Unprotect"/>
        ///   method.
        /// </summary>
        /// 
        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void UnprotectTakesNoActionOnMissingData(string data)
        {
            var serializer = new Mock<IDataSerializer<string>>();
            var protector  = new Mock<IDataProtector>();
            var formatter  = new OpenIdConnectNonceStringDataFormat(serializer.Object, protector.Object);

            formatter.Unprotect(data).Should().Be(data, "because no action should be taken to unprotect missing data");
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="OpenIdConnectNonceStringDataFormat.Unprotect"/>
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void UnprotectInvokesTheDecoder()
        {
            var data       = "ABSECSOASJERC";            
            var serializer = new Mock<IDataSerializer<string>>();
            var protector  = new Mock<IDataProtector>();
            var formatter  = new Mock<OpenIdConnectNonceStringDataFormat>(serializer.Object, protector.Object) { CallBase = false };

            formatter
                .Protected()
                .Setup<byte[]>("DecodeProtectedData", ItExpr.Is<string>(val => val == data))                
                .Returns(default(byte[]))
                .Verifiable("No attempt was made to decode the protected data");

            formatter.Object.Unprotect(data);
            formatter.VerifyAll();
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="OpenIdConnectNonceStringDataFormat.Unprotect"/>
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void UnprotectDoexNotInvokeTheProtectorWhenDataIsNotDecoded()
        {
            var data       = "ADFASDF@#ASDFASDF";           
            var serializer = new Mock<IDataSerializer<string>>();
            var protector  = new Mock<IDataProtector>();
            var formatter  = new Mock<OpenIdConnectNonceStringDataFormat>(serializer.Object, protector.Object) { CallBase = false };

            formatter
                .Protected()
                .Setup<byte[]>("DecodeProtectedData", ItExpr.Is<string>(val => val == data))                
                .Returns(default(byte[]));

            formatter.Object.Unprotect(data).Should().Be(null, "because the data could not be decoded");
            protector.Verify(instance => instance.Unprotect(It.IsAny<byte[]>()), Times.Never, "data that was not decoded cannot be unprotected");
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="OpenIdConnectNonceStringDataFormat.Unprotect"/>
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void UnprotectInvokesTheDataProtector()
        {
            var data       = "ABSECSOASJERC";
            var dataBytes  = new byte[] { 0x11, 0x22, 0x12, 0x21 };
            var serializer = new Mock<IDataSerializer<string>>();
            var protector  = new Mock<IDataProtector>();
            var formatter  = new Mock<OpenIdConnectNonceStringDataFormat>(serializer.Object, protector.Object) { CallBase = false };

            formatter
                .Protected()
                .Setup<byte[]>("DecodeProtectedData", ItExpr.Is<string>(val => val == data))                
                .Returns(dataBytes);

            formatter.Object.Unprotect(data);
            protector.Verify(instance => instance.Unprotect(It.Is<byte[]>(val => val == dataBytes)), Times.Once);
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="OpenIdConnectNonceStringDataFormat.Unprotect"/>
        ///   method.
        /// </summary>
        /// 
        [Theory]
        [InlineData("something", null)]
        [InlineData("otherthing", "purpose")]
        public void UnprotectInvokesTheDataProtectorWhenAPurposeIsPassed(string data,
                                                                         string purpose)
        {
            var dataBytes  = new byte[] { 0x11, 0x22, 0x12, 0x21 };
            var serializer = new Mock<IDataSerializer<string>>();
            var protector  = new Mock<IDataProtector>();
            var formatter  = new Mock<OpenIdConnectNonceStringDataFormat>(serializer.Object, protector.Object) { CallBase = false };

            protector
                .Setup(instance => instance.CreateProtector(It.Is<string>(val => val == purpose)))
                .Returns(protector.Object);
                
            formatter
                .Protected()
                .Setup<byte[]>("DecodeProtectedData", ItExpr.Is<string>(val => val == data))                
                .Returns(dataBytes);

            formatter.Object.Unprotect(data, purpose);
            protector.Verify(instance => instance.Unprotect(It.Is<byte[]>(val => val == dataBytes)), Times.Once);
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="OpenIdConnectNonceStringDataFormat.Unprotect"/>
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void UnprotectDoesNotInvokesTheSerializerWhenDataIsNotUnprotected()
        {
            var data       = "ABSECSOASJERC";
            var dataBytes  = new byte[] { 0x11, 0x22, 0x12, 0x21 };
            var serializer = new Mock<IDataSerializer<string>>();
            var protector  = new Mock<IDataProtector>();
            var formatter  = new Mock<OpenIdConnectNonceStringDataFormat>(serializer.Object, protector.Object) { CallBase = false };

            protector
                .Setup(instance => instance.Unprotect(It.Is<byte[]>(val => val == dataBytes)))
                .Returns((byte[])null)
                .Verifiable("The data protector should have been invoked with the decoded data");

            formatter
                .Protected()
                .Setup<byte[]>("DecodeProtectedData", ItExpr.Is<string>(val => val == data))                
                .Returns(dataBytes);

            formatter.Object.Unprotect(data).Should().Be(null, "because data that cannot be unprotected should result in no return value");
            serializer.Verify(instance => instance.Deserialize(It.IsAny<byte[]>()), Times.Never, "No attempt to deserialize should be made for data that cannot be unprotected");
            protector.VerifyAll();
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="OpenIdConnectNonceStringDataFormat.Unprotect"/>
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void UnprotectInvokesTheDataSerializer()
        {
            var data             = "ABSECSOASJERC";
            var dataBytes        = new byte[] { 0x11, 0x22, 0x12, 0x21 };
            var unprotectedBytes = new byte[] { 0x21, 0x32, 0x22, 0x31 };
            var serializer       = new Mock<IDataSerializer<string>>();
            var protector        = new Mock<IDataProtector>();
            var formatter        = new Mock<OpenIdConnectNonceStringDataFormat>(serializer.Object, protector.Object) { CallBase = false };

            protector
                .Setup(instance => instance.Unprotect(It.Is<byte[]>(val => val == dataBytes)))
                .Returns(unprotectedBytes);

            formatter
                .Protected()
                .Setup<byte[]>("DecodeProtectedData", ItExpr.Is<string>(val => val == data))                
                .Returns(dataBytes);

            formatter.Object.Unprotect(data);
            serializer.Verify(instance => instance.Deserialize(It.Is<byte[]>(val => val == unprotectedBytes)), Times.Once, "The unprotected data should have been deserialized");
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="OpenIdConnectNonceStringDataFormat.Unprotect"/>
        ///   method.
        /// </summary>
        ///  
        [Fact]
        public void UnprotectReturnsTheDeserializedData()
        {
            var data             = "ABSECSOASJERC";
            var expected         = "This is totally not the same as the data at all";
            var dataBytes        = new byte[] { 0x11, 0x22, 0x12, 0x21 };
            var unprotectedBytes = new byte[] { 0x21, 0x32, 0x22, 0x31 };
            var serializer       = new Mock<IDataSerializer<string>>();
            var protector        = new Mock<IDataProtector>();
            var formatter        = new Mock<OpenIdConnectNonceStringDataFormat>(serializer.Object, protector.Object) { CallBase = false };

            serializer
                .Setup(instance => instance.Deserialize(It.Is<byte[]>(val => val == unprotectedBytes)))
                .Returns(expected);

            protector
                .Setup(instance => instance.Unprotect(It.Is<byte[]>(val => val == dataBytes)))
                .Returns(unprotectedBytes);

            formatter
                .Protected()
                .Setup<byte[]>("DecodeProtectedData", ItExpr.Is<string>(val => val == data))                
                .Returns(dataBytes);

            formatter.Object.Unprotect(data).Should().Be(expected, "because the result of unprotecting should be the deserialized unprotected data");
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="OpenIdConnectNonceStringDataFormat.Protect"/> and 
        ///   <see cref="OpenIdConnectNonceStringDataFormat.Unprotect"/> methods.
        /// </summary>
        /// 
        [Fact]
        public void ProtectedDataCanBeUnprotected()
        {
            var data           = "ABSECSOASJERC";
            var byteData       = Encoding.UTF8.GetBytes(data);
            var protectedBytes = Encoding.UTF8.GetBytes("This is a protected string");
            var serializer     = new Mock<IDataSerializer<string>>();
            var protector      = new Mock<IDataProtector>();
            var formatter      = new OpenIdConnectNonceStringDataFormat(serializer.Object, protector.Object);

            serializer
                .Setup(instance => instance.Serialize(It.Is<string>(val => val == data)))
                .Returns(byteData)
                .Verifiable();

            serializer
                .Setup(instance => instance.Deserialize(It.Is<byte[]>(val => Enumerable.SequenceEqual(val, byteData))))
                .Returns(data)
                .Verifiable();

            protector
                .Setup(instance => instance.Protect(It.Is<byte[]>(val => val == byteData)))
                .Returns(protectedBytes)
                .Verifiable();

            protector
                .Setup(instance => instance.Unprotect(It.Is<byte[]>(val => Enumerable.SequenceEqual(val, protectedBytes))))
                .Returns(byteData)
                .Verifiable();

            var protectedData = formatter.Protect(data);
            protectedData.Should().NotBeNullOrEmpty("because the data should have been protected");

            formatter.Unprotect(protectedData).Should().Be(data, "because a protect/unprotect cycle should result in the source data being restored");
            serializer.VerifyAll();
            protector.VerifyAll();
            
            
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="OpenIdConnectNonceStringDataFormat.Protect"/> methods to
        ///   ensure that the resulting protected value does not violdate the OWSAP CRS v3 rule (#942440) for detecting
        ///   a possible SQL injection attack.
        /// </summary>
        /// 
        /// <seealso cref="l:https://github.com/SpiderLabs/owasp-modsecurity-crs/blob/v3.2/dev/rules/REQUEST-942-APPLICATION-ATTACK-SQLI.conf" />
        /// 
        [Theory]
        [InlineData(" OR 1# ")]
        [InlineData(" DROP sampletable;-- ")]
        [InlineData(" admin'-- ")]
        [InlineData(" DROP/*comment*/sampletable ")]
        [InlineData(" DR/**/OP/*bypass blacklisting*/sampletable ")]
        [InlineData(" SELECT/*avoid-spaces*/password/**/FROM/**/Members ")]
        [InlineData(" SELECT /*!32302 1/0, */ 1 FROM tablename ")]
        [InlineData(" ' or 1=1# ")]
        [InlineData(" ' or 1=1-- - ")]
        [InlineData(" ' or 1=1/* ")]
        [InlineData(" ' or 1=1;\x00 ")]
        [InlineData(" 1='1' or-- - ")]
        [InlineData(" ' /*!50000or*/1='1 ")]
        [InlineData(" ' /*!or*/1='1 ")]
        [InlineData(" 0/**/union/*!50000select*/table_name`foo`/**/ ")]
        public void ProtectedDataDoesNotViolateOwaspRule942440(string data)
        {                   
            var byteData         = Encoding.UTF8.GetBytes(data);
            var violationPattern = new Regex("(/\\*!?|\\*/|[';]--|--[\\s\\r\\n\\v\\f]|(?:--[^-]*?-)|([^\\-&])#.*?[\\s\\r\\n\\v\\f]|;?\\x00)", RegexOptions.ECMAScript);            
            var serializer       = new Mock<IDataSerializer<string>>();
            var protector        = new Mock<IDataProtector>();
            var formatter        = new OpenIdConnectNonceStringDataFormat(serializer.Object, protector.Object);

            // Use the raw byte data as the protected result to, hopefully, increase the chances that
            // it would potentially result in an invalid sequence.

            serializer
                .Setup(instance => instance.Serialize(It.Is<string>(val => val == data)))
                .Returns(byteData)
                .Verifiable();

            protector
                .Setup(instance => instance.Protect(It.Is<byte[]>(val => val == byteData)))
                .Returns(byteData)
                .Verifiable();

            var protectedData = formatter.Protect(data);
            protectedData.Should().NotBeNullOrEmpty("because the data should have been protected");
            violationPattern.IsMatch(protectedData).Should().BeFalse("because the data should not violate the rule");
        }
    }
}
