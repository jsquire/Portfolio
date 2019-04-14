using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Squire.Toolbox.Extensions
{
    /// <summary>
    ///   The set of extension methods for the <see cref="Task" /> class.
    /// </summary>
    ///
    public static class TaskExtensions
    {
        /// <summary>
        ///   Executes a task without waiting for or observing its completion.
        /// </summary>
        ///
        /// <param name="instance">The target of this fire-and-forget.</param>
        /// <param name="errorHandler">An action to execute should an exception occur.</param>
        ///
        public static async void FireAndForget(this Task              instance,
                                                    Action<Exception> errorHandler = null)
        {
            if (instance == null)
            {
                return;
            }

            try
            {
                await instance.ConfigureAwait(false);
            }

            catch (Exception ex)
            {
                errorHandler?.Invoke(ex);
            }
        }

        /// <summary>
        ///   Executes a task without waiting for or observing its completion.
        /// </summary>
        ///
        /// <typeparam name="T">The type of return value of the task.</typeparam>
        ///
        /// <param name="instance">The target of this fire-and-forget.</param>
        /// <param name="errorHandler">An action to execute should an exception occur.</param>
        ///
        public static async void FireAndForget<T>(this Task<T>           instance,
                                                       Action<Exception> errorHandler = null)
        {
            if (instance == null)
            {
                return;
            }

            try
            {
                await instance.ConfigureAwait(false);
            }

            catch (Exception ex)
            {
                errorHandler?.Invoke(ex);
            }
        }

        /// <summary>
        ///   Executes a task with a timeout period, ignoring any result should the timeout period elapse.
        /// </summary>
        ///
        /// <param name="instance">The target of task execution.</param>
        /// <param name="timeout">The timeout period to allow for the <paramref name="instance"/> to complete.</param>
        /// <param name="cancellationToken">A cancellation token for signaling the <paramref name="instance" /> that a timeout has occurred and work should be stopped.</param>
        /// <param name="timeoutHandler">An action to take on timeout; if not specified, a <see cref="TimeoutException" /> will be thrown.</param>
        /// <param name="postTimeoutErrorHandler">An action to execute should an exception occur in the original task after it has timed out; if not specified, exceptions will go unobserved.</param>
        ///
        /// <returns>A task to be resolved on completion or timeout.</returns>
        ///
        public static async Task WithTimeout(this Task                    instance,
                                                  TimeSpan                timeout,
                                                  CancellationTokenSource cancellationToken       = null,
                                                  Action                  timeoutHandler          = null,
                                                  Action<Exception>       postTimeoutErrorHandler = null)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            if (instance.IsCompleted || Debugger.IsAttached)
            {
                instance.GetAwaiter().GetResult();
                return;
            }

            using (var timeoutTokenSource = new CancellationTokenSource())
            {
                if (instance == await Task.WhenAny(instance, Task.Delay(timeout, timeoutTokenSource.Token)))
                {
                    timeoutTokenSource.Cancel();
                    instance.GetAwaiter().GetResult();
                    return;
                }
            }

            // A timeout occured.  Perform the needed actions to request cancellation, allow the task to complete unobserved,
            // and to signal the caller.

            cancellationToken?.Cancel();
            instance.FireAndForget(postTimeoutErrorHandler);

            if (timeoutHandler != null)
            {
                timeoutHandler();
                return;
            }

            throw new TimeoutException();
        }

        /// <summary>
        ///   Executes a task with a timeout period, ignoring any result should the timeout period elapse.
        /// </summary>
        ///
        /// <typeparam name="T">The type of return value of the task.</typeparam>
        ///
        /// <param name="instance">The target of task execution.</param>
        /// <param name="timeout">The timeout period to allow for the <paramref name="instance"/> to complete.</param>
        /// <param name="cancellationToken">A cancellation token for signaling the <paramref name="instance" /> that a timeout has occurred and work should be stopped.</param>
        /// <param name="timeoutHandler">An action to take on timeout which is expected to produce the value of <typeparamref name="T"/> to be used as the result; if not specified, a <see cref="TimeoutException" /> will be thrown.</param>
        /// <param name="postTimeoutErrorHandler">An action to execute should an exception occur in the original task after it has timed out; if not specified, exceptions will go unobserved.</param>
        ///
        /// <returns>A task to be resolved on completion or timeout.</returns>
        ///
        public static async Task<T> WithTimeout<T>(this Task<T> instance,
                                                        TimeSpan timeout,
                                                        CancellationTokenSource cancellationToken       = null,
                                                        Func<T>                 timeoutHandler          = null,
                                                        Action<Exception>       postTimeoutErrorHandler = null)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            if (instance.IsCompleted || Debugger.IsAttached)
            {
                return await instance;
            }

            using (var timeoutTokenSource = new CancellationTokenSource())
            {
                if (instance == await Task.WhenAny(instance, Task.Delay(timeout, timeoutTokenSource.Token)))
                {
                    timeoutTokenSource.Cancel();
                    return instance.GetAwaiter().GetResult();
                }
            }

            // A timeout occured.  Perform the needed actions to request cancellation, allow the task to complete unobserved,
            // and to signal the caller.

            cancellationToken?.Cancel();
            instance.FireAndForget(postTimeoutErrorHandler);

            if (timeoutHandler != null)
            {
                return timeoutHandler();
            }

            throw new TimeoutException();
        }
    }
}
