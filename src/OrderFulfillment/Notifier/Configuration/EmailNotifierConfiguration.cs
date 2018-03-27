using OrderFulfillment.Core.Configuration;

namespace OrderFulfillment.Notifier.Configuration
{
    /// <summary>
    ///   The configuration needed to allow for email notifications to be
    ///   sent.
    /// </summary>
    /// 
    public class EmailNotifierConfiguration : IConfiguration
    {        
        /// <summary>
        ///   Indicates whether the email notification is enabled.
        /// </summary>
        /// 
        public bool Enabled { get;  set; }

        /// <summary>
        ///   The maximum number of retry counts for performing a given operation.  When these 
        ///   retries are exhuasted, the operation is considered a failure.
        /// </summary>
        /// 
        public int OperationRetryMaxCount { get;  set; }

        /// <summary>
        ///   The baseline number of seconds to apply when calculating the
        ///   exponential backoff for performing a retry.
        /// </summary>
        /// 

        public double OperationRetryExponentialSeconds { get;  set; }

        /// <summary>
        ///   The baseline number of seconds to combine with a random multiplier
        ///   when calculating the jitter for a retry.
        /// </summary>
        /// 
        public double OperationRetryJitterSeconds { get;  set; }

        /// <summary>
        ///   The address of the SMTP server host.
        /// </summary>
        /// 
        public string SmtpHostAddress { get;  set; }

        /// <summary>
        ///   The SMTP port in use.
        /// </summary>
        /// 
        public int SmtpPort { get;  set; }

        /// <summary>
        ///   The username for authenticating with the SMTP server.
        /// </summary>
        /// 
        public string SmtpUserName { get;  set; }

        /// <summary>
        ///   The password for authenticating with the SMTP server.
        /// </summary>
        /// 
        public string SmtpPasword { get;  set; }

        /// <summary>
        ///   The timeout to use when communicating with the SMTP server.
        /// </summary>
        /// 
        public int SmtpTimeoutMilliseconds { get;  set; }

        /// <summary>
        ///   The list of email addresses to send notifications to.
        /// </summary>
        /// 
        /// <value>
        ///   A comma-separated list.
        /// </value>
        /// 
        public string ToEmailAddressList { get;  set; }

        /// <summary>
        ///   The email address to use as the return address for notification email.
        /// </summary>
        /// 
        public string FromEmailAddress { get;  set; }

        /// <summary>
        ///   The subject to use for a failure notification email.
        /// </summary>
        /// 
        public string FailureNotificationSubject { get;  set; }

        /// <summary>
        ///   The body to use for a failure notification email.
        /// </summary>
        /// 
        /// <remarks>
        ///   This value may contain slugs for {partner}, {orderId}, and {correlationId} to 
        ///   be replaced when the email is formed.
        /// </remarks>
        /// 
        public string FailureNotificationBody { get;  set; }

        /// <summary>
        ///   The subject to use for a dead letter notification email.
        /// </summary>
        /// 
        public string DeadLetterNotificationSubject { get;  set; }

        /// <summary>
        ///   The body to use for a dead letter notification email.
        /// </summary>
        /// 
        /// <remarks>
        ///   This value may contain slugs for {location}, {partner}, {orderId}, and {correlationId} to 
        ///   be replaced when the email is formed.
        /// </remarks>
        /// 
        public string DeadLetterNotificationBody { get;  set; }
    }
}
