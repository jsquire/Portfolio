using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OrderFulfillment.Core.Extensions;
using OrderFulfillment.Core.External;
using OrderFulfillment.Core.Infrastructure;
using OrderFulfillment.Core.Models.External.OrderProduction;
using OrderFulfillment.Core.Models.Operations;
using OrderFulfillment.Core.Storage;
using OrderFulfillment.OrderProcessor.Configuration;
using Polly;
using Serilog;

namespace OrderFulfillment.OrderSubmitter.Infrastructure
{
    /// <summary>
    ///   Performs thet acitons needed to submit an order for production.
    /// </summary>
    /// 
    /// <seealso cref="IOrderSubmitter" />
    /// 
    public class OrderSubmitter : IOrderSubmitter
    {
        /// <summary>The configuration to use for influencing order submission behavior.</summary>
        private readonly OrderSubmitterConfiguration configuration;

        // <summary>The generator to use for random numbers.</summary>
        private readonly Random rng;

        /// <summary>The client to use for interacting with the order production service.</summary>
        private readonly IOrdeProductionClient orderProductionClient;

        /// <summary>The storage to use for orders.</summary>
        private readonly IOrderStorage orderStorage;

        /// <summary>The settings to use for JSON serialization activities.</summary>
        private readonly JsonSerializerSettings jsonSerializerSettings;

        /// <summary>
        ///   The logger to be used for emitting telemetry from the processor.
        /// </summary>
        /// 
        private ILogger Log { get; }

        /// <summary>
        ///   Initializes a new instance of the <see cref="OrderSubmitter"/> class.
        /// </summary>
        /// 
        /// <param name="configuration">The configuration to use for influencing order submission behavior.</param>
        /// <param name="orderProductionClient">The client to use for interacting with the order production service.</param>
        /// <param name="orderStorage">The storage to use for orders.</param>
        /// <param name="logger">The logger to be used for emitting telemetry from the controller.</param>
        /// <param name="jsonSerializerSettings">The settings to use for JSON serializerion.</param>
        /// 
        public OrderSubmitter(OrderSubmitterConfiguration configuration,
                              IOrdeProductionClient       orderProductionClient,
                              IOrderStorage               orderStorage, 
                              ILogger                     logger,
                              JsonSerializerSettings      jsonSerializerSettings)
        {
            this.configuration          = configuration          ?? throw new ArgumentNullException(nameof(configuration));
            this.orderProductionClient  = orderProductionClient  ?? throw new ArgumentNullException(nameof(orderProductionClient));
            this.orderStorage           = orderStorage           ?? throw new ArgumentNullException(nameof(orderStorage));
            this.Log                    = logger                 ?? throw new ArgumentNullException(nameof(logger));
            this.jsonSerializerSettings = jsonSerializerSettings ?? throw new ArgumentNullException(nameof(jsonSerializerSettings));

            this.rng = new Random();
        }

        /// <summary>
        ///   Performs the actions needed to submit an order for production.
        /// </summary>
        /// 
        /// <param name="partner">The partner associated with the order.</param>
        /// <param name="orderId">The unique identifier of the order.</param>
        /// <param name="emulation">The set of emulation requirements for processing; this will override the associated external communication an, instead, use the emulated result.</param>
        /// <param name="correlationId">An optional identifier used to correlate activities across the disparate parts of processing, including external interations.</param>
        /// 
        /// <returns>The result of the operation.</returns>
        ///
        public async Task<OperationResult> SubmitOrderForProductionAsync(string              partner, 
                                                                         string              orderId, 
                                                                         DependencyEmulation emulation     = null, 
                                                                         string              correlationId = null) 
        {
            if (String.IsNullOrEmpty(partner))
            {
                throw new ArgumentException("The partner must be provided.", nameof(partner));
            }

            if (String.IsNullOrEmpty(orderId))
            {
                throw new ArgumentException("The order identifier must be provided.", nameof(orderId));
            }

            // Begin processing
            
            var log = this.Log.WithCorrelationId(correlationId);
            
            log.Information("Submission for {Partner}//{Order} has begun.", partner, orderId);
            
            try
            {
                var config      = this.configuration;
                var retryPolicy = this.CreateRetryPolicy<OperationResult, string>(this.rng, config.OperationRetryMaxCount, config.OperationRetryExponentialSeconds, config.OperationRetryJitterSeconds);

                // Retrieve the details for the order.  If the result was not successful, return it as the result of processing.  This will allow
                // the caller to process the result and understand whether or not it should be retried.

                var orderRetryPolicy   = this.CreateRetryPolicy<OperationResult<CreateOrderMessage>, CreateOrderMessage>(this.rng, config.OperationRetryMaxCount, config.OperationRetryExponentialSeconds, config.OperationRetryJitterSeconds);
                var orderPolicyResult  = await orderRetryPolicy.ExecuteAndCaptureAsync( () => this.RetrievePendingOrderAsync(log, this.orderStorage, this.jsonSerializerSettings, partner, orderId, correlationId, emulation?.CreateOrderMessage));
                var orderResult        = orderPolicyResult.Result ?? orderPolicyResult.FinalHandledResult ?? OperationResult<CreateOrderMessage>.ExceptionResult; 
                
                if (orderResult.Outcome != Outcome.Success)
                {
                    return new OperationResult
                    {
                        Outcome     = orderResult.Outcome,
                        Reason      = orderResult.Reason,
                        Recoverable = orderResult.Recoverable,
                        Payload     = String.Empty
                    };
                }
                
                // Submit the order for production.
                                
                var policyResult     = await retryPolicy.ExecuteAndCaptureAsync( () => this.SendOrderToProductionAsync(log, this.orderProductionClient, orderResult.Payload, correlationId, emulation?.OrderSubmission));
                var submissionResult = policyResult.Result ?? policyResult.FinalHandledResult ?? OperationResult.ExceptionResult;
                
                if (submissionResult.Outcome != Outcome.Success)
                {
                    return submissionResult;
                }
                
                // Store the CreateOrderMessage as complated, so that it can be used for any troubleshooting in the future.
                
                policyResult = await retryPolicy.ExecuteAndCaptureAsync( () => this.StoreOrderAsCompletedAsync(log, this.orderStorage, orderResult.Payload, correlationId, null));

                var completedStorageResult = policyResult.Result ?? policyResult.FinalHandledResult ?? OperationResult.ExceptionResult;
                                
                if (completedStorageResult.Outcome != Outcome.Success)
                {
                    return completedStorageResult;
                }

                // Remove the pending order from storage, as it is now complete.  If there was an issue deleting, log a warning and continue;  this is
                // a non-critical operation.

                OperationResult deletePendingResult;
                
                try
                {
                    policyResult        = await retryPolicy.ExecuteAndCaptureAsync( () => this.DeletePendingOrderAsync(log, this.orderStorage, partner, orderId, correlationId, null));
                    deletePendingResult = policyResult.Result ?? policyResult.FinalHandledResult ?? OperationResult.ExceptionResult;

                }

                catch
                {
                    deletePendingResult = OperationResult.ExceptionResult;
                }
                
                if (deletePendingResult.Outcome != Outcome.Success)
                {
                    log.Warning("Could not remove {Partner}//{Order} from the pending order storage.  This should be cleaned up.", partner, orderId);
                }

                // Submission is complete.
                
                log.Information("Submission for {Partner}//{Order} was successful.", partner, orderId);
                
                return new OperationResult
                {
                    Outcome     = Outcome.Success,
                    Reason      = String.Empty,
                    Recoverable = Recoverability.Final,
                    Payload     = String.Empty
                };

            }
            catch (Exception ex)
            {
                log.Error(ex, "An exception occurred at an indeterminite point of submission for {Partner}//{Order}", partner, orderId);                
                return OperationResult.ExceptionResult;
            }
        }

        /// <summary>
        ///   Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// 
        public void Dispose()
        {
            this.orderProductionClient?.Dispose();
        }

        /// <summary>
        ///   Retrieves an order from pending storage.
        /// </summary>
        /// 
        /// <param name="log">The logging instance to use for emitting information.</param>
        /// <param name="storage">The storage to use for the order.</param>
        /// <param name="serializerSettings">The settings to use for JSON serialization operations.</param>
        /// <param name="partner">The partner associated with the order.</param>
        /// <param name="orderId">The unique identifier of the order to retrieve the detials of.</param>
        /// <param name="correlationId">An optional identifier used to correlate activities across the disparate parts of processing, including external interations.</param>
        /// <param name="emulatedResult">An optional emulated result to use in place of interacting with storage.</param>
        /// 
        /// <returns>The result of the operation.</returns>
        /// 
        protected virtual async Task<OperationResult<CreateOrderMessage>> RetrievePendingOrderAsync(ILogger                log, 
                                                                                                    IOrderStorage          storage,
                                                                                                    JsonSerializerSettings serializerSettings,
                                                                                                    string                 partner,
                                                                                                    string                 orderId,
                                                                                                    string                 correlationId  = null,
                                                                                                    OperationResult        emulatedResult = null)
        {
            OperationResult<CreateOrderMessage> result;

            try
            {
                if (emulatedResult != null)
                {
                    result = new OperationResult<CreateOrderMessage>
                    {
                        Outcome     = emulatedResult.Outcome,
                        Reason      = emulatedResult.Reason,
                        Recoverable = emulatedResult.Recoverable,
                        Payload     = (String.IsNullOrEmpty(emulatedResult.Payload)) ? null : JsonConvert.DeserializeObject<CreateOrderMessage>(emulatedResult.Payload, serializerSettings)
                    };
                }
                else
                {
                    var storageResult = await storage.TryRetrievePendingOrderAsync(partner, orderId);

                    if (!storageResult.Found)
                    {
                        log.Error("Order details for {Partner}//{Order} were not found in the pending order storage. Submission cannot continue. Emulated: {Emulated}.", 
                           partner,
                           orderId,
                           (emulatedResult != null));

                        return new OperationResult<CreateOrderMessage>
                        {
                           Outcome     = Outcome.Failure,
                           Reason      = FailureReason.OrderNotFoundInPendingStorage,
                           Recoverable = Recoverability.Final,
                        };
                    }
                    
                    result = new OperationResult<CreateOrderMessage>
                    {
                        Outcome     = Outcome.Success,
                        Reason      = String.Empty,
                        Recoverable = Recoverability.Final,
                        Payload     = storageResult.Order                      
                    };
                }                    

                log.Information("The order for {Partner}//{Order} was retrieved from pending storage.  Emulated: {Emulated}.  Result: {Result}", 
                    partner, 
                    orderId,
                    (emulatedResult != null), 
                    result);
            }

            catch (Exception ex)
            {
                log.Error(ex, "An error occured while retrieving {Partner}//{Order} as pending submission.", partner, orderId);                
                return OperationResult<CreateOrderMessage>.ExceptionResult;
            }

            return result;
        }

        /// <summary>
        ///   Deletes an order from the pending submission storage.
        /// </summary>
        /// 
        /// <param name="log">The logging instance to use for emitting information.</param>
        /// <param name="storage">The storage to use for the order.</param>
        /// <param name="partner">The partner associated with the order.</param>
        /// <param name="orderId">The unique identifier of the order to retrieve the detials of.</param>
        /// <param name="correlationId">An optional identifier used to correlate activities across the disparate parts of processing, including external interations.</param>
        /// <param name="emulatedResult">An optional emulated result to use in place of interacting with storage.</param>
        /// 
        /// <returns>The result of the operation.</returns>
        /// 
        protected virtual async Task<OperationResult>DeletePendingOrderAsync(ILogger            log, 
                                                                             IOrderStorage      storage,
                                                                             string             partner,
                                                                             string             orderId,
                                                                             string             correlationId  = null,
                                                                             OperationResult    emulatedResult = null)
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
                    await storage.DeletePendingOrderAsync(partner, orderId);
                    
                    result = new OperationResult
                    {
                        Outcome     = Outcome.Success,
                        Reason      = String.Empty,
                        Recoverable = Recoverability.Final,
                        Payload     = String.Empty                        
                    };
                }                    

                log.Information("Order for {Partner}//{Order} has been deleted from the storage for pending sumbissions.  Emulated: {Emulated}.  Result: {Result}", 
                    partner, 
                    orderId,
                    (emulatedResult != null), 
                    result);
            }

            catch (Exception ex)
            {
                log.Error(ex, "An error occured while depeting {Partner}//{Order} from pending submissions.", partner, orderId);                
                return OperationResult.ExceptionResult;
            }

            return result;
        }

        /// <summary>
        ///   Stores an order as a final order which has completed submission.
        /// </summary>
        /// 
        /// <param name="log">The logging instance to use for emitting information.</param>
        /// <param name="storage">The storage to use for the order.</param>
        /// <param name="createOrderMessage">The CreateOrderMessage representing the order.</param>
        /// <param name="correlationId">An optional identifier used to correlate activities across the disparate parts of processing, including external interations.</param>
        /// <param name="emulatedResult">An optional emulated result to use in place of interacting with storage.</param>
        /// 
        /// <returns>The result of the operation.</returns>
        /// 
        protected virtual async Task<OperationResult> StoreOrderAsCompletedAsync(ILogger            log, 
                                                                                 IOrderStorage      storage,
                                                                                 CreateOrderMessage order,
                                                                                 string             correlationId  = null,
                                                                                 OperationResult    emulatedResult = null)
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
                    var key = await storage.SaveCompletedOrderAsync(order);
                    
                    result = new OperationResult
                    {
                        Outcome     = Outcome.Success,
                        Reason      = String.Empty,
                        Recoverable = Recoverability.Final,
                        Payload     = key                        
                    };
                }                    

                log.Information("Order details for {Partner}//{Order} have been saved as final.  Emulated: {Emulated}.  Result: {Result}", 
                    order?.Identity?.PartnerCode, 
                    order?.Identity?.PartnerOrderId, 
                    (emulatedResult != null), 
                    result);
            }

            catch (Exception ex)
            {
                log.Error(ex, "An error occured while saving {Partner}//{Order} as completed submission.", order.Identity.PartnerCode, order.Identity.PartnerOrderId);                
                return OperationResult.ExceptionResult;
            }

            return result;
        }

        /// <summary>
        ///   Submits an order to the order production service, signaling a request that it be produced.
        /// </summary>
        /// 
        /// <param name="log">The logging instance to use for emitting information.</param>
        /// <param name="client">The client to use for interacting with the order production service.</param>
        /// <param name="createOrderMessage">The CreateOrderMessage representing the order to be produced.</param>
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
        protected virtual async Task<OperationResult> SendOrderToProductionAsync(ILogger               log, 
                                                                                 IOrdeProductionClient client,
                                                                                 CreateOrderMessage    order,
                                                                                 string                correlationId  = null,
                                                                                 OperationResult       emulatedResult = null)
        {
            OperationResult result;

            try
            {
                result = emulatedResult ?? (await client.SubmitOrderForProductionAsync(order, correlationId));

                log.Information("The order {Partner}//{Order} has been submitted for production.  Emulated: {Emulated}.  Result: {Result}", 
                    order?.Identity?.PartnerCode,
                    order?.Identity?.PartnerOrderId,
                    (emulatedResult != null), 
                    result);
            }

            catch (Exception ex)
            {
                log.Error(ex, "An error occured while submitting the order {Partner}//{Order}", order?.Identity?.PartnerCode, order?.Identity?.PartnerOrderId);                
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
        protected virtual IAsyncPolicy<TResult> CreateRetryPolicy<TResult, TPayload>(Random rng,
                                                                                     int    maxRetryAttempts,
                                                                                     double exponentialBackoffSeconds,
                                                                                     double baseJitterSeconds) where TResult : OperationResult<TPayload>
        {
            return Policy
                .HandleResult<TResult>(result => ((result.Outcome != Outcome.Success) && (result.Recoverable == Recoverability.Retriable)))
                .Or<TimeoutException>()
                .WaitAndRetryAsync(maxRetryAttempts, attempt => TimeSpan.FromSeconds((Math.Pow(2, attempt) * exponentialBackoffSeconds) + (rng.NextDouble() * baseJitterSeconds)));
        }
    }
}
