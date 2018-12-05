using System;
using FluentAssertions;
using WebApp_OpenIDConnect_DotNet;
using Xunit;

namespace WebApp_OpenIDConnect_DotNet_Tests
{
    /// <summary>
    ///     The suite of tests for the <see cref="WebApp_OpenIDConnect_DotNet.Utf8ByteSerializer" />
    ///     class.
    /// </summary>
    /// 
    public class Utf8ByteSerializerTests
    {
        /// <summary>
        ///   Verifies functionality of the <see cref="WebApp_OpenIDConnect_DotNet.Utf8ByteSerializer.Serialize" />
        ///   method.
        /// </summary>
        /// 
        [Theory]
        [InlineData("1")]
        [InlineData("Some")]
        [InlineData("A very long string of data")]
        public void StringDataCanBeSerialized(string data)
        {
            var serialized = new Utf8ByteSerializer().Serialize(data);
            
            serialized.Should().NotBeNull("because serialization should produce a result");
            serialized.Should().HaveCountGreaterOrEqualTo(1, "because seiralization should produce at least a byte");
            serialized.Should().HaveCountLessOrEqualTo(data.Length * 2, "because serialization should take no more than 2 bytes per character in UTF-8");
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="WebApp_OpenIDConnect_DotNet.Utf8ByteSerializer.Serialize" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void SerializingNullFails()
        {
            Action underTest = () => new Utf8ByteSerializer().Serialize(null);            
            underTest.Should().Throw<ArgumentNullException>("because a null string cannot be serialized");
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="WebApp_OpenIDConnect_DotNet.Utf8ByteSerializer.Serialize" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void SerializingAnEmptyStringProducesEmptyResults()
        {
            var serialized = new Utf8ByteSerializer().Serialize(String.Empty);            
            serialized.Should().NotBeNull("because serialization should produce a result");
            serialized.Should().HaveCount(0, "because an empty string should produce an empty byte sequence.");
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="WebApp_OpenIDConnect_DotNet.Utf8ByteSerializer.Serialize" />
        ///   method.
        /// </summary>
        /// 
        [Theory]
        [InlineData("1")]
        [InlineData("Some")]
        [InlineData("A very long string of data")]
        public void SerializedValuesCanBeDeserialized(string data)
        {
            var serializer   = new Utf8ByteSerializer();
            var serialized   = serializer.Serialize(data);
            var deserialized = serializer.Deserialize(serialized);
            
            serialized.Should().NotBeNull("because serialization should produce a result");
            serialized.Should().HaveCountGreaterThan(0, "because serialziation should not produce an empty byte sequence.");
            deserialized.Should().Be(data, "because serialization should deserialize to the same value");
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="WebApp_OpenIDConnect_DotNet.Utf8ByteSerializer.Serialize" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void DeserializingNullFails()
        {
            Action underTest = () => new Utf8ByteSerializer().Deserialize(null);            
            underTest.Should().Throw<ArgumentNullException>("because a null string cannot be deserialized");
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="WebApp_OpenIDConnect_DotNet.Utf8ByteSerializer.Serialize" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void DeserializingAnEmptySequenceProducesAnEmptyString()
        {
            var deserialized = new Utf8ByteSerializer().Deserialize(new byte[0]);            
            deserialized.Should().NotBeNull("because deserialization should produce a result");
            deserialized.Should().Be(String.Empty, "because an empty sequence should produce and empty string");
        }
    }
}
