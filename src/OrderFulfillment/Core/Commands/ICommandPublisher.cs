using System.Threading.Tasks;
using NodaTime;

namespace OrderFulfillment.Core.Commands
{
    /// <summary>
    ///   Serves as a publsiher for a single command, or family of commands with 
    ///   a common ancestor.
    /// </summary>
    /// 
    /// <typeparam name="T">The type of command capable of being published</typeparam>
    /// 
    public interface ICommandPublisher<T> where T : CommandBase
    {
        /// <summary>
        ///   Publishes the specified command.
        /// </summary>
        /// 
        /// <param name="command">The command to publish.</param>
        /// <param name="publishTimeUtc">If provided, this value will defer publishing of the command until the specified UTC date/time.</param>
        /// 
        Task PublishAsync(T        command,
                          Instant? publishTimeUtc = null);

        /// <summary>
        ///   Attempts to publish the specified command.
        /// </summary>
        /// 
        /// <param name="command">The command to publish.</param>
        /// <param name="publishTimeUtc">If provided, this value will defer publishing of the command until the specified UTC date/time.</param>
        /// 
        /// <returns><c>true</c> if the command was successfully published; otherwise, <c>false</c>.</returns>
        /// 
        /// <remarks>
        ///     The implementor of this interface may or may not log failures; the contract
        ///     makes no demands one way or the other.
        /// </remarks>
        /// 
        Task<bool> TryPublishAsync(T        command,
                                   Instant? publishTimeUtc = null);
    }
}
