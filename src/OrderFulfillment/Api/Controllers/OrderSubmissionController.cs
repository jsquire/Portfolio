using System;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Http;
using OrderFulfillment.Api.Configuration;
using OrderFulfillment.Api.Extensions;
using OrderFulfillment.Api.Filters;
using OrderFulfillment.Api.Models.Requests;
using OrderFulfillment.Api.Models.Responses;
using OrderFulfillment.Api.Security;
using OrderFulfillment.Core.Commands;
using OrderFulfillment.Core.Events;
using OrderFulfillment.Core.Extensions;
using OrderFulfillment.Core.Models.Errors;
using NodaTime;
using Serilog;

namespace OrderFulfillment.Api.Controllers
{
    /// <summary>
    ///   Defines the endpoints intended to allow orders to be submitted for fulfillment.
    /// </summary>
    ///     
    public class OrderSubmissionController : ApiController
    {
        /// <summary>The clock to use for date/time operations.</summary>
        private readonly IClock clock;

        /// <summary>The publisher to use for sending of the <see cref="ProcessOrder" /> command</summary>
        private readonly ICommandPublisher<ProcessOrder> processOrderCommandPublisher;

        /// <summary>The publisher to use for the sending of events</summary>
        private readonly IEventPublisher<EventBase> eventPublisher;

        /// <summary>The configuration that influences the behavior of the controller</summary>
        private readonly OrderSubmissionControllerConfiguration config;

        /// <summary>The random number generator to use for jitter calculations on Retry-After headers.</summary>
        private readonly Random rng = new Random();

        /// <summary>
        ///   The logger to be used for emitting telemetry from the controller.
        /// </summary>
        /// 
        private ILogger Log { get; }

        /// <summary>
        ///   Initializes a new instance of the <see cref="OrderSubmissionController"/> class.
        /// </summary>
        /// 
        /// <param name="clock">The clock to use for time/date operations.</param>
        /// <param name="processOrderCommandPublisher">The publisher to use for the sending of the command to process an order.</param>
        /// <param name="eventPublisher">The publisher to use for the publishing of events.</param>
        /// <param name="logger">The logger to be used for emitting telemetry from the controller.</param>
        /// <param name="configuration">The configuration to use for influencing the behavior of the controller.</param>
        /// 
        public OrderSubmissionController(IClock                                 clock,
                                         ICommandPublisher<ProcessOrder>        processOrderCommandPublisher,
                                         IEventPublisher<EventBase>             eventPublisher,
                                         ILogger                                logger,
                                         OrderSubmissionControllerConfiguration configuration)
        {
            this.clock                        = clock                        ?? throw new ArgumentNullException(nameof(clock));
            this.processOrderCommandPublisher = processOrderCommandPublisher ?? throw new ArgumentNullException(nameof(processOrderCommandPublisher));
            this.eventPublisher               = eventPublisher               ?? throw new ArgumentNullException(nameof(eventPublisher));
            this.Log                          = logger                       ?? throw new ArgumentNullException(nameof(logger));
            this.config                       = configuration                ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        ///   Provides the web hook called for submission when an order is
        ///   ready for fulfillment.
        /// </summary>
        /// 
        /// <param name="partner">The partner associaed with this request.</param>
        /// <param name="order">Information about the order for which fulfillment is requested.</param>
        /// 
        /// <returns>The <see cref="System.Web.Http.IHttpActionResult"/> containing the response for the operation.  If successful, this will contain an acknowledgement of order receipt.</returns>
        ///
        [HttpPost]
        [ValidateMessage]
        [Route("partners/{partner:partnerIdentifier}/orders")]
        public async Task<IHttpActionResult> FulfillOrderWebHook(string                            partner,
                                                                [FromBody] OrderFulfillmentMessage order)
        {
            // Verify that the order was sent as the request body.
            
            if (order == null)
            {
                var errorSet = new ErrorSet(ErrorCode.ValueIsRequired, nameof(order), "The order body is required.");
                
                try
                {
                    var body = await this.Request.SafeReadContentAsStringAsync();

                    this.Log
                        .WithCorrelationId(this.Request.GetOrderFulfillmentCorrelationId())
                        .Information($"Response: {{Response}} { Environment.NewLine } Missing order detected for {{Controller}}::{{ActionMethod}}({{Partner}}) with Headers: [{{Headers}}] { Environment.NewLine } Body: {{RequestBody}} { Environment.NewLine } The following errors were observed: { Environment.NewLine }{{ErrorSet}}", 
                            HttpStatusCode.BadRequest,
                            nameof(OrderSubmissionController),
                            nameof(FulfillOrderWebHook),
                            partner,
                            this.Request.Headers,
                            body,
                            errorSet);
                }
                
                catch
                {
                    // Do nothing; logging is a non-critical operation that should not cause
                    // cascading failures.
                }   
                 
                return this.BadRequest(errorSet);
            }

            // If the emulation data was provided and the caller is not priviledged, then reject the request.

            var principal = this.User as ClaimsPrincipal;

            if ((order.Emulation != null) && 
               ((principal == null) || (!principal.HasClaim(claim => claim.Type == CustomClaimTypes.MayAccessPriviledgedOperations))))
            {
                try
                {
                    this.Log
                        .WithCorrelationId(this.Request.GetOrderFulfillmentCorrelationId())
                        .Warning($"Response: {{Response}} { Environment.NewLine } Unauthorized request detected for {{Controller}}::{{ActionMethod}}({{Partner}}) with Headers: [{{Headers}}] { Environment.NewLine }  The caller does not have permission to perform a priviledged operation.", 
                            HttpStatusCode.Forbidden,
                            nameof(OrderSubmissionController),
                            nameof(FulfillOrderWebHook),
                            partner,
                            this.Request.Headers);
                }
                
                catch
                {
                    // Do nothing; logging is a non-critical operation that should not cause
                    // cascading failures.
                }   

                return this.Content<object>(HttpStatusCode.Forbidden, null);
            }
            
            var assets = order.LineItems
                .SelectMany(item => item.Assets)
                .Where(asset => asset != null)
                .ToDictionary(asset => asset.Name, asset => this.ParseAssetLocationToUrl(asset.Location));

            // Create the command to trigger order processing and place it on the command queue
            
            var command = new ProcessOrder
            {
                OrderId         = order.OrderRequestHeader.OrderId,
                PartnerCode     = partner,
                Assets          = assets,
                Emulation       = order.Emulation,
                Id              = Guid.NewGuid(),
                CorrelationId   = this.Request.GetOrderFulfillmentCorrelationId(),
                OccurredTimeUtc = this.clock.GetCurrentInstant().ToDateTimeUtc(),
                CurrentUser     = principal?.Identity?.Name,
                Sequence        = 0
            };

            if (!(await this.processOrderCommandPublisher.TryPublishAsync(command)))
            {
                this.Log
                    .WithCorrelationId(this.Request.GetOrderFulfillmentCorrelationId())
                    .Error("Unable to publish the {CommandName} for {Partner}//{Order}", nameof(ProcessOrder), partner, command.OrderId);

                return this.ServiceUnavailable(this.CaclulateRetryAfter(this.config.ServiceUnavailableeRetryAfterInSeconds));
            }
            
            // This can't be fire-and-forget, as completing the handler with the outstanding request causes an exception.

            try
            {
                await this.eventPublisher.TryPublishAsync(command.CreateNewOrderEvent<OrderReceived>());
            }

            catch (Exception ex)
            {
                this.Log
                    .WithCorrelationId(this.Request.GetOrderFulfillmentCorrelationId())
                    .Error(ex, "The event {EventName} for {Partner}//{Order} could not be published.  This is non-critical for fulfillment processing.", nameof(OrderReceived), partner, command.OrderId);
            }

            return this.Accepted(new OrderFulfillmentAccepted(command.OrderId), this.CaclulateRetryAfter(this.config.OrderAcceptedRetryAfterInSeconds));            
        }

        /// <summary>
        ///   Performs the tasks needed to parse an asset location into a url.
        /// </summary>
        /// 
        /// <param name="location">The value to parse.</param>
        /// 
        /// <returns>The string-form of the url represented by the asset item location.</returns>
        /// 
        protected virtual string ParseAssetLocationToUrl(string location)
        {
            if (String.IsNullOrEmpty(location))
            {
                return null;
            }

            if (Uri.TryCreate(location, UriKind.Absolute, out var url))
            {
                // In order for production systems to be able to retrieve the intrinsic item,
                // the scheme must be HTTPS.  HTTP is not supported.
                
                var builder        = new UriBuilder(url);
                var hadDefaultPort = builder.Uri.IsDefaultPort;

                builder.Scheme = Uri.UriSchemeHttps;
                builder.Port   = hadDefaultPort ? -1 : builder.Port;

                return builder.ToString();
            }

            return location;
        }

        /// <summary>
        ///   Caclulates a value for the Retry-After header, adding some random jitter to avoid 
        ///   the thundering herd under load.
        /// </summary>
        /// 
        /// <param name="baseValueInSeconds">The base value desired for the retry period, in seconds.</param>
        /// 
        /// <returns>The time span to use for the retry-after period.</returns>
        /// 
        protected virtual TimeSpan CaclulateRetryAfter(int baseValueInSeconds)
        {
            return TimeSpan.FromSeconds(baseValueInSeconds + this.rng.Next(0, this.config.RetryAfterJitterUpperBoundInSeconds));
        }
    }
}