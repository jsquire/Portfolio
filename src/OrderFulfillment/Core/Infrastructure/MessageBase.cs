using System;
using Newtonsoft.Json;

namespace OrderFulfillment.Core.Infrastructure
{

    /// <summary>
    ///   Serves as the common base for messaging types, such as
    ///   commands and events.
    /// </summary>
    /// 
    public abstract class MessageBase
    {
        /// <summary>
        ///   The unique identifier of the message.
        /// </summary>
        /// 
       public Guid Id { get; set; }

        /// <summary>
        ///   The date and time, in UTC, that the message occurred.
        /// </summary>
        /// 
        public DateTime OccurredTimeUtc { get; set; }

        /// <summary>
        ///   The sequence number associated with the message, if any.
        ///   Used to help determine the order of messages if many are sent
        ///   at similar times.
        /// </summary>
        /// 
        public int? Sequence { get; set; }

        /// <summary>
        ///   An identifier that can be used to uniquely identify a single request across disparate systems.
        /// </summary>
        /// 
        public string CorrelationId { get; set; }

        /// <summary>
        ///   The identity of the current user when the message occurred. May be null for message
        ///   sent in response to observing an event from another system, or if produced by automated processing.
        /// </summary>
        /// 
        public string CurrentUser { get; set; }

        /// <summary>
        /// The date and time, in UTC, when the message was received locally
        /// </summary>
        /// 
        public DateTime? LocalReceivedTimeUtc { get; set; }

        /// <summary>
        ///   Creates a string that represents this instance.
        /// </summary>
        /// 
        /// <returns>The <see cref="System.String" /> representing this instance.</returns>
        /// 
        public override string ToString() => 
            JsonConvert.SerializeObject(this, Formatting.Indented);
    }
}
