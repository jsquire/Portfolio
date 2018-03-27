using System;
using System.Threading;
using System.Threading.Tasks;

namespace OrderFulfillment.Core.Extensions
{
    /// <summary>
    ///   The set of extension methods for the <see cref="Task" /> class.
    /// </summary>
    /// 
    public static class TaskExtensions
    {
        /// <summary>
        ///   Allows a task to be run and the result ignored, with an optional action invoked
        ///   upon failure.
        /// </summary>
        /// 
        /// <param name="instance">The instance that this method was invoked on.</param>
        /// <param name="exceptionAction">If provided, this action will be invoked when an exception is observed.</param>
        /// 
        /// <remarks>
        ///   This method intentionally does not return the task on which the instance was called, despite awaiting.  This
        ///   is because the result is not intended to be observed and doing so avoids propagaging the need for the async decoration
        ///   
        /// </remarks>
        /// 
        public static async void FireAndForget(this Task              instance,
                                                    Action<Exception> exceptionAction = null)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            try
            {
                await instance.ConfigureAwait(false);
            }

            catch (Exception ex)
            {
                exceptionAction?.Invoke(ex);
            }
        }

        /// <summary>
        ///   Allows a task to be run with a timeout specified.  If the task has not been 
        ///   completed within the timeout period, it will signaled for cancellation and then
        ///   be run to completion in the background.  Any observed background exceptions will be
        ///   passed to the optional exception action.
        /// </summary>
        /// 
        /// <param name="instance">The instance that this method was invoked on.</param>
        /// <param name="timeout">The timeout period to allow for.</param>
        /// <param name="cancellationToken">The cancellation token to signal if the timeout period has elapsed.</param>
        /// <param name="exceptionAction">If provided, this action will be invoked when an exception is observed.</param>
        /// 
        /// <returns>The specified task, if it compelted within the timeout period requested.</returns>
        /// 
        /// <exception cref="TimeoutException">Thrown if the timeout period has elapsed before the specified task completed.</exception>
        /// 
        public static Task WithTimeout(this Task                    instance,
                                            TimeSpan                timeout,
                                            CancellationTokenSource cancellationToken = null,
                                            Action<Exception>       exceptionAction   = null)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            // If the task has already completed, then simply return it.

            if (instance.IsCompleted)
            {
                return instance;
            }

            return TaskExtensions.WithTimeoutInternal(instance, timeout, cancellationToken, exceptionAction);
        }

        /// <summary>
        ///   Allows a task to be run with a timeout specified.  If the task has not been 
        ///   completed within the timeout period, it will signaled for cancellation and then
        ///   be run to completion in the background.  Any observed background exceptions will be
        ///   passed to the optional exception action.
        /// </summary>
        /// 
        /// <typeparam name="T">The return type of the task being evaluated against the timeout.</typeparam>
        /// 
        /// <param name="instance">The instance that this method was invoked on.</param>
        /// <param name="timeout">The timeout period to allow for.</param>
        /// <param name="cancellationToken">The cancellation token to signal if the timeout period has elapsed.</param>
        /// <param name="exceptionAction">If provided, this action will be invoked when an exception is observed.</param>
        /// 
        /// <returns>The specified task, if it compelted within the timeout period requested.</returns>
        /// 
        /// <exception cref="TimeoutException">Thrown if the timeout period has elapsed before the specified task completed.</exception>
        /// 
        public static Task<T> WithTimeout<T>(this Task<T>                 instance,
                                                  TimeSpan                timeout,
                                                  CancellationTokenSource cancellationToken = null,
                                                  Action<Exception>       exceptionAction   = null)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            // If the task has already completed, then simply return it.

            if (instance.IsCompleted)
            {
                return instance;
            }

            return TaskExtensions.WithTimeoutInternal(instance, timeout, cancellationToken, exceptionAction);
        }

        /// <summary>
        ///   Implements the main set of operations for the WithTimeout method.  This helper is needed due to the need
        ///   to await the task to ensure any exceptions are observed.  Because the public method wishes to directly 
        ///   return the task if it has already been completed, it cannot use "async" in the signature.
        /// </summary>
        /// 
        /// <param name="target">The task that is being run.</param>
        /// <param name="timeout">The timeout period to allow for.</param>
        /// <param name="cancellationToken">The cancellation token to signal if the timeout period has elapsed.</param>
        /// <param name="exceptionAction">If provided, this action will be invoked when an exception is observed.</param>
        /// 
        /// <returns>The specified task, if it compelted within the timeout period requested.</returns>
        /// 
        /// <exception cref="TimeoutException">Thrown if the timeout period has elapsed before the specified task completed.</exception>
        /// 
        private static async Task WithTimeoutInternal(Task                    target,
                                                      TimeSpan                timeout,
                                                      CancellationTokenSource cancellationToken,
                                                      Action<Exception>       exceptionAction)
        {
            await Task.WhenAny(target, Task.Delay(timeout));

            // If the target task is completed, then it has done so within the allowable timeout
            // period.

            if (target.IsCompleted)
            {
                await target;
            }
            else
            {
                // The timeout occured.  Run the task to completion in the background to capture 
                // any observed exceptions.

                cancellationToken?.Cancel();
                target.FireAndForget(exceptionAction);

                throw new TimeoutException($"Timeout has occurred after { timeout }");
            }
        }

        /// <summary>
        ///   Implements the main set of operations for the WithTimeout method.  This helper is needed due to the need
        ///   to await the task to ensure any exceptions are observed.  Because the public method wishes to directly 
        ///   return the task if it has already been completed, it cannot use "async" in the signature.
        /// </summary>
        /// 
        /// <typeparam name="T">The return type of the task being evaluated against the timeout.</typeparam>
        /// 
        /// <param name="target">The task that is being run.</param>
        /// <param name="timeout">The timeout period to allow for.</param>
        /// <param name="cancellationToken">The cancellation token to signal if the timeout period has elapsed.</param>
        /// <param name="exceptionAction">If provided, this action will be invoked when an exception is observed.</param>
        /// 
        /// <returns>The specified task, if it compelted within the timeout period requested.</returns>
        /// 
        /// <exception cref="TimeoutException">Thrown if the timeout period has elapsed before the specified task completed.</exception>
        /// 
        private static async Task<T> WithTimeoutInternal<T>(Task<T>                 target,
                                                            TimeSpan                timeout,
                                                            CancellationTokenSource cancellationToken,
                                                            Action<Exception>       exceptionAction)
        {
            await Task.WhenAny(target, Task.Delay(timeout));

            // If the target task is completed, then it has done so within the allowable timeout
            // period.

            if (target.IsCompleted)
            {
                return await target;
            }
            else
            {
                // The timeout occured.  Run the task to completion in the background to capture 
                // any observed exceptions.

                cancellationToken?.Cancel();
                target.FireAndForget(exceptionAction);

                throw new TimeoutException($"Timeout has occurred after { timeout }");
            }
        }
    }
}
