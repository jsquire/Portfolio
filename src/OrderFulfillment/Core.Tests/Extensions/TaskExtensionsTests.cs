using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Moq;
using OrderFulfillment.Core.Extensions;
using System.Threading;

namespace OrderFulfillment.Core.Tests.Extensions
{
    /// <summary>
    ///   The suite of tests for the <see cref="Core.Extensions.TaskExtensions" />
    ///   class.
    /// </summary>
    /// 
    public class TaskExtensionsTests
    {
        /// <summary>
        ///  Verifies functionality of the <see cref="Core.Extensions.TaskExtensions.FireAndForget" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task FireAndForgetInvokesTheExceptionActionWhenAnExceptionOccurs()
        {
            var expectedException = new Exceptions.MissingDependencyException("OMG!");
            var observedException = default(Exception);
            var invoked           = false;

            Action<Exception> exceptionAction = ex =>
            {
                invoked           = true;
                observedException = ex;
            };

            Task.Factory.StartNew( () =>
            {
                throw expectedException;

            }).FireAndForget(exceptionAction);

            await Task.Delay(1000);
                        
            invoked.Should().BeTrue("because the exception action should have been invoked");
            observedException.ShouldBeEquivalentTo(expectedException, "because the exception should have been passed to the handler");
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="Core.Extensions.TaskExtensions.WithTimeout" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public void WithTimeoutThrowsWhenATimeoutOccurs()
        {
            var target = Task.Delay(1000);

            Func<Task> actionUnderTest = async () => await target.WithTimeout(TimeSpan.FromMilliseconds(1));
            actionUnderTest.ShouldThrow<TimeoutException>("because the task did not complete within the timeout period");
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="Core.Extensions.TaskExtensions.WithTimeout" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public void WithTimeoutGenericThrowsWhenATimeoutOccurs()
        {
            var target = Task.Delay(1000).ContinueWith( _ => "blue");

            Func<Task> actionUnderTest = async () => await target.WithTimeout(TimeSpan.FromMilliseconds(1));
            actionUnderTest.ShouldThrow<TimeoutException>("because the task did not complete within the timeout period");
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="Core.Extensions.TaskExtensions.WithTimeout" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public void WithTimeoutDoesNotThrowsWhenATimeoutDoesNotOccur()
        {
            var target = Task.Delay(1);

            Func<Task> actionUnderTest = async () => await target.WithTimeout(TimeSpan.FromMilliseconds(1000));
            actionUnderTest.ShouldNotThrow("because the task completed within the timeout period");
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="Core.Extensions.TaskExtensions.WithTimeout" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public void WithTimeoutGenericDoesNotThrowsWhenATimeoutDoesNotOccur()
        {
            var target = Task.Delay(1).ContinueWith( _ => "blue");;

            Func<Task> actionUnderTest = async () => await target.WithTimeout(TimeSpan.FromMilliseconds(1000));
            actionUnderTest.ShouldNotThrow("because the task completed within the timeout period");
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="Core.Extensions.TaskExtensions.WithTimeout" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task WithTimeoutGenericReturnsTheFalueWhenATimeoutDoesNotOccur()
        {
            var expected = "hello";
            var target   = Task.Delay(1).ContinueWith( _ => expected);

            var result = await target.WithTimeout(TimeSpan.FromMilliseconds(1000));
            result.Should().Be(expected, "because the result should be returned when no timeout occurs");
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="Core.Extensions.TaskExtensions.WithTimeout" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public void WithTimeoutPropagatesAnExceptionForACompletedTask()
        {
            var completionSource = new TaskCompletionSource<object>();

            completionSource.SetException(new MissingFieldException("oops"));

            Func<Task> actionUnderTest = async () => await completionSource.Task.WithTimeout(TimeSpan.FromMilliseconds(1000));
            actionUnderTest.ShouldThrow<MissingFieldException>("because an exception for a completed task should be propagated");
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="Core.Extensions.TaskExtensions.WithTimeout" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public void WithTimeoutPropagatesAnExceptionForThatCompletesBeforeTimeout()
        {
            var target = Task.Delay(1).ContinueWith( _ => throw new MissingMemberException("oh no"));

            Func<Task> actionUnderTest = async () => await target.WithTimeout(TimeSpan.FromMilliseconds(1000));
            actionUnderTest.ShouldThrow<MissingMemberException>("because an exception for a task that finishes before the timeout should be propagated");
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="Core.Extensions.TaskExtensions.WithTimeout" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public void WithTimeoutGenericPropagatesAnExceptionForThatCompletesBeforeTimeout()
        {
            var target = Task.Delay(1).ContinueWith<string>( _ => throw new MissingMemberException("oh no"));

            Func<Task> actionUnderTest = async () => await target.WithTimeout(TimeSpan.FromMilliseconds(1000));
            actionUnderTest.ShouldThrow<MissingMemberException>("because an exception for a task that finishes before the timeout should be propagated");
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="Core.Extensions.TaskExtensions.WithTimeout" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public void WithTimeouPropagatesACancelledTask()
        {
            var completionSource = new TaskCompletionSource<object>();
            completionSource.SetCanceled();

            Func<Task> actionUnderTest = async () => await completionSource.Task.WithTimeout(TimeSpan.FromMilliseconds(1000));
            actionUnderTest.ShouldThrow<TaskCanceledException>("because an exception for a completed task should be propagated");
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="Core.Extensions.TaskExtensions.WithTimeout" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task WithTimeoutInvokesTheCancellationTokenWhenATimeoutOccurs()
        {
            var target = Task.Delay(1000);
            var token  = new CancellationTokenSource();

            try
            {
                await target.WithTimeout(TimeSpan.FromMilliseconds(1), token);
            }

            catch (TimeoutException)
            {
                // Expected; do nothing
            }

            token.IsCancellationRequested.Should().BeTrue("because cancellation should be requested at timeout");
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="Core.Extensions.TaskExtensions.WithTimeout" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task WithTimeoutGenericInvokesTheCancellationTokenWhenATimeoutOccurs()
        {
            var target = Task.Delay(1000).ContinueWith( _ => "hello");
            var token  = new CancellationTokenSource();

            try
            {
                await target.WithTimeout(TimeSpan.FromMilliseconds(1), token);
            }

            catch (TimeoutException)
            {
                // Expected; do nothing
            }

            token.IsCancellationRequested.Should().BeTrue("because cancellation should be requested at timeout");
        }
    }
}
