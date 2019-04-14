using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Squire.Toolbox.Tests.Extensions
{
    /// <summary>
    ///     The suite of tests for the <see cref="Squire.Toolbox.Extensions.TaskExtensions" />
    ///     class.
    /// </summary>
    ///
    public class TaskExtensionsTests
    {
        /// <summary>A long delay which should never occur under normal circumstances.</summary>
        private static readonly TimeSpan LongDelay = TimeSpan.FromSeconds(5);

        /// <summary>A minimal delay to simulate an almost-immediate action.</summary>
        private static readonly TimeSpan TinyDelay = TimeSpan.FromMilliseconds(1);

        /// <summary>
        ///  Verifies functionality of the <see cref="Squire.Toolbox.Extensions.TaskExtensions.FireAndForget" />
        ///  method.
        /// </summary>
        ///
        [Fact]
        [TestCategory(Category.BuildVerification)]
        public async Task FireAndForgetInvokesTheExceptionActionWhenAnExceptionOccurs()
        {
            var expectedException = new ArithmeticException();
            var observedException = default(Exception);
            var completionSource  = new TaskCompletionSource<byte>(TaskCreationOptions.RunContinuationsAsynchronously);
            var invoked           = false;

            Action<Exception> exceptionAction = ex =>
            {
                invoked           = true;
                observedException = ex;

                completionSource.TrySetResult(0);
            };

            Task.Factory.StartNew( () =>
            {
                throw expectedException;

            }).FireAndForget(exceptionAction);

            // Wait for completion with timeout, just in case something goes wrong.

            var tokenSource = new CancellationTokenSource();

            if (completionSource.Task != await Task.WhenAny(completionSource.Task, Task.Delay(500, tokenSource.Token)))
            {
                tokenSource.Cancel();
            }

            // Verify the results.

            invoked.Should().BeTrue("because the exception action should have been invoked");
            observedException.Should().BeEquivalentTo(expectedException, "because the exception should have been passed to the handler");
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="Squire.Toolbox.Extensions.TaskExtensions.FireAndForget" />
        ///  method.
        /// </summary>
        ///
        [Fact]
        [TestCategory(Category.BuildVerification)]
        public async Task FireAndForgetDoesNotInvokeTheExceptionActionWhenNoExceptionOccurs()
        {
            var completionSource = new TaskCompletionSource<byte>(TaskCreationOptions.RunContinuationsAsynchronously);
            var invoked          = false;

            Action<Exception> exceptionAction = ex =>
            {
                invoked = true;
                completionSource.TrySetResult(0);
            };

            Task.Factory.StartNew(async () =>
            {
                await Task.Delay(TaskExtensionsTests.TinyDelay);
                completionSource.TrySetResult(0);
                return Task.CompletedTask;

            }).FireAndForget(exceptionAction);

            // Wait for completion with timeout, just in case something goes wrong.

            var tokenSource = new CancellationTokenSource();

            if (completionSource.Task != await Task.WhenAny(completionSource.Task, Task.Delay(500, tokenSource.Token)))
            {
                tokenSource.Cancel();
            }

            // Verify the results.

            invoked.Should().BeFalse("because the exception action should not have been invoked");
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="Squire.Toolbox.Extensions.TaskExtensions.WithTimeout" />
        ///  method.
        /// </summary>
        ///
        [Fact]
        [TestCategory(Category.BuildVerification)]
        public void WithTimeoutThrowsWhenATimeoutOccursAndNoActionIsSpecified()
        {
            Func<Task> actionUnderTest = async () =>
                await Task.Delay(TaskExtensionsTests.LongDelay).WithTimeout(TaskExtensionsTests.TinyDelay);

            actionUnderTest.Should().Throw<TimeoutException>("because the task did not complete within the timeout period");
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="Squire.Toolbox.Extensions.TaskExtensions.WithTimeout" />
        ///  method.
        /// </summary>
        ///
        [Fact]
        [TestCategory(Category.BuildVerification)]
        public async Task WithTimeoutExecutesTheActionWhenATimeoutOccurs()
        {
            var invoked = false;

            Action timeoutHandler = () => { invoked = true; };

            await Task.Delay(TaskExtensionsTests.LongDelay)
                .WithTimeout(TaskExtensionsTests.TinyDelay, null, timeoutHandler);

            invoked.Should().BeTrue("because the task did not complete within the timeout period");
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="Squire.Toolbox.Extensions.TaskExtensions.WithTimeout" />
        ///  method.
        /// </summary>
        ///
        [Fact]
        [TestCategory(Category.BuildVerification)]
        public void WithTimeoutGenericThrowsWhenATimeoutOccursAndNoActionIsSpecified()
        {
            Func<Task> actionUnderTest = async () =>
                await Task.Delay(TaskExtensionsTests.LongDelay)
                    .ContinueWith(_ => "blue")
                    .WithTimeout(TaskExtensionsTests.TinyDelay);

            actionUnderTest.Should().Throw<TimeoutException>("because the task did not complete within the timeout period");
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="Squire.Toolbox.Extensions.TaskExtensions.WithTimeout" />
        ///  method.
        /// </summary>
        ///
        [Fact]
        [TestCategory(Category.BuildVerification)]
        public async Task WithTimeoutGenericExecutesTheActionWhenATimeoutOccurs()
        {
            var invoked  = false;
            var expected = "blue";

            Func<string> timeoutHandler = () =>
            {
                invoked = true;
                return expected;
            };

            var actual = await Task.Delay(TaskExtensionsTests.LongDelay)
                .ContinueWith(_ => $"NOT { expected } ")
                .WithTimeout(TaskExtensionsTests.TinyDelay, null, timeoutHandler);

            invoked.Should().BeTrue("because the task did not complete within the timeout period");
            actual.Should().Be(expected, "because the timeout return value should have been used");
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="Squire.Toolbox.Extensions.TaskExtensions.WithTimeout" />
        ///  method.
        /// </summary>
        ///
        [Fact]
        [TestCategory(Category.BuildVerification)]
        public void WithTimeoutDoesNotThrowWhenATimeoutDoesNotOccurAndNoActionIsSpecified()
        {
            Func<Task> actionUnderTest = async () =>
                await Task.Delay(TaskExtensionsTests.TinyDelay).WithTimeout(TaskExtensionsTests.LongDelay);

            actionUnderTest.Should().NotThrow<TimeoutException>("because the task completed within the timeout period");
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="Squire.Toolbox.Extensions.TaskExtensions.WithTimeout" />
        ///  method.
        /// </summary>
        ///
        [Fact]
        [TestCategory(Category.BuildVerification)]
        public async Task WithTimeoutDoesNotExecuteTheActionWhenATimeoutDoesNotOccur()
        {
            var invoked = false;

            Action timeoutHandler = () => { invoked = true; };

            await Task.Delay(TaskExtensionsTests.TinyDelay)
                .WithTimeout(TaskExtensionsTests.LongDelay, null, timeoutHandler);

            invoked.Should().BeFalse("because the task completed within the timeout period");
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="Squire.Toolbox.Extensions.TaskExtensions.WithTimeout" />
        ///  method.
        /// </summary>
        ///
        [Fact]
        [TestCategory(Category.BuildVerification)]
        public void WithTimeoutGenericDoesNotThrowWhenATimeouDoesNotOccurAndNoActionIsSpecified()
        {
            Func<Task> actionUnderTest = async () =>
                await Task.Delay(TaskExtensionsTests.TinyDelay)
                    .ContinueWith(_ => "green")
                    .WithTimeout(TaskExtensionsTests.LongDelay);

            actionUnderTest.Should().NotThrow<TimeoutException>("because the task completed within the timeout period");
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="Squire.Toolbox.Extensions.TaskExtensions.WithTimeout" />
        ///  method.
        /// </summary>
        ///
        [Fact]
        [TestCategory(Category.BuildVerification)]
        public async Task WithTimeoutGenericDoesNotExecuteTheActionWhenATimeoutDoesNotOccur()
        {
            var invoked    = false;
            var unExpected = "blue";

            Func<string> timeoutHandler = () =>
            {
                invoked = true;
                return unExpected;
            };

            var actual = await Task.Delay(TaskExtensionsTests.TinyDelay)
                .ContinueWith(_ => $"NOT { unExpected } ")
                .WithTimeout(TaskExtensionsTests.LongDelay, null, timeoutHandler);

            invoked.Should().BeFalse("because the task completed within the timeout period");
            actual.Should().NotBe(unExpected, "because the timeout return value should not have been used");
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="Squire.Toolbox.Extensions.TaskExtensions.WithTimeout" />
        ///  method.
        /// </summary>
        ///
        [Fact]
        [TestCategory(Category.BuildVerification)]
        public async Task WithTimeoutGenericReturnsTheResultWhenATimeoutDoesNotOccur()
        {
            var expected = "blue";
            Func<string> timeoutHandler = () => $"NOT { expected } ";

            var actual = await Task.Delay(TaskExtensionsTests.TinyDelay)
                .ContinueWith(_ => expected)
                .WithTimeout(TaskExtensionsTests.LongDelay, null, timeoutHandler);

            actual.Should().Be(expected, "because the actual return value should have been used");
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="Squire.Toolbox.Extensions.TaskExtensions.WithTimeout" />
        ///  method.
        /// </summary>
        ///
        [Fact]
        [TestCategory(Category.BuildVerification)]
        public void WithTimeoutPropagatesAnExceptionWhenATimeoutDoesNotOccur()
        {
            Func<Task> actionUnderTest = async () =>
                await Task.Factory.StartNew( () => throw new MissingMethodException()).WithTimeout(TaskExtensionsTests.LongDelay);

            actionUnderTest.Should().Throw<MissingMethodException>("because a completed task that throws should have the exeception propagated");
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="Squire.Toolbox.Extensions.TaskExtensions.WithTimeout" />
        ///  method.
        /// </summary>
        ///
        [Fact]
        [TestCategory(Category.BuildVerification)]
        public void WithTimeoutGenericPropagatesAnExceptionWhenATimeoutDoesNotOccur()
        {
            Func<Task> actionUnderTest = async () =>
                await Task<string>.Factory.StartNew( () => throw new MissingMethodException()).WithTimeout(TaskExtensionsTests.LongDelay);

            actionUnderTest.Should().Throw<MissingMethodException>("because a completed task that throws should have the exeception propagated");
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="Squire.Toolbox.Extensions.TaskExtensions.WithTimeout" />
        ///  method.
        /// </summary>
        ///
        [Fact]
        [TestCategory(Category.BuildVerification)]
        public async Task WithTimeoutInvokesTheCancellationTokenWhenATimeoutOccurs()
        {
            using (var token = new CancellationTokenSource())
            {
                Action doNothing = () => {};
                await Task.Delay(TaskExtensionsTests.LongDelay).WithTimeout(TaskExtensionsTests.TinyDelay, token, doNothing);

                token.IsCancellationRequested.Should().BeTrue("because cancellation should have been requested at timeout");
            }
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="Squire.Toolbox.Extensions.TaskExtensions.WithTimeout" />
        ///  method.
        /// </summary>
        ///
        [Fact]
        [TestCategory(Category.BuildVerification)]
        public async Task WithTimeoutGenericInvokesTheCancellationTokenWhenATimeoutOccurs()
        {
            using (var token = new CancellationTokenSource())
            {
                await Task.Delay(TaskExtensionsTests.LongDelay)
                    .ContinueWith(_ => "hello")
                    .WithTimeout(TaskExtensionsTests.TinyDelay, token, () => String.Empty);

                token.IsCancellationRequested.Should().BeTrue("because cancellation should have been requested at timeout");
            }
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="Squire.Toolbox.Extensions.TaskExtensions.WithTimeout" />
        ///  method.
        /// </summary>
        ///
        [Fact]
        [TestCategory(Category.BuildVerification)]
        public async Task WithTimeoutHandlesUnobservedExceptionsWhenATimeoutOccurs()
        {
            var invoked          = false;
            var expected         = new FormatException();
            var observed         = default(Exception);
            var innerCompletion  = new TaskCompletionSource<byte>(TaskCreationOptions.RunContinuationsAsynchronously);
            var outerCompletion  = new TaskCompletionSource<byte>(TaskCreationOptions.RunContinuationsAsynchronously);

            Action timeoutHandler = () =>
            {
                innerCompletion.TrySetResult(0);
            };

            Action<Exception> exceptionHandler = ex =>
            {
                invoked  = true;
                observed = ex;
                outerCompletion.TrySetResult(0);
            };

            Func<Task> actionUnderTest = async () =>
            {
                await innerCompletion.Task;
                throw expected;
            };

            await actionUnderTest().WithTimeout(TaskExtensionsTests.TinyDelay, null, timeoutHandler, exceptionHandler);

            // The completion source will be set when either the exception handler runs, or the test operation
            // gives up on waiting for the timeout.

            await outerCompletion.Task.WithTimeout(TimeSpan.FromSeconds(2));

            invoked.Should().BeTrue("because the handler for unobserved exceptions should have been called");
            observed.Should().Be(expected, "because the handler should have received the exception thrown after the timeout occured");
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="Squire.Toolbox.Extensions.TaskExtensions.WithTimeout" />
        ///  method.
        /// </summary>
        ///
        [Fact]
        [TestCategory(Category.BuildVerification)]
        public async Task WithTimeoutGenericHandlesUnobservedExceptionsWhenATimeoutOccurs()
        {
            var invoked          = false;
            var expected         = new FormatException();
            var observed         = default(Exception);
            var innerCompletion  = new TaskCompletionSource<byte>(TaskCreationOptions.RunContinuationsAsynchronously);
            var outerCompletion  = new TaskCompletionSource<byte>(TaskCreationOptions.RunContinuationsAsynchronously);

            Func<string> timeoutHandler = () =>
            {
                innerCompletion.TrySetResult(0);
                return String.Empty;
            };

            Action<Exception> exceptionHandler = ex =>
            {
                invoked  = true;
                observed = ex;
                outerCompletion.TrySetResult(0);
            };

            Func<Task<string>> actionUnderTest = async () =>
            {
                await innerCompletion.Task;
                throw expected;
            };

            await actionUnderTest().WithTimeout(TaskExtensionsTests.TinyDelay, null, timeoutHandler, exceptionHandler);

            // The completion source will be set when either the exception handler runs, or the test operation
            // gives up on waiting for the timeout.

            await outerCompletion.Task.WithTimeout(TimeSpan.FromSeconds(2));

            invoked.Should().BeTrue("because the handler for unobserved exceptions should have been called");
            observed.Should().Be(expected, "because the handler should have received the exception thrown after the timeout occured");
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="Squire.Toolbox.Extensions.TaskExtensions.WithTimeout" />
        ///  method.
        /// </summary>
        ///
        [Fact]
        [TestCategory(Category.BuildVerification)]
        public void WithTimeoutPropagatesExceptionsForCompletedTasks()
        {
            var completedTask = Task.FromException(new InvalidCastException());

            Func<Task> actionUnderTest = async () => await completedTask.WithTimeout(TaskExtensionsTests.LongDelay);
            actionUnderTest.Should().Throw<InvalidCastException>("because the task was completed with an exception before requesting a timeout");
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="Squire.Toolbox.Extensions.TaskExtensions.WithTimeout" />
        ///  method.
        /// </summary>
        ///
        [Fact]
        [TestCategory(Category.BuildVerification)]
        public void WithTimeoutGenericIPropagatesExceptionsForCompletedTasks()
        {
            var completedTask = Task.FromException<string>(new DivideByZeroException());

            Func<Task> actionUnderTest = async () => await completedTask.WithTimeout(TaskExtensionsTests.LongDelay);
            actionUnderTest.Should().Throw<DivideByZeroException>("because the task was completed with an exception before requesting a timeout");
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="Squire.Toolbox.Extensions.TaskExtensions.WithTimeout" />
        ///  method.
        /// </summary>
        ///
        [Fact]
        [TestCategory(Category.BuildVerification)]
        public async Task WithTimeoutGenericIPropagatesResultsForCompletedTasks()
        {
            var expected      = "oh, hello, there.";
            var completedTask = Task.FromResult(expected);
            var actual        = await completedTask.WithTimeout(TaskExtensionsTests.LongDelay);

            actual.Should().Be(expected, "because the task was completed with a result before requesting a timeout");
        }
    }
}
