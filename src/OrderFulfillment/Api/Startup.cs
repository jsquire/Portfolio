using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Routing;
using ApplicationInsights.OwinExtensions;
using Autofac;
using Autofac.Integration.WebApi;
using FluentValidation.Validators;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Owin;
using OrderFulfillment.Api.Filters;
using OrderFulfillment.Api.Infrastructure;
using OrderFulfillment.Api.RouteConstraints;
using OrderFulfillment.Api.Security;
using OrderFulfillment.Core.Commands;
using OrderFulfillment.Core.Configuration;
using OrderFulfillment.Core.Events;
using OrderFulfillment.Core.Infrastructure;
using OrderFulfillment.Core.Logging;
using OrderFulfillment.Core.Validators;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using NodaTime;
using Owin;
using Serilog;
using Serilog.Events;
using Serilog.ExtensionMethods;

[assembly: OwinStartup(typeof(OrderFulfillment.Api.Startup), nameof(OrderFulfillment.Api.Startup.ConfigureApplication))]

namespace OrderFulfillment.Api
{
    /// <summary>
    ///   Serves at the entry point for bootstrapping the API host and performing 
    ///   any needed global configuration and registration.
    /// </summary>
    /// 
    public class Startup
    {
        /// <summary>
        ///   Configures the application.
        /// </summary>
        /// 
        /// <param name="appBuilder">The builder for use in boostrapping and configuring the application.</param>
        ///
        public void ConfigureApplication(IAppBuilder appBuilder)
        {        
            var httpConfiguration = new HttpConfiguration();
            
            Startup.ConfigureDependencyResolver(httpConfiguration);            
            Startup.UseApplicationInsights(appBuilder, (LoggingConfiguration)httpConfiguration.DependencyResolver.GetService(typeof(LoggingConfiguration)), TelemetryConfiguration.Active); 

            Startup.ConfigureWebApi(httpConfiguration);
            Startup.RegisterGlobalFilters(httpConfiguration);
            
            httpConfiguration.EnsureInitialized();            
            appBuilder.UseWebApi(httpConfiguration);
            
            // Ensure that the correlation identifier of the request is reflected in the response.

            appBuilder.Use((IOwinContext owinContext, Func<Task> next) =>
            {               
                if ((owinContext.Request != null) && (owinContext.Request.Headers.ContainsKey(HttpHeaders.CorrelationId)))
                {
                    owinContext.Response.OnSendingHeaders(Startup.AppendCorrelationIdHeader, new AppendCorrelationIdHeaderState
                    {
                        CorrelationId =  owinContext.Request.Headers[HttpHeaders.CorrelationId],
                        OwinContext = owinContext
                    });
                }

                return next();
            });
        }

        /// <summary>
        ///   Performs the tasks needed to configure Web API for use with the Order Fulfillment
        ///   API offerings.
        /// </summary>
        /// 
        /// <param name="config">The HTTP configuration to be used with Web API.</param>
        /// 
        internal static void ConfigureWebApi(HttpConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            // Register any route constraints that have been discovered.

            var constraintLocator = new RouteConstraintLocator();
            var constraintResolver = new DefaultInlineConstraintResolver();

            foreach (var constraintPair in constraintLocator.DiscoveredConstraints)
            {
                constraintResolver.ConstraintMap.Add(constraintPair.Key, constraintPair.Value);
            }

            // Map the API routes that were defined using attributes, using the discovered constraints.

            config.MapHttpAttributeRoutes(constraintResolver);

            // Configure the JSON serializer to use enumeration names (instead of index) and adhere to 
            // camel casing for serialized members.

            var jsonFormatter = config.Formatters.JsonFormatter;

            jsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            jsonFormatter.SerializerSettings.Converters.Add(new StringEnumConverter());

            // Clear all of the other formatters, supporting only JSON.

            config.Formatters.Clear();
            config.Formatters.Add(jsonFormatter);
        }

        /// <summary>
        ///   Performs the tasks needed to configure dependency injection for use with Web API
        ///   and other consumers of the HTTP configuration.
        /// </summary>
        /// 
        /// <param name="config">The HTTP configuration to be used for DI configuration.</param>
        /// 
        internal static void ConfigureDependencyResolver(HttpConfiguration           config,
                                                         Func<IConfigurationFactory> createConfigurationFactoryDelegate = null)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var apiAssembly  = typeof(Startup).Assembly;
            var coreAssembly = typeof(IConfigurationFactory).Assembly;
            var builder      = new ContainerBuilder();

            // Infrastructure dependencies

            builder.Register<IClock>(context => SystemClock.Instance);

            // Configuration dependencies

            builder
                .RegisterInstance(createConfigurationFactoryDelegate?.Invoke() ?? new ApplicationSettingsConfigurationFactory())                
                .AsImplementedInterfaces()
                .SingleInstance();
                            
            builder
                .RegisterAssemblyTypes(apiAssembly, coreAssembly)
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
                .WriteTo.ApplicationInsightsEvents(new TelemetryClient(TelemetryConfiguration.Active), logEventToTelemetryConverter: Startup.ConvertLogEventToTelementry);
                
            builder
                .RegisterInstance(logBuilder.CreateLogger())
                .AsImplementedInterfaces();

            // Validation dependencies

            builder
                .RegisterAssemblyTypes(apiAssembly, coreAssembly)
                .AssignableTo<IPropertyValidator>()
                .AsSelf()
                .SingleInstance();

            builder
                .RegisterAssemblyTypes(apiAssembly, coreAssembly)
                .AsClosedTypesOf(typeof(IMessageValidator<>))
                .SingleInstance();

            // Security dependencies

            builder
                .RegisterAssemblyTypes(apiAssembly, coreAssembly)
                .AssignableTo<IAuthenticationHandler>()
                .As<IAuthenticationHandler>()
                .PreserveExistingDefaults();

            builder
                .RegisterAssemblyTypes(apiAssembly, coreAssembly)
                .AssignableTo<IAuthorizationPolicy>()
                .As<IAuthorizationPolicy>()
                .PreserveExistingDefaults();

            // Event and Command dependencies

            builder
                .RegisterGeneric(typeof(ServiceBusQueueCommandPublisher<>))
                .As(typeof(ICommandPublisher<>))
                .SingleInstance();

            builder
                .RegisterGeneric(typeof(ServiceBusTopicEventPublisher<>))
                .As(typeof(IEventPublisher<>))
                .SingleInstance();
                
            // API dependencies

            builder.RegisterType<HttpHeaderParser>()
                .AsImplementedInterfaces()
                .SingleInstance();
              
            builder.RegisterType<GlobalExceptionFilter>();
            builder.RegisterApiControllers(apiAssembly);
            builder.RegisterWebApiFilterProvider(config);

             // Finalize the resolver, using the configured dependencies.

            config.DependencyResolver = new AutofacWebApiDependencyResolver(builder.Build());
        }

        /// <summary>
        ///   Configures the application to use Application Insights.
        /// </summary>
        /// 
        /// <param name="appBuilder">The application builder instance.</param>
        /// <param name="loggingConfiguration">The configuration containing logging-specific information.</param>        
        /// <param name="telemetryConfiguration">The Application Insights active telemetry configuration.</param>
        /// 
        internal static void UseApplicationInsights(IAppBuilder            appBuilder,
                                                    LoggingConfiguration   loggingConfiguration,
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

            telemetryConfiguration.TelemetryInitializers.Add(new OperationIdTelemetryInitializer());

            appBuilder.UseApplicationInsights(new RequestTrackingConfiguration 
            { 
                GetAdditionalContextProperties = context => 
                {
                    IEnumerable<KeyValuePair<string, string>> result;

                    context.Request.Headers.TryGetValue(HttpHeaders.CorrelationId, out var values);
                    
                    var id = values?.FirstOrDefault();                    
                    
                    if (id != null)
                    {
                        result = new[] { new KeyValuePair<string, string>(HttpHeaders.CorrelationId, id) };
                    }
                    else
                    {
                        result = Enumerable.Empty<KeyValuePair<string, string>>();
                    }

                    return Task.FromResult(result);                
                }
            });

            appBuilder.Use<OperationIdContextMiddleware>(new OperationIdContextMiddlewareConfiguration 
            {
                ShouldTryGetIdFromHeader = true,
                OperationIdHeaderName    = HttpHeaders.CorrelationId
            });
            
            appBuilder.Use((IOwinContext owinContext, Func<Task> next) =>
            {
                var operationId = OperationIdContext.Get();

                // If there was not a correlation identifier issued as part of the request, this will ensure that one will be available
                // later in the pipeline by using the one generated by Application Insights.
                
                if ((owinContext.Request != null) && (!owinContext.Request.Headers.ContainsKey(HttpHeaders.CorrelationId)))
                {
                    owinContext.Request.Headers[HttpHeaders.CorrelationId] = operationId;
                }

                return next();
            });
        }

        /// <summary>
        ///   Performs the tasks needed to register the filters that should be globally applied to
        ///   all endpoints.
        /// </summary>
        /// 
        /// <param name="config">The HTTP configuration to be used for DI configuration.</param>
        /// 
        internal static void RegisterGlobalFilters(HttpConfiguration config)
        {
            config.Filters.Add((GlobalExceptionFilter)config.DependencyResolver.GetService(typeof(GlobalExceptionFilter)));
            config.Filters.Add(new OrderFulfillmentAuthenticateAttributeAttribute());

            // When registering the authorization attribute, instruct it only to enforce the default policies globally.  This allows
            // individual endpoints to add additional authorization attributes with more specific policies for specific needs.

            var defaultAuthorizationPolicies = typeof(AuthorizationPolicy)
                                                  .GetFields()
                                                  .Where(field => field.GetCustomAttributes(typeof(DefaultPolicyAttribute), true).Any())
                                                  .Select(field => (AuthorizationPolicy)field.GetValue(null))
                                                  .ToArray();

            config.Filters.Add(new OrderFulfillmentAuthorizeAttribute(defaultAuthorizationPolicies));
        }

        /// <summary>
        ///   Appends a correlation identifier to the response for the current call context.
        /// </summary>
        /// 
        /// <param name="owinState">The state passed via Owin containing correlation information</param>
        /// 
        private static void AppendCorrelationIdHeader(object owinState)
        {
            var state           = owinState as AppendCorrelationIdHeaderState;
            var responseHeaders = state?.OwinContext?.Response?.Headers;

            
            if ((state != null) && (responseHeaders != null) && (!responseHeaders.ContainsKey(HttpHeaders.CorrelationId)))
            {
                responseHeaders.Set(HttpHeaders.CorrelationId, state.CorrelationId);
            }
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

        #region Nested Class Definitions

            /// <summary>
            ///   Represents the state to use when appending a correlation identifier to
            ///   responses.
            /// </summary>
            /// 
            private class AppendCorrelationIdHeaderState
            {
                /// <summary>The active context of the request.</summary>
                public IOwinContext OwinContext;

                /// <summary>The correlation identifier to include in the response</summary>
                public string CorrelationId;
            }

        #endregion
    }
}