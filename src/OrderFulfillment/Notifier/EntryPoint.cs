using System;
using Autofac;
using Autofac.Core;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.ServiceBus;
using OrderFulfillment.Core.Commands;
using OrderFulfillment.Core.Configuration;
using OrderFulfillment.Core.Events;
using OrderFulfillment.Core.Logging;
using OrderFulfillment.Core.WebJobs;
using OrderFulfillment.Notifier.Configuration;
using OrderFulfillment.Notifier.Infrastructure;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using NodaTime;
using Serilog;
using Serilog.Events;
using Serilog.ExtensionMethods;

namespace OrderFulfillment.Notifier
{
    /// <summary>
    ///   Serves at the entry point for bootstrapping the WebJob host and performing 
    ///   any needed global configuration and registration.
    /// </summary>
    /// 
    public class EntryPoint
    {
        /// <summary>
        ///   Serves as the main entry point for the application.
        /// </summary>
        /// 
        public static void Main()
        {
            var container            = EntryPoint.CreateDependencyResolver();
            var notifierConfiguration = container.Resolve<NotifierJobHostConfiguration>();            

            // Configure logging

            EntryPoint.UseApplicationInsights(container.Resolve<LoggingConfiguration>(), TelemetryConfiguration.Active); 

            // Configure the Job Host

            var nameSpace = typeof(EntryPoint).Namespace;

            var hostConfiguration = new JobHostConfiguration
            {
               HostId                    = nameSpace.Substring(nameSpace.LastIndexOf('.') + 1).ToLower(), 
               DashboardConnectionString = notifierConfiguration.DashboardConnectionString,
               StorageConnectionString   = notifierConfiguration.StorageConnectionString,
               JobActivator              = new AutoFacWebJobActivator(container)
            };

            var serviceBusConfig = new ServiceBusConfiguration
            {
               ConnectionString = notifierConfiguration.ServiceBusConnectionString,
               MessageOptions   = { AutoComplete = false }
            };
            
            hostConfiguration.Tracing.Tracers.Add(new SerilogWebJobTraceWriter(container.Resolve<ILogger>()));
            hostConfiguration.UseServiceBus(serviceBusConfig);
            
            if (hostConfiguration.IsDevelopment)
            {
                hostConfiguration.UseDevelopmentSettings();
            }

            // Start the host

            var host = new JobHost(hostConfiguration);           
            host.RunAndBlock();
        }

        /// <summary>
        ///   Performs the tasks needed to configure dependency injection for use with Web API
        ///   and other consumers of the HTTP configuration.
        /// </summary>
        /// 
        /// <param name="config">The HTTP configuration to be used for DI configuration.</param>
        /// 
        internal static IContainer CreateDependencyResolver(Func<IConfigurationFactory> createConfigurationFactoryDelegate = null)
        {            
            var orderSubmitterAssembly = typeof(EntryPoint).Assembly;
            var coreAssembly           = typeof(IConfigurationFactory).Assembly;
            var builder                = new ContainerBuilder();
                        
            // Infrastructure dependencies

            var serializerSettings = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            serializerSettings.Converters.Add(new StringEnumConverter());

            var serializer = new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            serializer.Converters.Add(new StringEnumConverter());

            builder
                .RegisterInstance(serializer)
                .AsSelf()
                .SingleInstance();

            builder
                .RegisterInstance(serializerSettings)
                .AsSelf()
                .SingleInstance();

            builder
                .RegisterInstance(SystemClock.Instance)
                .AsImplementedInterfaces();

            // Configuration dependencies

            builder
                .RegisterInstance(createConfigurationFactoryDelegate?.Invoke() ?? new ApplicationSettingsConfigurationFactory())                
                .AsImplementedInterfaces()
                .SingleInstance();
                            
            builder
                .RegisterAssemblyTypes(orderSubmitterAssembly, coreAssembly)
                .AssignableTo<IConfiguration>()
                .AsSelf()
                .OnActivating(args => args.ReplaceInstance(args.Context.Resolve<IConfigurationFactory>().Create(args.Instance.GetType())));

            builder
                .RegisterGeneric(typeof(ServiceBusQueueCommandPublisherConfiguration<>))
                .AsSelf()
                .OnActivating(args => args.ReplaceInstance(args.Context.Resolve<IConfigurationFactory>().Create(ServiceBusQueueCommandPublisherConfiguration<object>.GetSpecificConfigurationType(args.Instance.GetType()))));                

            builder
                .RegisterGeneric(typeof(ServiceBusTopicEventPublisherConfiguration<>))
                .AsSelf()
                .OnActivating(args => args.ReplaceInstance(args.Context.Resolve<IConfigurationFactory>().Create(ServiceBusTopicEventPublisherConfiguration<object>.GetSpecificConfigurationType(args.Instance.GetType()))));

            // Logging dependencies

            var logBuilder = new LoggerConfiguration()
                .WriteTo.Trace()
                .WriteTo.Console()
                .WriteTo.ApplicationInsightsEvents(new TelemetryClient(TelemetryConfiguration.Active), logEventToTelemetryConverter: EntryPoint.ConvertLogEventToTelementry);
                
            builder
                .RegisterInstance(logBuilder.CreateLogger())
                .AsImplementedInterfaces();                            

            // Event and Command dependencies

            builder
                .RegisterGeneric(typeof(ServiceBusQueueCommandPublisher<>))
                .As(typeof(ICommandPublisher<>))
                .SingleInstance();

            builder
                .RegisterGeneric(typeof(ServiceBusTopicEventPublisher<>))
                .As(typeof(IEventPublisher<>))
                .SingleInstance();            

            // Notifier dependencies                        

            builder
                .RegisterAssemblyTypes(orderSubmitterAssembly, coreAssembly)
                .AssignableTo<INotifier>()
                .AsImplementedInterfaces()
                .SingleInstance();

            // Web Job Function dependencies

            builder
                .RegisterType<CommandRetryThresholds>()
                .AsSelf()
                .OnActivating(args => args.ReplaceInstance(args.Context.Resolve<NotifierJobHostConfiguration>().CreateCommandRetryThresholdsFromConfiguration()));

            builder
                .RegisterAssemblyTypes(orderSubmitterAssembly, coreAssembly)
                .AssignableTo<WebJobFunctionBase>()
                .AsSelf()
                .WithParameter(new ResolvedParameter( (pi, ctx) => ((pi.ParameterType == typeof(IDisposable)) && (pi.Name == "lifetimeScope")), (pi, ctx) => ctx.Resolve<ILifetimeScope>()));

            // Create and return the container.

            return builder.Build();
        }

        /// <summary>
        ///   Configures the application to use Application Insights.
        /// </summary>
        /// 
        /// <param name="loggingConfiguration">The configuration containing logging-specific information.</param>
        /// <param name="telemetryConfiguration">The Application Insights active telemetry configuration.</param>
        /// 
        internal static void UseApplicationInsights(LoggingConfiguration   loggingConfiguration,
                                                    TelemetryConfiguration telemetryConfiguration)
        {
            // Configure the global Application Insights settings
            
            if (string.IsNullOrEmpty(loggingConfiguration?.ApplicationInsightsKey))
            {
                telemetryConfiguration.DisableTelemetry = true;
            }
            else
            {
                telemetryConfiguration.InstrumentationKey = loggingConfiguration.ApplicationInsightsKey;
                
                var builder = telemetryConfiguration.TelemetryProcessorChainBuilder;

                if (builder != null)
                {
                    builder.Use(next => new ApplicationInsightsDependencyTelemetryFilter(next, loggingConfiguration.DependencySlowResponseThresholdMilliseconds));
                    builder.Build();
                }
            }

            // Use or create a correlation identifier based on headers

            telemetryConfiguration.TelemetryInitializers.Add(new ApplicationInsightsWebJobTelemetryInitializer());
        }

        /// <summary>
        ///   Converts a Serilog log event to telementry for Application Insights.
        /// </summary>
        /// 
        /// <param name="logEvent">The log event to convert.</param>
        /// <param name="formatProvider">The provider to use for formatting the log entry.</param>
        /// 
        /// <returns>A telemetry instance for Application Insights to consume.</returns>
        /// 
        private static ITelemetry ConvertLogEventToTelementry(LogEvent        logEvent, 
                                                              IFormatProvider formatProvider)
        {
            ITelemetry telemetry;

            if (logEvent.Exception != null)
            {
                telemetry = logEvent.ToDefaultExceptionTelemetry(formatProvider);
            }
            else
            {
                telemetry = logEvent.ToDefaultTraceTelemetry(formatProvider);
            }

            // If a correlation identifier is associated with the log entry, use it for the operation identifier
            // in Application Insights. This allows telemetry for related operations to be more easily located in AI.

            if (logEvent.Properties.TryGetValue(LogPropertyNames.CorrelationId, out var correlationIdPropertyValue))
            {
                telemetry.Context.Operation.Id = correlationIdPropertyValue.ToString("l", null);
            }

            return telemetry;
        }
    }
}
