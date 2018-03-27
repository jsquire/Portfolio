using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Routing;
using FluentAssertions;
using OrderFulfillment.Api.Configuration;
using OrderFulfillment.Api.Controllers;
using OrderFulfillment.Api.Models.Requests;
using OrderFulfillment.Api.Models.Responses;
using OrderFulfillment.Api.Security;
using OrderFulfillment.Core.Commands;
using OrderFulfillment.Core.Events;
using OrderFulfillment.Core.Models.Errors;
using OrderFulfillment.Core.Models.Operations;
using Moq;
using NodaTime;
using Serilog;
using Xunit;

namespace OrderFulfillment.Api.Tests.Controllers
{
    /// <summary>
    ///   The suite of tests for the <see cref="OrderSubmissionController" /> class.
    /// </summary>
    /// 
    public class OrderSubmissionControllerTests
    {
        /// <summary>
        ///   Validates the behavior of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesTheLogger()
        {
            Action actionUnderTest;

            actionUnderTest = () => new OrderSubmissionController(null, Mock.Of<ICommandPublisher<ProcessOrder>>(), Mock.Of<IEventPublisher<EventBase>>(), Mock.Of<ILogger>(), new OrderSubmissionControllerConfiguration());
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the clock was missing");
        }

        /// <summary>
        ///   Validates the behavior of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesTheCommandPublisher()
        {
            Action actionUnderTest;

            actionUnderTest = () => new OrderSubmissionController(Mock.Of<IClock>(), null, Mock.Of<IEventPublisher<EventBase>>(), Mock.Of<ILogger>(), new OrderSubmissionControllerConfiguration());
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the clock was missing");
        }

        /// <summary>
        ///   Validates the behavior of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesTheEventPublisher()
        {
            Action actionUnderTest;

            actionUnderTest = () => new OrderSubmissionController(Mock.Of<IClock>(), Mock.Of<ICommandPublisher<ProcessOrder>>(), null, Mock.Of<ILogger>(), new OrderSubmissionControllerConfiguration());
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the clock was missing");
        }

        /// <summary>
        ///   Validates the behavior of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesTheClock()
        {
            Action actionUnderTest;

            actionUnderTest = () => new OrderSubmissionController(Mock.Of<IClock>(), Mock.Of<ICommandPublisher<ProcessOrder>>(), Mock.Of<IEventPublisher<EventBase>>(), null, new OrderSubmissionControllerConfiguration());
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the logger was missing");
        }

        /// <summary>
        ///   Validates the behavior of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesItsConfiguration()
        {
            Action actionUnderTest;

            actionUnderTest = () => new OrderSubmissionController(Mock.Of<IClock>(), Mock.Of<ICommandPublisher<ProcessOrder>>(), Mock.Of<IEventPublisher<EventBase>>(), Mock.Of<ILogger>(), null);
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the configuration was missing");
        }

        /// <summary>
        ///  Verifies the behavior of the <see cref="OrderSubmissionController.FulfillOrderWebHook "/>
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task FulfillOrderWebHookWithNullOrderIsRejected()
        {
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = nameof(OrderSubmissionController) };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);

            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");
            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);

            var controller = new OrderSubmissionController(Mock.Of<IClock>(), Mock.Of<ICommandPublisher<ProcessOrder>>(), Mock.Of<IEventPublisher<EventBase>>(), Mock.Of<ILogger>(), new OrderSubmissionControllerConfiguration())
            {
                ControllerContext = controllerContext
            };

            var actionResult = await controller.FulfillOrderWebHook("PLACE-ORDER", null);
            var result       = await actionResult.ExecuteAsync(CancellationToken.None);

            result.Should().NotBeNull("because a result should have been returned");
            result.StatusCode.Should().Be(HttpStatusCode.BadRequest, "because the request should have been rejected");
            
            var errorSet = await result.Content.ReadAsAsync<ErrorSet>();

            errorSet.Should().NotBeNull("because a set of errors should have been returned");
            errorSet.Errors.Should().ContainSingle(error => error.MemberPath == "order", "because the order was invalid");
        }

        /// <summary>
        ///  Verifies the behavior of the <see cref="OrderSubmissionController.FulfillOrderWebHook "/>
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task FulfillOrderWebHookWithNullOrderLogs()
        {
            var mockLogger           = new Mock<ILogger>();
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = nameof(OrderSubmissionController) };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);

            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");
            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);

            mockLogger.Setup(log => log.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                      .Returns(mockLogger.Object);
                                    
            var controller = new OrderSubmissionController(Mock.Of<IClock>(), Mock.Of<ICommandPublisher<ProcessOrder>>(), Mock.Of<IEventPublisher<EventBase>>(), mockLogger.Object, new OrderSubmissionControllerConfiguration())
            {
                ControllerContext = controllerContext
            };
            
            var partner      = "SQUIRE";
            var actionResult = await controller.FulfillOrderWebHook(partner, null);

            mockLogger.Verify(logger => logger.Information(It.IsAny<string>(), 
                                                           It.Is<HttpStatusCode>(code => code == HttpStatusCode.BadRequest), 
                                                           It.Is<string>(name => name == nameof(OrderSubmissionController)),
                                                           It.Is<string>(method => method == nameof(OrderSubmissionController.FulfillOrderWebHook)),
                                                           It.Is<string>(partnerName => partnerName == partner),
                                                           It.IsAny<HttpRequestHeaders>(),
                                                           It.IsAny<string>(),
                                                           It.IsAny<ErrorSet>()), 
                 Times.Once, "The failure should be logged");
        }

        /// <summary>
        ///  Verifies the behavior of the <see cref="OrderSubmissionController.FulfillOrderWebHook "/>
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task FulfillOrderWebHookWithNonPriviledgedUserIsRejectedWhenSupplyingTestData()
        {               
            var mockActionDescriptor   = new Mock<HttpActionDescriptor>();
            var httpConfiguration      = new HttpConfiguration();
            var routeData              = new HttpRouteData(new HttpRoute());
            var request                = new HttpRequestMessage();
            var controllerDescriptor   = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = nameof(OrderSubmissionController) };
            var controllerContext      = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext          = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var unpriviledgedIdentity  = new ClaimsIdentity(new Claim[] { new Claim(CustomClaimTypes.IdentityType, "UnitTest") });

            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");
            actionContext.RequestContext.Principal = new ClaimsPrincipal(unpriviledgedIdentity);
            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
                        
            var controller = new OrderSubmissionController(Mock.Of<IClock>(), Mock.Of<ICommandPublisher<ProcessOrder>>(), Mock.Of<IEventPublisher<EventBase>>(), Mock.Of<ILogger>(), new OrderSubmissionControllerConfiguration())
            {
                ControllerContext = controllerContext
            };            

            var order = new OrderFulfillmentMessage
            {
               OrderRequestHeader = new OrderHeader { OrderId = "ABC123", OrderDate = new DateTime(2017, 12, 09, 09, 00, 00, DateTimeKind.Utc) },
               LineItems            = new List<LineItem>(),
               Emulation          = new DependencyEmulation()
            };

            var actionResult = await controller.FulfillOrderWebHook("SQUIRE", order);
            var result       = await actionResult.ExecuteAsync(CancellationToken.None);

            result.Should().NotBeNull("because a result should have been returned");
            result.StatusCode.Should().Be(HttpStatusCode.Forbidden, "because the request should have been rejected");
            
            var errorSet = await result.Content.ReadAsAsync<ErrorSet>();
            errorSet.Should().BeNull("because no errors should be returned when the caller is unpriviledged");
        }

        /// <summary>
        ///  Verifies the behavior of the <see cref="OrderSubmissionController.FulfillOrderWebHook "/>
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task FulfillOrderWebHookWithNonPriviledgedUserLogsWhenSupplyingTestData()
        {      
            var mockLogger             = new Mock<ILogger>();
            var mockActionDescriptor   = new Mock<HttpActionDescriptor>();
            var httpConfiguration      = new HttpConfiguration();
            var routeData              = new HttpRouteData(new HttpRoute());
            var request                = new HttpRequestMessage();
            var controllerDescriptor   = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = nameof(OrderSubmissionController) };
            var controllerContext      = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext          = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var unpriviledgedIdentity  = new ClaimsIdentity(new Claim[] { new Claim(CustomClaimTypes.IdentityType, "UnitTest") });

            mockLogger.Setup(log => log.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                      .Returns(mockLogger.Object);

            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");
            actionContext.RequestContext.Principal = new ClaimsPrincipal(unpriviledgedIdentity);
            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
                        
            var controller = new OrderSubmissionController(Mock.Of<IClock>(), Mock.Of<ICommandPublisher<ProcessOrder>>(), Mock.Of<IEventPublisher<EventBase>>(), mockLogger.Object, new OrderSubmissionControllerConfiguration())
            {
                ControllerContext = controllerContext
            };            

            var order = new OrderFulfillmentMessage
            {
               OrderRequestHeader = new OrderHeader { OrderId = "ABC123", OrderDate = new DateTime(2017, 12, 09, 09, 00, 00, DateTimeKind.Utc) },
               LineItems            = new List<LineItem>(),
               Emulation          = new DependencyEmulation()
            };

            var partner      = "SQUIRE";
            var actionResult = await controller.FulfillOrderWebHook(partner, order);
            var result       = await actionResult.ExecuteAsync(CancellationToken.None);

            mockLogger.Verify(logger => logger.Warning(It.IsAny<string>(), 
                                                       It.Is<HttpStatusCode>(code => code == HttpStatusCode.Forbidden), 
                                                       It.Is<string>(name => name == nameof(OrderSubmissionController)),
                                                       It.Is<string>(method => method == nameof(OrderSubmissionController.FulfillOrderWebHook)),
                                                       It.Is<string>(partnerName => partnerName == partner),
                                                       It.IsAny<HttpRequestHeaders>()), 
                 Times.Once, "The failure should be logged");
        }

        /// <summary>
        ///  Verifies the behavior of the <see cref="OrderSubmissionController.FulfillOrderWebHook "/>
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task FulfillOrderWebHookWithFailedCommandFails()
        {      
            var mockLogger             = new Mock<ILogger>();
            var mockCommandPublisher   = new Mock<ICommandPublisher<ProcessOrder>>();
            var mockActionDescriptor   = new Mock<HttpActionDescriptor>();
            var httpConfiguration      = new HttpConfiguration();
            var routeData              = new HttpRouteData(new HttpRoute());
            var request                = new HttpRequestMessage();
            var controllerDescriptor   = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = nameof(OrderSubmissionController) };
            var controllerContext      = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext          = new HttpActionContext(controllerContext, mockActionDescriptor.Object);

            mockLogger.Setup(log => log.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                      .Returns(mockLogger.Object);

            mockCommandPublisher.Setup(publisher => publisher.TryPublishAsync(It.IsAny<ProcessOrder>(), It.IsAny<Instant?>()))
                                .ReturnsAsync(false);

            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");
            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
                        
            var controller = new OrderSubmissionController(Mock.Of<IClock>(), mockCommandPublisher.Object, Mock.Of<IEventPublisher<EventBase>>(), mockLogger.Object, new OrderSubmissionControllerConfiguration { ServiceUnavailableeRetryAfterInSeconds = 5 })
            {
                ControllerContext = controllerContext
            };            

            var order = new OrderFulfillmentMessage
            {
               OrderRequestHeader = new OrderHeader { OrderId = "ABC123", OrderDate = new DateTime(2017, 12, 09, 09, 00, 00, DateTimeKind.Utc) },
               LineItems            = new List<LineItem>()
            };

            var actionResult = await controller.FulfillOrderWebHook("SQUIRE", order);
            var result       = await actionResult.ExecuteAsync(CancellationToken.None);

            result.Should().NotBeNull("because a result should have been returned");
            result.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable, "because the request should have been rejected");
            result.Headers.RetryAfter.Should().NotBeNull("because a RETRY-AFTER header should be specified for an Service Unavailable result");
        }

        /// <summary>
        ///  Verifies the behavior of the <see cref="OrderSubmissionController.FulfillOrderWebHook "/>
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task FulfillOrderWebHookWithAPriviledgedUserAcceptsTestData()
        {   
            var mockCommandPublisher = new Mock<ICommandPublisher<ProcessOrder>>();
            var mockActionDescriptor = new Mock<HttpActionDescriptor>();
            var httpConfiguration    = new HttpConfiguration();
            var routeData            = new HttpRouteData(new HttpRoute());
            var request              = new HttpRequestMessage();
            var controllerDescriptor = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = nameof(OrderSubmissionController) };
            var controllerContext    = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext        = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var priviledgedIdentity  = new ClaimsIdentity(new Claim[] { new Claim(CustomClaimTypes.MayAccessPriviledgedOperations, "true") });

            mockCommandPublisher.Setup(publisher => publisher.TryPublishAsync(It.IsAny<ProcessOrder>(), It.IsAny<Instant?>()))
                                .ReturnsAsync(true);

            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");
            actionContext.RequestContext.Principal = new ClaimsPrincipal(priviledgedIdentity);
            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
                        
            var controller = new OrderSubmissionController(Mock.Of<IClock>(), mockCommandPublisher.Object, Mock.Of<IEventPublisher<EventBase>>(), Mock.Of<ILogger>(), new OrderSubmissionControllerConfiguration())
            {
                ControllerContext = controllerContext
            };            

            var order = new OrderFulfillmentMessage
            {
               OrderRequestHeader = new OrderHeader { OrderId = "ABC123", OrderDate = new DateTime(2017, 12, 09, 09, 00, 00, DateTimeKind.Utc) },
               LineItems            = new List<LineItem>(),
               Emulation          = new DependencyEmulation()
            };

            var actionResult = await controller.FulfillOrderWebHook("SQUIRE", order);
            var result       = await actionResult.ExecuteAsync(CancellationToken.None);

            result.Should().NotBeNull("because a result should have been returned");
            result.StatusCode.Should().Be(HttpStatusCode.Accepted, "because the request was valid"); 
        }

        /// <summary>
        ///  Verifies the behavior of the <see cref="OrderSubmissionController.FulfillOrderWebHook "/>
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task FulfillOrderWebHookAcceptsAnOrder()
        {           
            var mockCommandPublisher   = new Mock<ICommandPublisher<ProcessOrder>>();
            var mockActionDescriptor   = new Mock<HttpActionDescriptor>();
            var httpConfiguration      = new HttpConfiguration();
            var routeData              = new HttpRouteData(new HttpRoute());
            var request                = new HttpRequestMessage();
            var controllerDescriptor   = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = nameof(OrderSubmissionController) };
            var controllerContext      = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext          = new HttpActionContext(controllerContext, mockActionDescriptor.Object);

            mockCommandPublisher.Setup(publisher => publisher.TryPublishAsync(It.IsAny<ProcessOrder>(), It.IsAny<Instant?>()))
                                .ReturnsAsync(true);

            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");
            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
                        
            var controller = new OrderSubmissionController(Mock.Of<IClock>(), mockCommandPublisher.Object, Mock.Of<IEventPublisher<EventBase>>(), Mock.Of<ILogger>(), new OrderSubmissionControllerConfiguration { OrderAcceptedRetryAfterInSeconds = 5 })
            {
                ControllerContext = controllerContext
            };            

            var order = new OrderFulfillmentMessage
            {
               OrderRequestHeader = new OrderHeader { OrderId = "ABC123", OrderDate = new DateTime(2017, 12, 09, 09, 00, 00, DateTimeKind.Utc) },
               LineItems            = new List<LineItem>()
            };

            var actionResult = await controller.FulfillOrderWebHook("SQUIRE", order);
            var result       = await actionResult.ExecuteAsync(CancellationToken.None);

            result.Should().NotBeNull("because a result should have been returned");
            result.StatusCode.Should().Be(HttpStatusCode.Accepted, "because the request was valid");            
            result.Headers.RetryAfter.Should().NotBeNull("because a RETRY-AFTER header should be specified for an Accepted result");
                        
            result.TryGetContentValue<OrderFulfillmentAccepted>(out var response).Should().BeTrue("because the response should have been set");
            response.FulfillerData.OrderId.Should().Be(order.OrderRequestHeader.OrderId, "because the correct order should have been acknoledged");
        }

        /// <summary>
        ///  Verifies the behavior of the <see cref="OrderSubmissionController.FulfillOrderWebHook "/>
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task FulfillOrderWebHookPublishesTheCommandToTiriggerProcessingForAnAcceptedOrder()
        {   
            var mockLogger             = new Mock<ILogger>();
            var mockCommandPublisher   = new Mock<ICommandPublisher<ProcessOrder>>();
            var mockActionDescriptor   = new Mock<HttpActionDescriptor>();
            var httpConfiguration      = new HttpConfiguration();
            var routeData              = new HttpRouteData(new HttpRoute());
            var request                = new HttpRequestMessage();
            var controllerDescriptor   = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = nameof(OrderSubmissionController) };
            var controllerContext      = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext          = new HttpActionContext(controllerContext, mockActionDescriptor.Object);

            mockLogger.Setup(log => log.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                      .Returns(mockLogger.Object);

            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");
            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
                        
            var controller = new OrderSubmissionController(Mock.Of<IClock>(), mockCommandPublisher.Object, Mock.Of<IEventPublisher<EventBase>>(), mockLogger.Object, new OrderSubmissionControllerConfiguration { OrderAcceptedRetryAfterInSeconds = 5 })
            {
                ControllerContext = controllerContext
            };            

            var order = new OrderFulfillmentMessage
            {
               OrderRequestHeader = new OrderHeader { OrderId = "ABC123", OrderDate = new DateTime(2017, 12, 09, 09, 00, 00, DateTimeKind.Utc) },
               LineItems            = new List<LineItem>()
            };

            var actionResult = await controller.FulfillOrderWebHook("SQUIRE", order);
            var result       = await actionResult.ExecuteAsync(CancellationToken.None);

            result.Should().NotBeNull("because a result should have been returned");

            mockCommandPublisher.Verify(publisher => publisher.TryPublishAsync(It.Is<ProcessOrder>(command => command.OrderId == order.OrderRequestHeader.OrderId), It.Is<Instant?>(time => time == null)), 
                Times.Once, 
                "An accepted order should emit an Order Received event");
        }

        /// <summary>
        ///  Verifies the behavior of the <see cref="OrderSubmissionController.FulfillOrderWebHook "/>
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task FulfillOrderWebHookForcesAssetUrlsToHttpsForAnAcceptedOrder()
        {   
            var mockLogger             = new Mock<ILogger>();
            var mockCommandPublisher   = new Mock<ICommandPublisher<ProcessOrder>>();
            var mockActionDescriptor   = new Mock<HttpActionDescriptor>();
            var httpConfiguration      = new HttpConfiguration();
            var routeData              = new HttpRouteData(new HttpRoute());
            var request                = new HttpRequestMessage();
            var controllerDescriptor   = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = nameof(OrderSubmissionController) };
            var controllerContext      = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext          = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var publishedCommand       = default(ProcessOrder);

            mockLogger.Setup(log => log.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                      .Returns(mockLogger.Object);

            mockCommandPublisher
                .Setup(publisher => publisher.TryPublishAsync(It.IsAny<ProcessOrder>(), It.IsAny<Instant?>()))
                .ReturnsAsync(true)
                .Callback<ProcessOrder, Instant?>( (command, publishTime) => publishedCommand = command);

            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");
            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
                        
            var controller = new OrderSubmissionController(Mock.Of<IClock>(), mockCommandPublisher.Object, Mock.Of<IEventPublisher<EventBase>>(), mockLogger.Object, new OrderSubmissionControllerConfiguration { OrderAcceptedRetryAfterInSeconds = 5 })
            {
                ControllerContext = controllerContext
            };            

            var order = new OrderFulfillmentMessage
            {
               OrderRequestHeader = new OrderHeader { OrderId = "ABC123", OrderDate = new DateTime(2017, 12, 09, 09, 00, 00, DateTimeKind.Utc) },
               LineItems            = new List<LineItem>
               {
                   { new LineItem { Assets = new List<ItemAsset> { { new ItemAsset { Name = "one", Location = "https://www.bob.com/path" } } } } },
                   { new LineItem { Assets = new List<ItemAsset> { { new ItemAsset { Name = "two", Location = "https://www.john.com:443/path/?name=john" } } } } },
                   { new LineItem { Assets = new List<ItemAsset> { { new ItemAsset { Name = "three", Location = "gopher://www.frank.com:8086/path" } } } } }
               }
            };

            var actionResult = await controller.FulfillOrderWebHook("SQUIRE", order);
            var result       = await actionResult.ExecuteAsync(CancellationToken.None);

            result.Should().NotBeNull("because a result should have been returned");
            publishedCommand.Should().NotBeNull("because the command should have been published");

            foreach (var item in order.LineItems)
            {
                var asset        = item.Assets.Single();
                var commandAsset = publishedCommand.Assets[asset.Name];
                
                asset.Should().NotBeNull("because an asset for asset {0} should be present", asset.Name);
                
                var assetUri        = new Uri(asset.Location);
                var commandAssetUri = new Uri(commandAsset);

                commandAssetUri.Scheme.Should().Be(Uri.UriSchemeHttps, "because the uri scheme should have been forced to HTTPS for asset {0}", asset.Name);
                commandAssetUri.Host.Should().Be(assetUri.Host, "because the authorities should match for asset {0}", asset.Name);
                commandAssetUri.PathAndQuery.Should().Be(assetUri.PathAndQuery, "because the path and querystring should match for asset {0}", asset.Name);

                if (assetUri.IsDefaultPort)
                {
                    commandAssetUri.Port.Should().Be(443, "because the default port was used for asset {0}", asset.Name);
                }
                else
                {
                    commandAssetUri.Port.Should().Be(assetUri.Port, "because a non-default port was used for asset {0}", asset.Name);
                }
            }
        }

        /// <summary>
        ///  Verifies the behavior of the <see cref="OrderSubmissionController.FulfillOrderWebHook "/>
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task FulfillOrderWebHookAllowsNonUrlAssetsForAnAcceptedOrder()
        {   
            var mockLogger             = new Mock<ILogger>();
            var mockCommandPublisher   = new Mock<ICommandPublisher<ProcessOrder>>();
            var mockActionDescriptor   = new Mock<HttpActionDescriptor>();
            var httpConfiguration      = new HttpConfiguration();
            var routeData              = new HttpRouteData(new HttpRoute());
            var request                = new HttpRequestMessage();
            var controllerDescriptor   = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = nameof(OrderSubmissionController) };
            var controllerContext      = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext          = new HttpActionContext(controllerContext, mockActionDescriptor.Object);
            var publishedCommand       = default(ProcessOrder);

            mockLogger.Setup(log => log.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                      .Returns(mockLogger.Object);

            mockCommandPublisher
                .Setup(publisher => publisher.TryPublishAsync(It.IsAny<ProcessOrder>(), It.IsAny<Instant?>()))
                .ReturnsAsync(true)
                .Callback<ProcessOrder, Instant?>( (command, publishTime) => publishedCommand = command);

            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");
            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
                        
            var controller = new OrderSubmissionController(Mock.Of<IClock>(), mockCommandPublisher.Object, Mock.Of<IEventPublisher<EventBase>>(), mockLogger.Object, new OrderSubmissionControllerConfiguration { OrderAcceptedRetryAfterInSeconds = 5 })
            {
                ControllerContext = controllerContext
            };            

            var order = new OrderFulfillmentMessage
            {
               OrderRequestHeader = new OrderHeader { OrderId = "ABC123", OrderDate = new DateTime(2017, 12, 09, 09, 00, 00, DateTimeKind.Utc) },
               LineItems            = new List<LineItem>
               {
                   { new LineItem { Assets = new List<ItemAsset> { { new ItemAsset { Name = "one", Location = "Not a url" } } } } }
               }
            };

            var actionResult = await controller.FulfillOrderWebHook("SQUIRE", order);
            var result       = await actionResult.ExecuteAsync(CancellationToken.None);

            result.Should().NotBeNull("because a result should have been returned");
            publishedCommand.Should().NotBeNull("because the command should have been published");

            foreach (var item in order.LineItems)
            {
                var asset        = item.Assets.Single();
                var commandAsset = publishedCommand.Assets[asset.Name];
                
                commandAsset.Should().NotBeNull("because an asset for asset {0} should be present", asset.Name);
                commandAsset.Should().Be(asset.Location, "because the asset value should be unchanged for asset {0}", asset.Name);
            }
        }

        /// <summary>
        ///  Verifies the behavior of the <see cref="OrderSubmissionController.FulfillOrderWebHook "/>
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task FulfillOrderWebHookPublishesTheEventForAnAcceptedOrder()
        {   
            var mockCommandPublisher   = new Mock<ICommandPublisher<ProcessOrder>>();
            var mockEventPublisher     = new Mock<IEventPublisher<EventBase>>();
            var mockActionDescriptor   = new Mock<HttpActionDescriptor>();
            var httpConfiguration      = new HttpConfiguration();
            var routeData              = new HttpRouteData(new HttpRoute());
            var request                = new HttpRequestMessage();
            var controllerDescriptor   = new HttpControllerDescriptor { Configuration = httpConfiguration, ControllerName = nameof(OrderSubmissionController) };
            var controllerContext      = new HttpControllerContext(httpConfiguration, routeData, request) { ControllerDescriptor = controllerDescriptor };
            var actionContext          = new HttpActionContext(controllerContext, mockActionDescriptor.Object);

            mockCommandPublisher.Setup(publisher => publisher.TryPublishAsync(It.IsAny<ProcessOrder>(), It.IsAny<Instant?>()))
                                .ReturnsAsync(true);

            mockActionDescriptor.SetupGet(descriptor => descriptor.ActionName).Returns("someAction");
            request.SetConfiguration(httpConfiguration);
            request.SetRouteData(routeData);
                        
            var controller = new OrderSubmissionController(Mock.Of<IClock>(), mockCommandPublisher.Object, mockEventPublisher.Object, Mock.Of<ILogger>(), new OrderSubmissionControllerConfiguration { OrderAcceptedRetryAfterInSeconds = 5 })
            {
                ControllerContext = controllerContext
            };            

            var order = new OrderFulfillmentMessage
            {
               OrderRequestHeader = new OrderHeader { OrderId = "ABC123", OrderDate = new DateTime(2017, 12, 09, 09, 00, 00, DateTimeKind.Utc) },
               LineItems            = new List<LineItem>()
            };

            var actionResult = await controller.FulfillOrderWebHook("SQUIRE", order);
            var result       = await actionResult.ExecuteAsync(CancellationToken.None);

            result.Should().NotBeNull("because a result should have been returned");

            mockEventPublisher.Verify(publisher => publisher.TryPublishAsync(It.Is<OrderReceived>(evt => evt.OrderId == order.OrderRequestHeader.OrderId)), Times.Once, "An accepted order should emit an Order Received event");
        }
    }


}
