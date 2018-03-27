using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Dependencies;
using FluentAssertions;
using OrderFulfillment.Api.Configuration;
using OrderFulfillment.Api.Filters;
using OrderFulfillment.Api.Security;
using OrderFulfillment.Core.Commands;
using OrderFulfillment.Core.Configuration;
using OrderFulfillment.Core.Events;
using Moq;
using Serilog;
using Xunit;

namespace OrderFulfillment.Api.Tests
{
    /// <summary>
    ///   The suite of tests for the <see cref="Startup" />
    ///   class.
    /// </summary>
    /// 
    public class StartupTests
    {
        /// <summary>
        ///   Verifies behavior of the ConfigureDependencyResolver method.
        ///   this is mucking with static members. It is not parallel safe
        /// </summary>
        /// 
        [Fact]
        public async Task ConfigureDeppendencyResolverDiscoversAuthenticationHandlers()
        {
            var mockConfigurationFactory = new Mock<IConfigurationFactory>();
            var httpConfig               = new HttpConfiguration();
            var sharedSecretConfig       = new SharedSecretAuthenticationConfiguration { Enabled = true, PrimarySecret = "test", PrimaryKey = "key!" };
            var clientCertificateConfig  = new ClientCertificateAuthenticationConfiguration { Enabled = true, EnforceLocalCertificateValidation = false, SerializedCertificateClaimsMapping = "{\";4497ebb9f0f694d219fe8652a8d4922fead6a5d9\";:{\";urn:ordering:security:privilege:sudo\";:\";true\";}}" };

            try
            {
                mockConfigurationFactory
                    .Setup(factory => factory.Create(It.IsAny<Type>()))
                    .Returns<Type>(type =>
                    {
                        if (type == typeof(SharedSecretAuthenticationConfiguration))
                        {
                            return sharedSecretConfig;
                        }
                        else if (type == typeof(ClientCertificateAuthenticationConfiguration))
                        {
                            return clientCertificateConfig;
                        }

                        return Activator.CreateInstance(type);
                    });

                Startup.ConfigureDependencyResolver(httpConfig, () => mockConfigurationFactory.Object);

                var locator = httpConfig.DependencyResolver;
                locator.Should().NotBeNull("because the dependency resolver should have been set");

                var authenticationHandlers = locator.GetServices(typeof(IAuthenticationHandler));
                authenticationHandlers.Should().NotBeNullOrEmpty("because the authentication handlers should have been found");

                var expected = typeof(IAuthenticationHandler).Assembly.GetTypes().Where(type => type.GetInterface(nameof(IAuthenticationHandler)) != null);
                authenticationHandlers.Select(handler => handler.GetType()).Should().BeEquivalentTo(expected, "because the locator should discover handlers defined along side the interface");

                await Task.Delay(1000);
            }
            finally
            {
                httpConfig?.DependencyResolver?.Dispose();
            }
        }

        /// <summary>
        ///   Verifies behavior of the ConfigureDependencyResolver method.
        ///   this is mucking with static members. It is not parallel safe
        /// </summary>
        /// 
        [Fact]
        public void ConfigureDeppendencyResolverDiscoversAuthorizationPolicies()
        {
            var mockConfigurationFactory = new Mock<IConfigurationFactory>();
            var httpConfig               = new HttpConfiguration();     
            
            try
            {                
                mockConfigurationFactory
                    .Setup(factory => factory.Create(It.IsAny<Type>()))
                    .Returns<Type>(type => 
                    {
                        if (type == typeof(AuthenticatedPrincipalAuthorizationPolicyConfiguration))
                        {
                             return new AuthenticatedPrincipalAuthorizationPolicyConfiguration { Enabled = true };
                        }
                        else if (type == typeof(RequireSslAuthorizationPolicyConfiguration))
                        {
                             return new RequireSslAuthorizationPolicyConfiguration{ Enabled = true };
                        }
                        else if (type == typeof(PartnerAuthorizationPolicyConfiguration))
                        {
                             return new PartnerAuthorizationPolicyConfiguration{ Enabled = true };
                        }
                        else if (type == typeof(PriviledgedOperationAuthorizationPolicyConfiguration))
                        {
                             return new PriviledgedOperationAuthorizationPolicyConfiguration{ Enabled = true };
                        }

                        return Activator.CreateInstance(type);
                    });                

                Startup.ConfigureDependencyResolver(httpConfig, () => mockConfigurationFactory.Object);

                var locator = httpConfig.DependencyResolver;
                locator.Should().NotBeNull("because the dependency resolver should have been set");

                var authenticationHandlers = locator.GetServices(typeof(IAuthorizationPolicy));
                authenticationHandlers.Should().NotBeNullOrEmpty("because the authentication handlers should have been found");

                var expected = typeof(IAuthorizationPolicy).Assembly.GetTypes().Where(type => type.GetInterface(nameof(IAuthorizationPolicy)) != null);
                authenticationHandlers.Select(handler => handler.GetType()).Should().BeEquivalentTo(expected, "because the locator should discover policies defined along side the interface");
            }
            finally
            {
                httpConfig?.DependencyResolver?.Dispose();
            }
        }

        /// <summary>
        ///   Verifies behavior of the ConfigureDependencyResolver method.
        ///   this is mucking with static members. It is not parallel safe
        /// </summary>
        /// 
        [Fact]
        public void ConfigureDependencyResolverDiscoversCommandPublishers()
        {
            var mockConfigurationFactory = new Mock<IConfigurationFactory>();
            var httpConfig               = new HttpConfiguration();
            var processOrderConfig       = new ProcessOrderServiceBusQueueCommandPublisherConfiguration { QueueName = "One" };
            var submitOrderConfig        = new SubmitOrderForProductionServiceBusQueueCommandPublisherConfiguration { QueueName = "Two" };

            try
            {
                mockConfigurationFactory
                    .Setup(factory => factory.Create(It.IsAny<Type>()))
                    .Returns<Type>(type =>
                    {
                        if (type == typeof(ProcessOrderServiceBusQueueCommandPublisherConfiguration))
                        {
                            return processOrderConfig;
                        }
                        else if (type == typeof(SubmitOrderForProductionServiceBusQueueCommandPublisherConfiguration))
                        {
                            return submitOrderConfig;
                        }

                        return Activator.CreateInstance(type);
                    });

                Startup.ConfigureDependencyResolver(httpConfig, () => mockConfigurationFactory.Object);

                var locator = httpConfig.DependencyResolver;
                locator.Should().NotBeNull("because the dependency resolver should have been set");

                var processOrderPublisher = locator.GetService(typeof(ICommandPublisher<ProcessOrder>));
                processOrderPublisher.Should().NotBeNull("because the ProcessOrder publisher should be resolvable");

                var submitOrderPublisher = locator.GetService(typeof(ICommandPublisher<SubmitOrderForProduction>));
                submitOrderPublisher.Should().NotBeNull("because the SubmitOrderForProduction publisher should be resolvable");
            }
            finally
            {
                httpConfig?.DependencyResolver?.Dispose();
            }
        }

        /// <summary>
        ///   Verifies behavior of the ConfigureDependencyResolver method.
        ///   this is mucking with static members. It is not parallel safe
        /// </summary>
        /// 
        [Fact]
        public void ConfigureDependencyResolverDiscoversEventPublishers()
        {
            var mockConfigurationFactory = new Mock<IConfigurationFactory>();
            var httpConfig               = new HttpConfiguration();
            var eventBaseConfig          = new EventBaseServiceBusTopicEventPublisherConfiguration { TopicName = "One" };

            try
            {
                mockConfigurationFactory
                    .Setup(factory => factory.Create(It.IsAny<Type>()))
                    .Returns<Type>(type =>
                    {
                        if (type == typeof(ServiceBusTopicEventPublisherConfiguration<EventBase>))
                        {
                            return eventBaseConfig;
                        }

                        return Activator.CreateInstance(type);
                    });

                Startup.ConfigureDependencyResolver(httpConfig, () => mockConfigurationFactory.Object);

                var locator = httpConfig.DependencyResolver;
                locator.Should().NotBeNull("because the dependency resolver should have been set");

                var eventBasePublisher = locator.GetService(typeof(IEventPublisher<EventBase>));
                eventBasePublisher.Should().NotBeNull("because the EventBase publisher should be resolvable");
            }
            finally
            {
                httpConfig?.DependencyResolver?.Dispose();
            }
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="Startup.RegisterGlobalFilters" />  method.
        /// </summary>
        /// 
        [Fact]
        public void RegisterGlobalFiltersRegistersGlobalExceptionFilter()
        {
            var mockDependencyResolver = new Mock<IDependencyResolver>();
            var httpConfiguration      = new HttpConfiguration();

            httpConfiguration.DependencyResolver = mockDependencyResolver.Object;

            mockDependencyResolver.Setup(scope => scope.GetService(It.Is<Type>(param => param == typeof(GlobalExceptionFilter))))
                                  .Returns(new GlobalExceptionFilter(new ErrorHandlingConfiguration(), Mock.Of<ILogger>()));

            Startup.RegisterGlobalFilters(httpConfiguration);

            httpConfiguration.Filters.Count(filter => filter.Instance.GetType() == typeof(GlobalExceptionFilter)).Should().Be(1, "because there should be a single instance of the exception filter registered");
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="Startup.RegisterGlobalFilters" />  method.
        /// </summary>
        /// 
        [Fact]
        public void RegisterGlobalFiltersRegistersAuthenticationFilter()
        {
            var mockDependencyResolver = new Mock<IDependencyResolver>();
            var httpConfiguration      = new HttpConfiguration();

            httpConfiguration.DependencyResolver = mockDependencyResolver.Object;

            mockDependencyResolver.Setup(scope => scope.GetService(It.Is<Type>(param => param == typeof(GlobalExceptionFilter))))
                                  .Returns(new GlobalExceptionFilter(new ErrorHandlingConfiguration(), Mock.Of<ILogger>()));

            Startup.RegisterGlobalFilters(httpConfiguration);

            httpConfiguration.Filters.Count(filter => filter.Instance.GetType() == typeof(OrderFulfillmentAuthenticateAttributeAttribute)).Should().Be(1, "because there should be a single instance of the authentication filter registered");
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="Startup.RegisterGlobalFilters" />  method.
        /// </summary>
        /// 
        [Fact]
        public void RegisterGlobalFiltersRegistersAuthorizationFilter()
        {
            var mockDependencyResolver = new Mock<IDependencyResolver>();
            var httpConfiguration      = new HttpConfiguration();

            httpConfiguration.DependencyResolver = mockDependencyResolver.Object;

            mockDependencyResolver.Setup(scope => scope.GetService(It.Is<Type>(param => param == typeof(GlobalExceptionFilter))))
                                  .Returns(new GlobalExceptionFilter(new ErrorHandlingConfiguration(), Mock.Of<ILogger>()));

            Startup.RegisterGlobalFilters(httpConfiguration);

            httpConfiguration.Filters.Count(filter => filter.Instance.GetType() == typeof(OrderFulfillmentAuthorizeAttribute)).Should().Be(1, "because there should be a single instance of the authorization filter registered");
        }

        /// <summary>
        ///   Verifies behavior of the <see cref="Startup.RegisterGlobalFilters" />  method.
        /// </summary>
        /// 
        [Fact]
        public void RegisterGlobalFiltersSetsDefaultAuthorizationPolicies()
        {
            var mockDependencyResolver = new Mock<IDependencyResolver>();
            var httpConfiguration      = new HttpConfiguration();

            httpConfiguration.DependencyResolver = mockDependencyResolver.Object;

            mockDependencyResolver.Setup(scope => scope.GetService(It.Is<Type>(param => param == typeof(GlobalExceptionFilter))))
                                  .Returns(new GlobalExceptionFilter(new ErrorHandlingConfiguration(), Mock.Of<ILogger>()));

            Startup.RegisterGlobalFilters(httpConfiguration);

            var authFilter = httpConfiguration.Filters.FirstOrDefault(filter => filter.Instance.GetType() == typeof(OrderFulfillmentAuthorizeAttribute));
            authFilter.Should().NotBeNull("because the authorization filter should have been regisered");

            var authPolicies = typeof(OrderFulfillmentAuthorizeAttribute).GetField("activePolicies", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(authFilter.Instance) as HashSet<AuthorizationPolicy>;
            authPolicies.Should().NotBeNull("because there should have been a set of active authorization policies");
            
            authPolicies.Any(policy => typeof(AuthorizationPolicy).GetField(policy.ToString()).GetCustomAttribute(typeof(DefaultPolicyAttribute), true) == null)
                .Should().BeFalse("because all policies in the set should have been marked as default");

            typeof(AuthorizationPolicy)
                .GetFields()
                .Where(field => field.GetCustomAttributes(typeof(DefaultPolicyAttribute), true).Any())
                .Select(field => (AuthorizationPolicy)field.GetValue(null))
                .Any(field => !authPolicies.Contains(field))
                .Should().BeFalse("beause each policy marked as default should be in the default policy set");
        }
    }
}
