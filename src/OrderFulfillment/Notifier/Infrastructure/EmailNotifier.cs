using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using OrderFulfillment.Core.Extensions;
using OrderFulfillment.Core.External;
using OrderFulfillment.Core.Infrastructure;
using OrderFulfillment.Core.Models.External.Ecommerce;
using OrderFulfillment.Core.Models.External.OrderProduction;
using OrderFulfillment.Core.Models.Operations;
using OrderFulfillment.Core.Storage;
using OrderFulfillment.Notifier.Configuration;
using Newtonsoft.Json;
using Polly;
using Serilog;

namespace OrderFulfillment.Notifier.Infrastructure
{
    /// <summary>
    ///   Performs thet acitons needed to submit an order for production.
    /// </summary>
    /// 
    /// <seealso cref="INotifier" />
    /// 
    public class EmailNotifier : INotifier
    {
        /// <summary>The characters to use for splitting a list of email addresses.</summary>
        private static readonly char[] EmailAddressListSplitChars = new[] { ',', ';' };

        /// <summary>The configuration to use for influencing behavior.</summary>
        private readonly EmailNotifierConfiguration configuration;

        /// <summary>The generator to use for random numbers.</summary>
        private readonly Random rng;

        /// <summary>The set of email addresses to use as recipients.</summary>
        private readonly IEnumerable<string> emailToAddresses;

        /// <summary>
        ///   The logger to be used for emitting telemetry from the processor.
        /// </summary>
        /// 
        private ILogger Log { get; }

        /// <summary>
        ///   Initializes a new instance of the <see cref="EmailNotifier"/> class.
        /// </summary>
        /// 
        /// <param name="configuration">The configuration to use for influencing behavior of the notifier.</param>
        /// <param name="logger">The logger to be used for emitting telemetry from the controller.</param>
        /// <param name="jsonSerializerSettings">The settings to use for JSON serializerion.</param>
        /// 
        public EmailNotifier(EmailNotifierConfiguration configuration,
                             ILogger                    logger)
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.Log           = logger        ?? throw new ArgumentNullException(nameof(logger));

            this.emailToAddresses = this.SplitEmailAddressList(EmailNotifier.EmailAddressListSplitChars, configuration.ToEmailAddressList);
            this.rng              = new Random();
        }

        /// <summary>
        ///   Performs the actions needed to notify of an order failure.
        /// </summary>
        /// 
        /// <param name="partner">The partner associated with the order.</param>
        /// <param name="orderId">The unique identifier of the order.</param>
        /// <param name="correlationId">An optional identifier used to correlate activities across the disparate parts of processing, including external interations.</param>
        /// 
        /// <returns>The result of the operation.</returns>
        /// 
        public async Task<OperationResult> NotifyOfOrderFailureAsync(string partner,
                                                                     string orderId,                                                       
                                                                     string correlationId = null)
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
            log.Information("Notification of failure for {Partner}//{Order} has begun.", partner, orderId);
            
            try
            {
                var successResult = new OperationResult
                {
                    Outcome     = Outcome.Success,
                    Reason      = String.Empty,
                    Recoverable = Recoverability.Final,
                    Payload     = String.Empty
                };

                // If the notifier is disabled, then take no action.

                if (!this.configuration.Enabled)
                {
                    log.Information("Notification was disabled; notification not sent for {Partner}//{Order}.", partner, orderId);                
                    return successResult;
                }

                // Create the body of the email to be sent.

                var body = this.configuration.FailureNotificationBody
                    ?.Replace(EmailBodyTokens.Partner, partner)
                    ?.Replace(EmailBodyTokens.Order, orderId)
                    ?.Replace(EmailBodyTokens.Correlation, correlationId);

                // Create and send the email.

                using (var message = this.CreateMessage(this.configuration.FailureNotificationSubject, body, this.configuration.FromEmailAddress, this.emailToAddresses))
                using (var client  = this.CreateSmtpClient(this.configuration))
                {
                    var config       = this.configuration;
                    var retryPolicy  = this.CreateRetryPolicy(this.rng, config.OperationRetryMaxCount, config.OperationRetryExponentialSeconds, config.OperationRetryJitterSeconds);
                    var policyResult = await retryPolicy.ExecuteAndCaptureAsync( () => Task.Run( () => this.SendMessage(client, message)));

                    if (policyResult.Outcome == OutcomeType.Failure)
                    {
                        throw policyResult.FinalException ?? new SmtpException("Unable to send the notification email");
                    }
                }   

                // Notification is complete.
                
                log.Information("Notification of failure for {Partner}//{Order} was successful.", partner, orderId);                
                return successResult;

            }
            catch (Exception ex)
            {
                log.Error(ex, "An exception occurred during notification for {Partner}//{Order}", partner, orderId);                
                return OperationResult.ExceptionResult;
            }
        }

        /// <summary>
        ///   Performs the actions needed to notify of a message stuck in a dead letter area.
        /// </summary>
        /// 
        /// <param name="location">The location of the dead letter message.</param>
        /// <param name="partner">The partner associated with the order.</param>
        /// <param name="orderId">The unique identifier of the order.</param>
        /// <param name="correlationId">An optional identifier used to correlate activities across the disparate parts of processing, including external interations.</param>
        /// 
        /// <returns>The result of the operation.</returns>
        /// 
        public async Task<OperationResult> NotifyDeadLetterMessageAsync(string location,
                                                                  string partner,
                                                                  string orderId,                                                       
                                                                  string correlationId = null)
        {
            if (String.IsNullOrEmpty(location))
            {
                throw new ArgumentException("The location must be provided.", nameof(location));
            }

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
            log.Information("Notification of dead letter {DeadLetterLocation} for {Partner}//{Order} has begun.", location, partner, orderId);
            
            try
            {
                var successResult = new OperationResult
                {
                    Outcome     = Outcome.Success,
                    Reason      = String.Empty,
                    Recoverable = Recoverability.Final,
                    Payload     = String.Empty
                };

                // If the notifier is disabled, then take no action.

                if (!this.configuration.Enabled)
                {
                    log.Information("Notification was disabled; notification not sent for dead letter {DeadLetterLocation} {Partner}//{Order}.", location, partner, orderId);                
                    return successResult;
                }

                // Create the body of the email to be sent.

                var body = this.configuration.DeadLetterNotificationBody
                    ?.Replace(EmailBodyTokens.DeadLetterLocation, location)
                    ?.Replace(EmailBodyTokens.Partner, partner)
                    ?.Replace(EmailBodyTokens.Order, orderId)
                    ?.Replace(EmailBodyTokens.Correlation, correlationId);

                // Create and send the email.

                using (var message = this.CreateMessage(this.configuration.DeadLetterNotificationSubject, body, this.configuration.FromEmailAddress, this.emailToAddresses))
                using (var client  = this.CreateSmtpClient(this.configuration))
                {
                    var config       = this.configuration;
                    var retryPolicy  = this.CreateRetryPolicy(this.rng, config.OperationRetryMaxCount, config.OperationRetryExponentialSeconds, config.OperationRetryJitterSeconds);
                    var policyResult = await retryPolicy.ExecuteAndCaptureAsync( () => Task.Run( () => this.SendMessage(client, message)));

                    if (policyResult.Outcome == OutcomeType.Failure)
                    {
                        throw policyResult.FinalException ?? new SmtpException("Unable to send the notification email");
                    }
                }   

                // Notification is complete.
                
                log.Information("Notification of dead letter {DeadLetterLocation} for {Partner}//{Order} was successful.", location, partner, orderId);                
                return successResult;

            }
            catch (Exception ex)
            {
                log.Error(ex, "An exception occurred during notificatoin of dead letter {DeadLetterLocation} for {Partner}//{Order}", location, partner, orderId);                
                return OperationResult.ExceptionResult;
            }
        }
        
        /// <summary>
        ///   Performs the actions needed to create an SMTP client that can be used to
        ///   sent email.
        /// </summary>
        /// 
        /// <param name="configuration">The configuration to use for configuring the SMTP client.</param>
        /// 
        /// <returns>The SMTP client with the requested configuration</returns>
        /// 
        protected virtual SmtpClient CreateSmtpClient(EmailNotifierConfiguration configuration) =>
            new SmtpClient
            {
                Host                  = configuration.SmtpHostAddress,
                Port                  = configuration.SmtpPort,
                DeliveryMethod        = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials           = new NetworkCredential(configuration.SmtpUserName, configuration.SmtpPasword),
                Timeout               = configuration.SmtpTimeoutMilliseconds
            };

        /// <summary>
        ///   Creates the message to be sent as email.
        /// </summary>
        /// 
        /// <param name="subject">The subject of the email.</param>
        /// <param name="body">The body template to use.</param>
        /// <param name="fromAddress">The address to use as the sender's address.</param>
        /// <param name="sendToAddresses">The set of email addresses for the recipients of the message.</param>
        /// 
        /// <returns>The message to be sent as email.</returns>
        /// 
        protected virtual MailMessage CreateMessage(string              subject,
                                                    string              body,
                                                    string              fromAddress,
                                                    IEnumerable<string> sendToAddresses)
        {
            var message = new MailMessage
            {
                BodyEncoding = Encoding.UTF8,
                Subject      = subject ?? String.Empty,
                Body         = body ?? String.Empty,
                From         = new MailAddress(fromAddress ?? String.Empty)
                
            };
            
            foreach (var recipient in sendToAddresses)
            {
                message.To.Add(new MailAddress(recipient));
            }

            return message;
        }

        /// <summary>
        ///   Sends the message via SMTP email.
        /// </summary>
        /// 
        /// <param name="client">The SMTP client to use for sending the message.</param>
        /// <param name="message">The message to send.</param>
        /// 
        protected virtual void SendMessage(SmtpClient client,
                                           MailMessage message)
        {
            client.Send(message);
        }

        /// <summary>
        ///   Splits an email address list into a set of individual addresses.
        /// </summary>
        /// 
        /// <param name="splitCharacters">The set of characters to split on.</param>
        /// <param name="emailAddressList">The list of email addresses to consider.</param>
        /// 
        /// <returns>The set of email adresses that were split from the list.</returns>
        /// 
        protected virtual IEnumerable<string> SplitEmailAddressList(char[] splitCharacters,
                                                                    string emailAddressList)
        {
            if (String.IsNullOrEmpty(emailAddressList))
            {
                return Enumerable.Empty<string>();
            }

            return emailAddressList
                .Split(splitCharacters, StringSplitOptions.RemoveEmptyEntries)
                .Select(address => address.Trim())
                .Where(address => !String.IsNullOrWhiteSpace(address));
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
        protected virtual IAsyncPolicy CreateRetryPolicy(Random rng,
                                                         int    maxRetryAttempts,
                                                         double exponentialBackoffSeconds,
                                                         double baseJitterSeconds)
        {
            return Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(maxRetryAttempts, attempt => TimeSpan.FromSeconds((Math.Pow(2, attempt) * exponentialBackoffSeconds) + (rng.NextDouble() * baseJitterSeconds)));
        }
    }
}
