using System;
using Autofac;
using Microsoft.Azure.WebJobs.Host;

namespace OrderFulfillment.Core.WebJobs
{
    /// <summary>
    ///   Provides an activator for WebJob constructs backed by an AutoFac
    ///   container.
    /// </summary>
    /// 
    /// <seealso cref="Microsoft.Azure.WebJobs.Host.IJobActivator" />
    /// 
    public class AutoFacWebJobActivator : IJobActivator
    {
        /// <summary>The container to use for resolving dependencies.</summary>
        private readonly IContainer container;

        /// <summary>
        ///   Initializes a new instance of the <see cref="AutoFacWebJobActivator" /> class.
        /// </summary>
        /// 
        /// <param name="container">The AutoFac container to use for resolving dependencies.</param>
        ///
        public AutoFacWebJobActivator(IContainer container)
        {
            this.container = container ?? throw new ArgumentNullException(nameof(container));
        }

        /// <summary>
        ///   Creates a new instance of a job type.
        /// </summary>
        /// 
        /// <typeparam name="T">The job type.</typeparam>
        /// 
        /// <returns>A new instance of the job type.</returns>
        ///
        public T CreateInstance<T>() => this.container.BeginLifetimeScope().Resolve<T>();
    }
}
