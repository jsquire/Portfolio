using System.Reflection;
using System.Runtime.CompilerServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.

[assembly: AssemblyCompany("Jesse Squire")]
[assembly: AssemblyProduct("Order Fulfillment Sample")]
[assembly: AssemblyCopyright("Copyright © Jesse Squire")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyInformationalVersion("0.0.1-devbuild-local")]

// Allow internals to be visible to test projects.

[assembly: InternalsVisibleTo("OrderFulfillment.Core.Tests")]
[assembly: InternalsVisibleTo("OrderFulfillment.Api.Tests")]
[assembly: InternalsVisibleTo("OrderFulfillment.OrderProcessor.Tests")]
[assembly: InternalsVisibleTo("OrderFulfillment.OrderSubmitter.Tests")]
[assembly: InternalsVisibleTo("OrderFulfillment.Notifier.Tests")]

#if DEBUG
[assembly: AssemblyConfiguration("DEBUG")]
#else
[assembly: AssemblyConfiguration("RELEASE")]
#endif