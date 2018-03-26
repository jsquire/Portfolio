using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace OrderFulfillment.Api.RouteConstraints
{
    /// <summary>
    ///   Performs discovery of the types identified as route constraints in the current assembly.
    /// </summary>
    /// 
    public class RouteConstraintLocator
    {
        /// <summary>The set of all route constraints in the target assembly.</summary>
        private readonly Lazy<Dictionary<string, Type>> discoveredConstraints;

        /// <summary>
        ///   The set of all route constraints in the target assembly.
        /// </summary>
        public Dictionary<string, Type> DiscoveredConstraints
        {
            get
            {
                return this.discoveredConstraints.Value;
            }
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="RouteConstraintLocator"/> class.
        /// </summary>
        /// 
        public RouteConstraintLocator() : this(typeof(RouteConstraintLocator).Assembly)
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="RouteConstraintLocator"/> class.
        /// </summary>
        /// 
        /// <param name="targetAssembly">The assembly to use for discovery of route constraints.</param>
        /// 
        public RouteConstraintLocator(Assembly targetAssembly) : base()
        {
            if (targetAssembly == null)
            {
                throw new ArgumentNullException(nameof(targetAssembly));
            }

            this.discoveredConstraints = new Lazy<Dictionary<string, Type>>( () => this.DiscoverConstraints(targetAssembly), LazyThreadSafetyMode.PublicationOnly);
        }

        /// <summary>
        ///   Discovers types that are decorated to indicate that they wish to be registered as a Web API
        ///   route constraint.
        /// </summary>
        /// 
        /// <param name="assembly">The assembly to consider for discovery.</param>
        /// 
        /// <returns>A set of all route constraints that were discovered.</returns>
        /// 
        private Dictionary<string, Type> DiscoverConstraints(Assembly assembly)
        {
             
             return assembly.GetTypes()
                            .SelectMany(type => type.GetCustomAttributes<RouteConstraintAttribute>().Select(attr => Tuple.Create(attr.ConstraintName, type)))
                            .ToDictionary(tuple => tuple.Item1, tuple => tuple.Item2);
        }
    }
}