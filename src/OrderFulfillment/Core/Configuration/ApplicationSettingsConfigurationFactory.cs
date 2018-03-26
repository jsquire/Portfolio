using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;

namespace OrderFulfillment.Core.Configuration
{
    /// <summary>
    ///   Serves as a factory for strongly-typed configuraiton, using the <see cref="System.Configuration.ConfigurationManager"/>
    ///   against the application's settings, following a named convention.
    /// </summary>
    /// 
    /// <seealso cref="OrderFulfillment.Core.Configuration.IConfigurationFactory" />
    /// 
    public class ApplicationSettingsConfigurationFactory : IConfigurationFactory
    {
        /// <summary>The suffix on the name of a type to be removed for settings key matches.</summary>
        private const string ConfigurationSuffix = "configuration";
        
        /// <summary>
        ///   Creates an instance of the requested configuration type.
        /// </summary>
        /// 
        /// <typeparam name="T">The type of configuration requested.</typeparam>
        /// 
        /// <returns>An instance of the requested configuration type.</returns>
        /// 
        public T Create<T>() where T : IConfiguration, new()
        {
           return (T)this.Create(typeof(T));
        }

        /// <summary>
        ///   Creates an instance of the requested configuration type.
        /// </summary>
        /// 
        /// <param name="configurationType">The type configuration to create.</param>
        /// 
        /// <returns>An instance of the requested configuration type.</returns>
        /// 
        public object Create(Type configurationType)
        {           
           var prefix       = this.GetSettingsPrefix(configurationType.Name);
           var prefixLength = prefix.Length;
           var flags        = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;
           var instance     = Activator.CreateInstance(configurationType);

           foreach (var settingKey in this.GetAppSettingsKeys().Where(key => key.StartsWith(prefix)))
           {
               var property = configurationType.GetProperty(settingKey.Substring(prefixLength), flags);

               if (property != null)
               {
                   this.TrySetValue(instance, property, this.GetSetting(settingKey));
               }
           }

           return instance;
        }

        /// <summary>
        ///   Gets the set of keys available in the application settings.
        /// </summary>
        /// 
        /// <returns>The set of application setttings keys.</returns>
        /// 
        protected virtual IEnumerable<string> GetAppSettingsKeys() =>
            ConfigurationManager.AppSettings.AllKeys;

        /// <summary>
        ///   Gets a specific application settting.
        /// </summary>
        /// 
        /// <param name="key">The key for the setting to retrieve</param>
        /// 
        /// <returns>The application setting associated with the given <paramref name="key" /></returns>
        /// 
        protected virtual string GetSetting(string key) =>
            ConfigurationManager.AppSettings[key]; 

        /// <summary>
        ///  Gets the settings prefix for the given type name.
        /// </summary>
        /// 
        /// <param name="typeName">Name of the type to form the settings prefix for.</param>
        /// 
        /// <returns>The prefix to use for discovering application settings for the type.</returns>
        /// 
        private string GetSettingsPrefix(string typeName)
        {
            if (String.IsNullOrWhiteSpace(typeName))
            {
                return typeName;
            }

            var type = typeName.ToLowerInvariant().EndsWith(ApplicationSettingsConfigurationFactory.ConfigurationSuffix)
                ? typeName.Substring(0, (typeName.Length - ApplicationSettingsConfigurationFactory.ConfigurationSuffix.Length))
                : typeName;

            return $"{ type }.";
        }

        /// <summary>
        ///   Attempts to set a property value, ensuring the proper destination
        ///   type conversion.
        /// </summary>
        /// 
        /// <param name="target">The target instance to set the property on.</param>
        /// <param name="propertyInfo">The reflective property information to use for setting the property.</param>
        /// <param name="value">The value to set, in string form.</param>
        /// 
        /// <returns><c>true</c> if the set was successful; otherwise, <c>false</c></returns>
        /// 
        private bool TrySetValue(object       target, 
                                 PropertyInfo propertyInfo, 
                                 string       value)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (propertyInfo == null)
            {
                throw new ArgumentNullException(nameof(propertyInfo));
            }

            object convertedValue;

            var propertyType = propertyInfo.PropertyType;

            if ((propertyType == typeof(object)) || (propertyType == typeof(string)))
            {
                convertedValue = value;
            }
            else if ((propertyType == typeof(bool)) || (propertyType == typeof(bool?)))
            {
                if (bool.TryParse(value, out bool typedValue))
                {
                    convertedValue = typedValue;
                }
                else
                {
                    return false;
                }
            }
            else if ((propertyType == typeof(short)) || (propertyType == typeof(short?)))
            {
                if (short.TryParse(value, out short typedValue))
                {
                    convertedValue = typedValue;
                }
                else
                {
                    return false;
                }
            }
            else if ((propertyType == typeof(ushort)) || (propertyType == typeof(ushort?)))
            {
                if (ushort.TryParse(value, out ushort typedValue))
                {
                    convertedValue = typedValue;
                }
                else
                {
                    return false;
                }
            }
            else if ((propertyType == typeof(int)) || (propertyType == typeof(int?)))
            {
                if (int.TryParse(value, out int typedValue))
                {
                    convertedValue = typedValue;
                }
                else
                {
                    return false;
                }
            }
            else if ((propertyType == typeof(uint)) || (propertyType == typeof(uint?)))
            {
                if (uint.TryParse(value, out uint typedValue))
                {
                    convertedValue = typedValue;
                }
                else
                {
                    return false;
                }
            }
            else if ((propertyType == typeof(double)) || (propertyType == typeof(double?)))
            {
                if (double.TryParse(value, out double typedValue))
                {
                    convertedValue = typedValue;
                }
                else
                {
                    return false;
                }
            }
            else if ((propertyType == typeof(float)) || (propertyType == typeof(float?)))
            {
                if (float.TryParse(value, out float typedValue))
                {
                    convertedValue = typedValue;
                }
                else
                {
                    return false;
                }
            }
            else if ((propertyType.IsEnum) && (Enum.IsDefined(propertyType, value)))
            {
                convertedValue = Enum.Parse(propertyType, value, true);
            }
            else
            {
                return false;
            }

            propertyInfo.SetValue(target, convertedValue);
            return true;
        }
    }
}
