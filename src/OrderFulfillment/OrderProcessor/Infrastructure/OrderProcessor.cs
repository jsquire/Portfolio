using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OrderFulfillment.Core.Extensions;
using OrderFulfillment.Core.External;
using OrderFulfillment.Core.Models.External.Ecommerce;
using OrderFulfillment.Core.Models.External.OrderProduction;
using OrderFulfillment.Core.Models.Operations;
using OrderFulfillment.Core.Storage;
using OrderFulfillment.OrderProcessor.Configuration;
using OrderFulfillment.OrderProcessor.Models;
using Newtonsoft.Json;
using NodaTime;
using Polly;
using Serilog;

using OrderProduction = OrderFulfillment.Core.Models.External.OrderProduction;

namespace OrderFulfillment.OrderProcessor.Infrastructure
{
    /// <summary>
    ///   Performs thet acitons needed to process an order.
    /// </summary>
    /// 
    /// <seealso cref="IOrderProcessor" />
    /// 
    public class OrderProcessor : IOrderProcessor
    {
        /// <summary>The configuration to use for influencing order processing behavior.</summary>
        private readonly OrderProcessorConfiguration configuration;

        /// <summary>The generator to use for random numbers.</summary>
        private readonly Random rng;

        /// <summary>The clock instance to use for date/time operations.</summary>
        private readonly IClock clock;

        /// <summary>The client to use for interacting with the eCommerce service.</summary>
        private readonly IEcommerceClient ecommerceClient;

        /// <summary>The storage to use for orders.</summary>
        private readonly IOrderStorage orderStorage;

        /// <summary>The processor for the metadata associated with a SKU.</summary>
        private readonly ISkuMetadataProcessor skuMetadataProcessor;

        /// <summary>The settings to use for JSON serialization activities.</summary>
        private readonly JsonSerializerSettings jsonSerializerSettings;

        /// <summary>
        ///   The logger to be used for emitting telemetry from the processor.
        /// </summary>
        /// 
        private ILogger Log { get; }

        /// <summary>
        ///   Initializes a new instance of the <see cref="OrderProcessor"/> class.
        /// </summary>
        /// 
        /// <param name="configuration">The configuration to use for influencing order processing behavior.</param>
        /// <param name="ecommerceClient">The client to use for interacting with the eCommerce service.</param>
        /// <param name="orderStorage">The storage to use for orders.</param>
        /// <param name="skuMetadataProcessor">The processor for the metadata associated with a SKU.</param>
        /// <param name="logger">The logger to be used for emitting telemetry from the controller.</param>
        /// <param name="clock">The clock instance to use for date/time related operations.</param>
        /// <param name="jsonSerializerSettings">The settings to use for JSON serializerion.</param>
        /// 
        public OrderProcessor(OrderProcessorConfiguration configuration,
                              IEcommerceClient            ecommerceClient,
                              IOrderStorage               orderStorage, 
                              ISkuMetadataProcessor       skuMetadataProcessor, 
                              ILogger                     logger,
                              IClock                      clock, 
                              JsonSerializerSettings      jsonSerializerSettings)
        {
            this.configuration          = configuration          ?? throw new ArgumentNullException(nameof(configuration));
            this.ecommerceClient        = ecommerceClient        ?? throw new ArgumentNullException(nameof(ecommerceClient));
            this.orderStorage           = orderStorage           ?? throw new ArgumentNullException(nameof(orderStorage));
            this.skuMetadataProcessor   = skuMetadataProcessor   ?? throw new ArgumentNullException(nameof(skuMetadataProcessor));
            this.Log                    = logger                 ?? throw new ArgumentNullException(nameof(logger));
            this.clock                  = clock                  ?? throw new ArgumentNullException(nameof(clock));
            this.jsonSerializerSettings = jsonSerializerSettings ?? throw new ArgumentNullException(nameof(jsonSerializerSettings));

            this.rng = new Random();
        }

        /// <summary>
        ///   Performs the actions needed to process an order in preparation for submission.
        /// </summary>
        /// 
        /// <param name="partner">The partner associated with the order.</param>
        /// <param name="orderId">The unique identifier of the order.</param>
        /// <param name="orderAssets">The set of assets associated with the order.</param>
        /// <param name="emulation">The set of emulation requirements for processing; this will override the associated external communication an, instead, use the emulated result.</param>
        /// <param name="correlationId">An optional identifier used to correlate activities across the disparate parts of processing, including external interations.</param>
        /// 
        /// <returns>The result of the operation.</returns>
        /// 
        public async Task<OperationResult> ProcessOrderAsync(string                              partner, 
                                                             string                              orderId, 
                                                             IReadOnlyDictionary<string, string> orderAssets, 
                                                             DependencyEmulation                 emulation     = null,
                                                             string                              correlationId = null)
        {
            if (String.IsNullOrEmpty(partner))
            {
                throw new ArgumentException("The partner must be provided.", nameof(partner));
            }

            if (String.IsNullOrEmpty(orderId))
            {
                throw new ArgumentException("The order identifier must be provided.", nameof(orderId));
            }

            if ((orderAssets == null) || (orderAssets.Count < 1))
            {
                throw new ArgumentException("At least one asset is expected to be associated with the order.", nameof(orderAssets));
            }

            // Begin processing

            var log = this.Log.WithCorrelationId(correlationId);
            log.Information("Processing for {Partner}//{Order} has begun.", partner, orderId);

            try
            {
                var config      = this.configuration;
                var retryPolicy = this.CreateRetryPolicy(this.rng, config.OperationRetryMaxCount, config.OperationRetryExponentialSeconds, config.OperationRetryJitterSeconds);

                // Retrieve the details for the order.  If the result was not successful, return it as the result of processing.  This will allow
                // the caller to process the result and understand whether or not it should be retried.
                                
                var policyResult  = await retryPolicy.ExecuteAndCaptureAsync( () => this.RetrieveOrderDetailsAsync(log, this.ecommerceClient, partner, orderId, correlationId, emulation?.OrderDetails));
                var detailsResult = policyResult.Result ?? policyResult.FinalHandledResult ?? OperationResult.ExceptionResult;

                if (detailsResult.Outcome != Outcome.Success)
                {
                    return detailsResult;
                }

                // Translate the order details into a CreateOrderMessage.  This is the format that the order will need to be submitted in to be produced.

                var details                  = JsonConvert.DeserializeObject<OrderDetails>(detailsResult.Payload, this.jsonSerializerSettings);
                var firstAssetUrl            = orderAssets.First().Value;
                var lineItemAssets           = details.LineItems.ToDictionary(item => item.LineItemId, item => firstAssetUrl);
                var createOrderMessageResult = await this.BuildCreateOrderMessageFromDetailsAsync(config, log, this.skuMetadataProcessor, partner, orderId, lineItemAssets, details, this.jsonSerializerSettings, correlationId, emulation?.CreateOrderMessage);

                if (createOrderMessageResult.Outcome != Outcome.Success)
                {
                    return createOrderMessageResult;
                }

                // Store the CreateOrderMessage so that it can be retrieved for submission at a later point.

                policyResult = await retryPolicy.ExecuteAndCaptureAsync( () => this.StoreOrderForSubmissionAsync(log, this.orderStorage, partner, orderId, JsonConvert.DeserializeObject<CreateOrderMessage>(createOrderMessageResult.Payload), correlationId));
                
                var storageResult = policyResult.Result ?? policyResult.FinalHandledResult ?? OperationResult.ExceptionResult; 

                if (storageResult.Outcome != Outcome.Success)
                {
                    return storageResult;
                }

                log.Information("Processing for {Partner}//{Order} was successful.  The order has been staged for submission.", partner, orderId);

                return new OperationResult
                {
                    Outcome     = Outcome.Success,
                    Reason      = String.Empty,
                    Recoverable = Recoverability.Final,
                    Payload     = storageResult.Payload
                };
                
            }

            catch (Exception ex)
            {
                log.Error(ex, "An exception occurred at an indeterminite point of processing for {Partner}//{Order}", partner, orderId);
                return OperationResult.ExceptionResult;
            }
        }

        /// <summary>
        ///   Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// 
        public void Dispose()
        {
            this.ecommerceClient?.Dispose();
            this.skuMetadataProcessor?.Dispose();
        }

        /// <summary>
        ///   Retrieves the details of the requested order.
        /// </summary>
        /// 
        /// <param name="log">The logging instance to use for emitting information.</param>
        /// <param name="client">The client to use for interacting with the eCommerce service.</param>
        /// <param name="partner">The partner associated with the order.</param>
        /// <param name="orderId">The unique identifier of the order to retrieve the detials of.</param>
        /// <param name="correlationId">An optional identifier used to correlate activities across the disparate parts of processing, including external interations.</param>
        /// <param name="emulatedResult">An optional emulated result to use in place of querying the eCommerce service.</param>
        /// 
        /// <returns>The result of the operation.</returns>
        /// 
        /// <remarks>
        ///   This method owns responsibility for logging the results of the operation and how
        ///   it was produced.
        /// </remarks>
        /// 
        protected virtual async Task<OperationResult> RetrieveOrderDetailsAsync(ILogger          log, 
                                                                                IEcommerceClient client,
                                                                                string           partner,
                                                                                string           orderId,
                                                                                string           correlationId  = null,
                                                                                OperationResult  emulatedResult = null)
        {
            OperationResult result;

            try
            {
                result = emulatedResult ?? (await client.GetOrderDetailsAsync(orderId, correlationId));

                log.Information("Order details for {Partner}//{Order} have been retrieved.  Emulated: {Emulated}.  Result: {Result}", 
                    partner, 
                    orderId, 
                    (emulatedResult != null), 
                    result);
            }

            catch (Exception ex)
            {
                log.Error(ex, "An error occured while requesting the order details for {Partner}//{Order}", partner, orderId);                
                return OperationResult.ExceptionResult;
            }

            return result;
        }

        /// <summary>
        ///   Performs the actions needed build a <see cref="CreateOrderMessage" /> from the
        ///   <see cref="OrderDetails" /> of an order.
        /// </summary>
        /// 
        /// <param name="configuration">The configuration to use for bulding the message.</param>
        /// <param name="log">The logging instance to use for emitting information.</param>
        /// <param name="skuMetadataProcessor">The processor for SKU metadata.</param>
        /// <param name="partner">The partner associated with the order.</param>
        /// <param name="transactionId">The identifier to use for the transaction associated with this unique submission.</param>
        /// <param name="orderAssets">The set of assets associated with the order.</param>
        /// <param name="orderDetails">The details about the order.</param>
        /// <param name="serializerSettings">The settings to use for serialization operations.</param>
        /// <param name="correlationId">An optional identifier used to correlate activities across the disparate parts of processing, including external interations.</param>
        /// <param name="emulatedResult">An optional emulated result to use in place of building the message.</param>
        /// 
        /// <returns>The result of the operation.</returns>
        /// 
        protected virtual async Task<OperationResult> BuildCreateOrderMessageFromDetailsAsync(OrderProcessorConfiguration         configuration,
                                                                                              ILogger                             log, 
                                                                                              ISkuMetadataProcessor               skuMetadataProcessor,
                                                                                              string                              partner,
                                                                                              string                              transactionId,
                                                                                              IReadOnlyDictionary<string, string> orderAssets, 
                                                                                              OrderDetails                        orderDetails,
                                                                                              JsonSerializerSettings              serializerSettings,
                                                                                              string                              correlationId        = null,
                                                                                              OperationResult                     emulatedResult       = null)
        {
            OperationResult result;

            try
            {
                if (emulatedResult != null)
                {
                    result = emulatedResult;
                }
                else
                {
                    // Base the message on the order details that were received.

                    var message = orderDetails.ToCreateOrderMessage();

                    // Populate the static values
                                
                    message.TransactionId                = transactionId;
                    message.Identity.PartnerCode         = partner;
                    message.Identity.PartnerSubCode      = configuration.PartnerSubCode;
                    message.Identity.PartnerRegionCode   = configuration.PartnerSubCode;
                    message.Shipping.ShippingInstruction = ShipWhen.ShipAsItemsAreAvailable;
                    message.Instructions.Priority        = OrderPriority.Normal;
                    message.PartnerMetadata              = new PartnerOrderMetadata { OrderDateUtc = this.clock.GetCurrentInstant().ToDateTimeUtc() };

                    // Process the line items against the SKU metadata to generate the necessary line item detail.

                    var itemDetail = new Dictionary<string, string>();
                
                    foreach (var item in orderDetails.LineItems)
                    {
                        var itemTemplate = await skuMetadataProcessor.RenderOrderTemplateAsync(new OrderTemplateMetadata 
                        { 
                            Sku                = item.ProductCode,
                            TotalSheets        = item.TotalSheetCount,
                            AdditionalSheets   = item.AdditionalSheetCount,
                            AssetUrl           = ((orderAssets.TryGetValue(item.LineItemId, out var value)) ? value : null),
                            LineItemCount      = orderDetails.Recipients.SelectMany(recipient => recipient.OrderedItems).Where(orderedItem => orderedItem.LineItemId == item.LineItemId).Sum(orderedItem => (int)orderedItem.Quantity),
                            NumberOfRecipients = orderDetails.Recipients.Count(recipient => recipient.OrderedItems.Any(orderedItem => orderedItem.LineItemId == item.LineItemId))
                        });       
                    
                        itemDetail.Add(item.LineItemId, itemTemplate);
                    };

                    message.LineItems = message.LineItems.Select(item => 
                    {
                        item.Item                  = itemDetail[item.LineItemId].Trim();
                        item.ServiceLevelAgreement = configuration.ServiceLevelAgreementCode;

                        return item;

                    }).ToList();

                    // The message is complete.  Create the result to return.

                    result = new OperationResult
                    {
                        Outcome     = Outcome.Success,
                        Reason      = String.Empty,
                        Recoverable = Recoverability.Final,
                        Payload     = JsonConvert.SerializeObject(message, serializerSettings)
                    };
                }

                log.Information("The CreateOrderMessage for {Partner}//{Order} has been built.  Emulated: {Emulated}.  Result: {Result}", 
                    partner, 
                    orderDetails.OrderId, 
                    (emulatedResult != null), 
                    result);
            }

            catch (Exception ex)
            {
                log.Error(ex, "An error occured while building the CreateOrderMessage for {Partner}//{Order}", partner, orderDetails.OrderId);                
                return OperationResult.ExceptionResult;
            }

            return result;            
        }

        /// <summary>
        ///   Stores an order for use in submission at a later point.
        /// </summary>
        /// 
        /// <param name="log">The logging instance to use for emitting information.</param>
        /// <param name="storage">The storage to use for the order.</param>
        /// <param name="createOrderMessage">The CreateOrderMessage being saved to storage.</param>
        /// <param name="correlationId">An optional identifier used to correlate activities across the disparate parts of processing, including external interations.</param>
        /// <param name="emulatedResult">An optional emulated result to use in place of interacting with storage.</param>
        /// 
        /// <returns>The result of the operation.</returns>
        /// 
        protected virtual async Task<OperationResult> StoreOrderForSubmissionAsync(ILogger            log, 
                                                                                   IOrderStorage      storage,
                                                                                   string             partner,
                                                                                   string             orderId,
                                                                                   CreateOrderMessage order,
                                                                                   string             correlationId  = null,
                                                                                   OperationResult    emulatedResult = null)
        {
            OperationResult result;

            try
            {
                string key;
                if (emulatedResult != null)
                {
                    result = emulatedResult;
                    key = string.Empty;
                }
                else
                {
                    key = await storage.SavePendingOrderAsync(partner, orderId, order);
                    
                    result = new OperationResult
                    {
                        Outcome     = Outcome.Success,
                        Reason      = String.Empty,
                        Recoverable = Recoverability.Final,
                        Payload     = key
                    };
                }

                log.Information("Order details for {Partner}//{Order} have been saved as a pending submission to {BlobKey}.  Emulated: {Emulated}.  Result: {Result}", 
                    order?.Identity?.PartnerCode,
                    order?.Identity?.PartnerOrderId, 
                    key,
                    (emulatedResult != null), 
                    result);
            }

            catch (Exception ex)
            {
                log.Error(ex, "An error occured while saving {Partner}//{Order} as pending submission.", order.Identity.PartnerCode, order.Identity.PartnerOrderId);                
                return OperationResult.ExceptionResult;
            }

            return result;
        }

        /// <summary>
        ///   Creates a short-term retry policy for use with external operations.
        /// </summary>
        /// 
        /// <param name="rng">The </param>
        /// <param name="maxRetryAttempts">The maximum number of retry attempts before giving up.</param>
        /// <param name="exponentialBackoffSeconds">The number of seconds on which to base the exponential backoff.</param>
        /// <param name="baseJitterSeconds">The base number of seconds to use when including random jitter.</param>
        /// 
        /// <returns>The retry policy.</returns>
        /// 
        protected virtual IAsyncPolicy<OperationResult> CreateRetryPolicy(Random rng,
                                                                          int    maxRetryAttempts,
                                                                          double exponentialBackoffSeconds,
                                                                          double baseJitterSeconds)
        {
            return Policy
                .HandleResult<OperationResult>(result => ((result.Outcome != Outcome.Success) && (result.Recoverable == Recoverability.Retriable)))
                .Or<TimeoutException>()
                .WaitAndRetryAsync(maxRetryAttempts, attempt => TimeSpan.FromSeconds((Math.Pow(2, attempt) * exponentialBackoffSeconds) + (rng.NextDouble() * baseJitterSeconds)));
        }
    }
}
