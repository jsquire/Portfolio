using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using OrderFulfillment.Core.Configuration;
using Moq;
using Moq.Protected;
using Xunit;

namespace OrderFulfillment.Core.Tests.Configuration
{
    /// <summary>
    ///   The suite of tests for the <see cref="ApplicationSettingsConfigurationFactory" />
    ///   class.
    /// </summary>
    /// 
    public class ApplicationSettingsConfigurationFactoryTests
    {
        /// <summary>
        ///   Verifies behavior of the <see cref="OrderFulfillment.Core.Configuration.ApplicationSettingsConfigurationFactory.Create{T}" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void CreateWithEmptyConfiguration()
        {
            var factory = new ApplicationSettingsConfigurationFactory();
            var result  = factory.Create<EmptyConfiguration>();
            
            result.Should().NotBeNull("because the instance should have been created");            
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="OrderFulfillment.Core.Configuration.ApplicationSettingsConfigurationFactory.Create{T}" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void CreateWithMissingSimpleConfiguration()
        {
            var factory = new ApplicationSettingsConfigurationFactory();
            var result  = factory.Create<SimpleConfiguration>();
            
            result.Should().NotBeNull("because the instance should have been created");
            result.Item.Should().BeNull("becaue there was no matching application setting");
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="OrderFulfillment.Core.Configuration.ApplicationSettingsConfigurationFactory.Create{T}" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void CreateWithMissingPrefix()
        {
            var factory = new Mock<ApplicationSettingsConfigurationFactory>();

            factory.Protected()
                   .Setup<IEnumerable<string>>("GetAppSettingsKeys")
                   .Returns(new[] { "item" });

            factory.Protected()
                   .Setup<string>("GetSetting", false, ItExpr.Is<string>(key => key.ToLowerInvariant() == "item"))
                   .Returns("Something!");


            var result = factory.Object.Create<SimpleConfiguration>();
            
            result.Should().NotBeNull("because the instance should have been created");
            result.Item.Should().BeNull("becaue the setting lacks the expected prefix");
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="OrderFulfillment.Core.Configuration.ApplicationSettingsConfigurationFactory.Create{T}" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void CreateWithEmptyValue()
        {
            var factory = new Mock<ApplicationSettingsConfigurationFactory>();

            factory.Protected()
                   .Setup<IEnumerable<string>>("GetAppSettingsKeys")
                   .Returns(new[] { "Simple.item" });

            factory.Protected()
                   .Setup<string>("GetSetting", false, ItExpr.Is<string>(key => key.ToLowerInvariant() == "simple.item"))
                   .Returns(String.Empty);


            var result = factory.Object.Create<SimpleConfiguration>();
            
            result.Should().NotBeNull("because the instance should have been created");
            result.Item.Should().Be(String.Empty, "because the setting was empty");
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="OrderFulfillment.Core.Configuration.ApplicationSettingsConfigurationFactory.Create{T}" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void CreateWithNullValue()
        {
            var factory = new Mock<ApplicationSettingsConfigurationFactory>();

            factory.Protected()
                   .Setup<IEnumerable<string>>("GetAppSettingsKeys")
                   .Returns(new[] { "Simple.item" });

            factory.Protected()
                   .Setup<string>("GetSetting", false, ItExpr.Is<string>(key => key.ToLowerInvariant() == "simple.item"))
                   .Returns((string)null);


            var result = factory.Object.Create<SimpleConfiguration>();
            
            result.Should().NotBeNull("because the instance should have been created");
            result.Item.Should().Be(null, "because the setting was empty");
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="OrderFulfillment.Core.Configuration.ApplicationSettingsConfigurationFactory.Create{T}" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void CreateWithSimpleConfigurationGeneric()
        {
            var factory  = new Mock<ApplicationSettingsConfigurationFactory>();
            var expected = "TheValue!";

            factory.Protected()
                   .Setup<IEnumerable<string>>("GetAppSettingsKeys")
                   .Returns(new[] { "Simple.Item" });

            factory.Protected()
                   .Setup<string>("GetSetting",false , ItExpr.Is<string>(key => key.ToLowerInvariant() == "simple.item"))
                   .Returns(expected);


            var result = factory.Object.Create<SimpleConfiguration>();
            
            result.Should().NotBeNull("because the instance should have been created");
            result.Item.Should().Be(expected, "becaue the application setting should have been returned");
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="OrderFulfillment.Core.Configuration.ApplicationSettingsConfigurationFactory.Create{T}" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void CreateWithSimpleConfigurationTypeName()
        {
            var factory  = new Mock<ApplicationSettingsConfigurationFactory>();
            var expected = "TheValue!";

            factory.Protected()
                   .Setup<IEnumerable<string>>("GetAppSettingsKeys")
                   .Returns(new[] { "Simple.Item" });

            factory.Protected()
                   .Setup<string>("GetSetting",false , ItExpr.Is<string>(key => key.ToLowerInvariant() == "simple.item"))
                   .Returns(expected);


            var result = factory.Object.Create(typeof(SimpleConfiguration)) as SimpleConfiguration;
            
            result.Should().NotBeNull("because the instance should have been created");
            result.Item.Should().Be(expected, "becaue the application setting should have been returned");
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="OrderFulfillment.Core.Configuration.ApplicationSettingsConfigurationFactory.Create{T}" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void CreateWithPrimitiveConfigurationGeneric()
        {
            var factory  = new Mock<ApplicationSettingsConfigurationFactory>();

            var expected = new PrimitiveTypesConfiguration
            {
                Text             = "Some text!",
                Boolean          = true,
                NullableBoolean  = false,
                Double           = 2.4,
                NullableDouble   = -9.1,
                Integer          = -3,
                NullableInteger  = 42,
                UInteger         = 43,
                NullableUInteger = 44,
                Short            = -12,
                NullableShort    = 13,
                UShort           = 14,
                NullableUShort   = 15,
                Float            = -1.8f,
                NullableFloat    = 2.3f,
                DayOfWeek        = DayOfWeek.Monday
            };

            var settings = typeof(PrimitiveTypesConfiguration)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .ToDictionary(property => $"PrimitiveTypes.{ property.Name }", property => property.GetValue(expected)?.ToString());


            Func<string, string> getSetting = key => settings[key];

            factory.Protected()
                   .Setup<IEnumerable<string>>("GetAppSettingsKeys")
                   .Returns(settings.Keys);            

            factory.Protected()
                   .Setup<string>("GetSetting",false , ItExpr.IsAny<string>())
                   .Returns(getSetting);


            var result = factory.Object.Create<PrimitiveTypesConfiguration>();
            
            result.Should().NotBeNull("because the instance should have been created");
            result.ShouldBeEquivalentTo(expected, "becaue the settings should have been created and the primitive types parsed");
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="OrderFulfillment.Core.Configuration.ApplicationSettingsConfigurationFactory.Create{T}" />
        ///   method.
        /// </summary>
        /// 
        [Fact]
        public void CreateWithPrimitiveConfigurationTypeName()
        {
            var factory  = new Mock<ApplicationSettingsConfigurationFactory>();

            var expected = new PrimitiveTypesConfiguration
            {
                Text             = "Some text!",
                Boolean          = true,
                NullableBoolean  = false,
                Double           = 2.4,
                NullableDouble   = -9.1,
                Integer          = -3,
                NullableInteger  = 42,
                UInteger         = 43,
                NullableUInteger = 44,
                Short            = -12,
                NullableShort    = 13,
                UShort           = 14,
                NullableUShort   = 15,
                Float            = -1.8f,
                NullableFloat    = 2.3f,
                DayOfWeek        = DayOfWeek.Monday
            };

            var settings = typeof(PrimitiveTypesConfiguration)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .ToDictionary(property => $"PrimitiveTypes.{ property.Name }", property => property.GetValue(expected)?.ToString());


            Func<string, string> getSetting = key => settings[key];

            factory.Protected()
                   .Setup<IEnumerable<string>>("GetAppSettingsKeys")
                   .Returns(settings.Keys);            

            factory.Protected()
                   .Setup<string>("GetSetting",false , ItExpr.IsAny<string>())
                   .Returns(getSetting);


            var result = factory.Object.Create(typeof(PrimitiveTypesConfiguration));
            
            result.Should().NotBeNull("because the instance should have been created");
            result.ShouldBeEquivalentTo(expected, "becaue the settings should have been created and the primitive types parsed");
        }


        #region Nested Classes
            protected class EmptyConfiguration : IConfiguration
            {
            }

            protected class SimpleConfiguration : IConfiguration
            {
                public string Item { get;  set; }
            }

            protected class PrimitiveTypesConfiguration : IConfiguration
            {
                public string Text
                {
                    get; set;
                }
                public int Integer
                {
                    get; set;
                }
                public int? NullableInteger
                {
                    get; set;
                }
                public int UInteger
                {
                    get; set;
                }
                public int? NullableUInteger
                {
                    get; set;
                }
                public bool Boolean
                {
                    get; set;
                }
                public bool? NullableBoolean
                {
                    get; set;
                }
                public short Short
                {
                    get; set;
                }
                public short? NullableShort
                {
                    get; set;
                }
                public ushort UShort
                {
                    get; set;
                }
                public ushort? NullableUShort
                {
                    get; set;
                }
                public double Double
                {
                    get; set;
                }
                public double? NullableDouble
                {
                    get; set;
                }
                public float Float
                {
                    get; set;
                }
                public float? NullableFloat
                {
                    get; set;
                }
                public DayOfWeek DayOfWeek
                {
                    get; set;
                }
            }
        #endregion
    }
}
