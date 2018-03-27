using System;

namespace OrderFulfillment.Core.Configuration
{
    /// <summary>
    ///   Serves as a factory for building strongly-typed configuration objects.
    /// </summary>
    /// 
    public interface IConfigurationFactory
    {
        /// <summary>
        ///   Creates an instance of the requested configuration type.
        /// </summary>
        /// 
        /// <typeparam name="T">The type of configuration requested.</typeparam>
        /// 
        /// <returns>An instance of the requested configuration type.</returns>
        /// 
        T Create<T>() where T : IConfiguration, new();

        /// <summary>
        ///   Creates an instance of the requested configuration type.
        /// </summary>
        /// 
        /// <param name="configurationType">The type configuration to create.</param>
        /// 
        /// <returns>An instance of the requested configuration type.</returns>
        /// 
        object Create(Type configurationType);
    }
}
