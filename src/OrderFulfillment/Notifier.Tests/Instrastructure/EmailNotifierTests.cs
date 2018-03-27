using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using FluentAssertions;
using OrderFulfillment.Core.Exceptions;
using OrderFulfillment.Core.External;
using OrderFulfillment.Core.Models.External.OrderProduction;
using OrderFulfillment.Core.Models.Operations;
using OrderFulfillment.Core.Storage;
using OrderFulfillment.Notifier.Configuration;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Serilog;
using Xunit;

using UnderTest = OrderFulfillment.Notifier.Infrastructure;

namespace OrderFulfillment.Notifier.Tests.Instrastructure
{
    /// <summary>
    ///   The suite of tests for the <see cref="UnderTestTest.Notifier" />
    /// </summary>
    /// 
    public class NotifierTests
    {
        /// <summary>
        ///   Verifies behavior of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesTheConfiguration()
        {
            Action actionUnderTest = () => new UnderTest.EmailNotifier(null, Mock.Of<ILogger>());
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the configuration is required");
        }

        /// <summary>
        ///   Verifies behavior of the constructor.
        /// </summary>
        /// 
        [Fact]
        public void ConstructorValidatesTheLogger()
        {
            Action actionUnderTest = () => new UnderTest.EmailNotifier(new EmailNotifierConfiguration(), null);
            actionUnderTest.ShouldThrow<ArgumentNullException>("because the logger is required");
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="UnderTest.EmailNotifier.NotifyOfOrderFailureAsync" />
        ///  method.
        /// </summary>
        /// 
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void NotifyOfOrderFailureAsyncValidatesThePartner(string partner)
        {
            var notifier =  new UnderTest.EmailNotifier(new EmailNotifierConfiguration(), Mock.Of<ILogger>());
            
            Action actionUnderTest = () => notifier.NotifyOfOrderFailureAsync(partner, "ABC", "blue").GetAwaiter().GetResult();
            actionUnderTest.ShouldThrow<ArgumentException>("because the partner is required").And.ParamName.Should().Be(nameof(partner), "because the partner was invalid");
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="UnderTest.EmailNotifier.NotifyOfOrderFailureAsync" />
        ///  method.
        /// </summary>
        /// 
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void NotifyOfOrderFailureAsyncValidatesTheOrder(string orderId)
        {
            var notifier =  new UnderTest.EmailNotifier(new EmailNotifierConfiguration(), Mock.Of<ILogger>());
            
            Action actionUnderTest = () => notifier.NotifyOfOrderFailureAsync("Bob", orderId, "blue").GetAwaiter().GetResult();
            actionUnderTest.ShouldThrow<ArgumentException>("because the orderId is required").And.ParamName.Should().Be(nameof(orderId), "because the orderId was invalid");
        }
        
        /// <summary>
        ///  Verifies functionality of the <see cref="UnderTest.EmailNotifier.NotifyOfOrderFailureAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task NotifyOfOrderFailureAsyncReturnsTheExceptionResultIfCreatingTheMessageThrows()
        {
            var config     = new EmailNotifierConfiguration { Enabled = true };
            var mockLogger = new Mock<ILogger>();
            var notifier   = new Mock<UnderTest.EmailNotifier>(config, mockLogger.Object) { CallBase = true };

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            notifier
                .Protected()
                .Setup<MailMessage>("CreateMessage", ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<IEnumerable<string>>())
                .Throws(new MissingDependencyException());

            var result = await notifier.Object.NotifyOfOrderFailureAsync("ABC", "123");
            
            result.Should().NotBeNull("becuase a result should have been returned");
            result.ShouldBeEquivalentTo(OperationResult.ExceptionResult, "because the exception should short-circuit the processing");

        }

        /// <summary>
        ///  Verifies functionality of the <see cref="UnderTest.EmailNotifier.NotifyOfOrderFailureAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task NotifyOfOrderFailureAsyncReturnsTheExceptionResultIfCreatingTheClientThrows()
        {
            var config     = new EmailNotifierConfiguration { Enabled = true };
            var mockLogger = new Mock<ILogger>();
            var notifier   = new Mock<UnderTest.EmailNotifier>(config, mockLogger.Object) { CallBase = true };

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            notifier
                .Protected()
                .Setup<MailMessage>("CreateMessage", ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<IEnumerable<string>>())
                .Returns(new MailMessage());

            notifier
                .Protected()
                .Setup<SmtpClient>("CreateSmtpClient", ItExpr.IsAny<EmailNotifierConfiguration>())
                .Throws(new MissingDependencyException());

            var result = await notifier.Object.NotifyOfOrderFailureAsync("ABC", "123");
            
            result.Should().NotBeNull("becuase a result should have been returned");
            result.ShouldBeEquivalentTo(OperationResult.ExceptionResult, "because the exception should short-circuit the processing");

        }

        /// <summary>
        ///  Verifies functionality of the <see cref="UnderTest.EmailNotifier.NotifyOfOrderFailureAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task NotifyOfOrderFailureAsyncReturnsTheExceptionResultIfSendingTheMessageThrows()
        {
            var config     = new EmailNotifierConfiguration { Enabled = true };
            var mockLogger = new Mock<ILogger>();
            var notifier   = new Mock<UnderTest.EmailNotifier>(config, mockLogger.Object) { CallBase = true };

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            notifier
                .Protected()
                .Setup<MailMessage>("CreateMessage", ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<IEnumerable<string>>())
                .Returns(new MailMessage());

            notifier
                .Protected()
                .Setup<SmtpClient>("CreateSmtpClient", ItExpr.IsAny<EmailNotifierConfiguration>())
                .Returns(new SmtpClient());

            notifier
                .Protected()
                .Setup("SendMessage", ItExpr.IsAny<SmtpClient>(), ItExpr.IsAny<MailMessage>())
                .Throws(new NullReferenceException());

            var result = await notifier.Object.NotifyOfOrderFailureAsync("ABC", "123");
            
            result.Should().NotBeNull("becuase a result should have been returned");
            result.ShouldBeEquivalentTo(OperationResult.ExceptionResult, "because the exception should short-circuit the processing");

        }

        /// <summary>
        ///  Verifies functionality of the <see cref="UnderTest.EmailNotifier.NotifyOfOrderFailureAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task NotifyOfOrderFailureAsyncHonorsTheRetryPolicyIfSendingTheMessageThrows()
        {
            var config     = new EmailNotifierConfiguration { Enabled = true, OperationRetryMaxCount = 3, OperationRetryExponentialSeconds = 0, OperationRetryJitterSeconds = 0 };
            var mockLogger = new Mock<ILogger>();
            var notifier   = new Mock<UnderTest.EmailNotifier>(config, mockLogger.Object) { CallBase = true };
            var retries    = 0;

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            notifier
                .Protected()
                .Setup<MailMessage>("CreateMessage", ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<IEnumerable<string>>())
                .Returns(new MailMessage());

            notifier
                .Protected()
                .Setup<SmtpClient>("CreateSmtpClient", ItExpr.IsAny<EmailNotifierConfiguration>())
                .Returns(new SmtpClient());

            notifier
                .Protected()
                .Setup("SendMessage", ItExpr.IsAny<SmtpClient>(), ItExpr.IsAny<MailMessage>())
                .Callback( () => ++retries)
                .Throws(new NullReferenceException());

            var result = await notifier.Object.NotifyOfOrderFailureAsync("ABC", "123");
            
            retries.Should().Be(config.OperationRetryMaxCount + 1, "because a failure should trigger the retry policy");
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="UnderTest.EmailNotifier.NotifyOfOrderFailureAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task NotifyOfOrderFailureAsyncRespectsTheEnabledFlag()
        {
            var config     = new EmailNotifierConfiguration { Enabled = false };
            var mockLogger = new Mock<ILogger>();
            var notifier   = new Mock<UnderTest.EmailNotifier>(config, mockLogger.Object) { CallBase = true };

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);
                
            var result = await notifier.Object.NotifyOfOrderFailureAsync("ABC", "123");

            notifier
                .Protected()
                .Verify("CreateMessage", Times.Never(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<IEnumerable<string>>());

            notifier
                .Protected()
                .Verify("CreateSmtpClient", Times.Never(), ItExpr.IsAny<EmailNotifierConfiguration>());

            notifier
                .Protected()
                .Verify("SendMessage", Times.Never(), ItExpr.IsAny<SmtpClient>(), ItExpr.IsAny<MailMessage>());
            
            result.Should().NotBeNull("becuase a result should have been returned");
            result.Outcome.Should().Be(Outcome.Success, "because a skipped notification should be successful");
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="UnderTest.EmailNotifier.NotifyOfOrderFailureAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task NotifyOfOrderFailureAsyncSendsTheEmailNotification()
        {
            var config     = new EmailNotifierConfiguration { Enabled = true };
            var mockLogger = new Mock<ILogger>();
            var notifier   = new Mock<UnderTest.EmailNotifier>(config, mockLogger.Object) { CallBase = true };
            var message    = new MailMessage();
            var client     = new SmtpClient();

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            notifier
                .Protected()
                .Setup<MailMessage>("CreateMessage", ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<IEnumerable<string>>())
                .Returns(message);

            notifier
                .Protected()
                .Setup<SmtpClient>("CreateSmtpClient", ItExpr.IsAny<EmailNotifierConfiguration>())
                .Returns(client);

            notifier
                .Protected()
                .Setup("SendMessage", ItExpr.Is<SmtpClient>(value => value == client), ItExpr.Is<MailMessage>(value => value == message))
                .Verifiable("The message should have been sent");

            var result = await notifier.Object.NotifyOfOrderFailureAsync("ABC", "123");
            
            result.Should().NotBeNull("becuase a result should have been returned");
            result.Outcome.Should().Be(Outcome.Success, "because the notification should be successful");

            notifier.VerifyAll();
        }    
        
        /// <summary>
        ///  Verifies functionality of the <see cref="UnderTest.EmailNotifier.NotifyOfOrderFailureAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task NotifyOfOrderFailureAsyncBuildsTheCorrectMessage()
        {
            var config = new EmailNotifierConfiguration 
            { 
                Enabled                    = true,
                FailureNotificationBody    = "{partner}/{orderId}/{correlationId}",
                FailureNotificationSubject = "omg! subject",
                FromEmailAddress           = "someone@somewhere.com",
                ToEmailAddressList         = "one@here.com,two@here.com"                
            };

            var mockLogger  = new Mock<ILogger>();
            var notifier    = new Mock<UnderTest.EmailNotifier>(config, mockLogger.Object) { CallBase = true };
            var client      = new SmtpClient();
            var message     = default(MailMessage);
            var partner     = "ABC";
            var order       = "345";
            var correlation = "DEF456";

            var expectedBody = config.FailureNotificationBody
                .Replace("{partner}", partner)
                .Replace("{orderId}", order)
                .Replace("{correlationId}", correlation);            

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            notifier
                .Protected()
                .Setup<SmtpClient>("CreateSmtpClient", ItExpr.IsAny<EmailNotifierConfiguration>())
                .Returns(client);

            notifier
                .Protected()
                .Setup("SendMessage", ItExpr.Is<SmtpClient>(value => value == client), ItExpr.IsAny<MailMessage>())
                .Callback<SmtpClient, MailMessage>( (clientParam, messageParam) => message = messageParam);

            var result = await notifier.Object.NotifyOfOrderFailureAsync(partner, order, correlation);
            
            result.Should().NotBeNull("becuase a result should have been returned");
            result.Outcome.Should().Be(Outcome.Success, "because the notification should be successful");

            message.Should().NotBeNull("because the message should have been sent");
            message.Subject.Should().Be(config.FailureNotificationSubject, "because the configuration subject should be used");
            message.Body.Should().Be(expectedBody, "because the body should have been populated from the configuration and parameers");
            message.From.Should().Be(config.FromEmailAddress);

            foreach (var recipient in config.ToEmailAddressList.Split(','))
            {
                message.To.Any(to => to.Address == recipient).Should().BeTrue("because the recipient {0} should be on the message", recipient);
            }
        }    

        /// <summary>
        ///  Verifies functionality of the <see cref="UnderTest.EmailNotifier.NotifyOfOrderFailureAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task NotifyOfOrderFailureAsyncBuildsTheCorrectClient()
        {
            var config = new EmailNotifierConfiguration 
            { 
                Enabled                 = true,
                SmtpHostAddress         = "server.thing.com",
                SmtpPort                = 1234,
                SmtpUserName            = "birb",
                SmtpPasword             = "fly",
                SmtpTimeoutMilliseconds = 4
            };

            var mockLogger  = new Mock<ILogger>();
            var notifier    = new Mock<UnderTest.EmailNotifier>(config, mockLogger.Object) { CallBase = true };
            var client      = default(SmtpClient);
            var message     = new MailMessage();
            var partner     = "ABC";
            var order       = "345";
            var correlation = "DEF456";      

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            notifier
                .Protected()
                .Setup<MailMessage>("CreateMessage", ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<IEnumerable<string>>())
                .Returns(message);

            notifier
                .Protected()
                .Setup("SendMessage", ItExpr.IsAny<SmtpClient>(), ItExpr.Is<MailMessage>(value => value == message))
                .Callback<SmtpClient, MailMessage>( (clientParam, messageParam) => client = clientParam);

            var result = await notifier.Object.NotifyOfOrderFailureAsync(partner, order, correlation);
            
            result.Should().NotBeNull("becuase a result should have been returned");
            result.Outcome.Should().Be(Outcome.Success, "because the notification should be successful");

            client.Should().NotBeNull("because the message should have been sent using the client");
            client.Host.Should().Be(config.SmtpHostAddress, "because the host from configuration should be used");
            client.Port.Should().Be(config.SmtpPort, "because the port from configuration should be used");
            client.Timeout.Should().Be(config.SmtpTimeoutMilliseconds, "because the timeout from configuration should be used");
                        
            var credentials = client.Credentials as NetworkCredential;
            
            credentials.Should().NotBeNull("because the credentials should be set");
            credentials.UserName.Should().Be(config.SmtpUserName, "because the user from configuration should have been used");
            credentials.Password.Should().Be(config.SmtpPasword, "because the password from configuration should have been used");           
        }    

        /// <summary>
        ///  Verifies functionality of the <see cref="UnderTest.EmailNotifier.NotifyDeadLetterMessageAsync" />
        ///  method.
        /// </summary>
        /// 
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void NotifyDeadLetterMessageAsyncValidatesTheLocation(string location)
        {
            var notifier =  new UnderTest.EmailNotifier(new EmailNotifierConfiguration(), Mock.Of<ILogger>());
            
            Action actionUnderTest = () => notifier.NotifyDeadLetterMessageAsync(location, "123", "ABC", "blue").GetAwaiter().GetResult();
            actionUnderTest.ShouldThrow<ArgumentException>("because the location is required").And.ParamName.Should().Be(nameof(location), "because the location was invalid");
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="UnderTest.EmailNotifier.NotifyDeadLetterMessageAsync" />
        ///  method.
        /// </summary>
        /// 
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void NotifyDeadLetterMessageAsyncValidatesThePartner(string partner)
        {
            var notifier =  new UnderTest.EmailNotifier(new EmailNotifierConfiguration(), Mock.Of<ILogger>());
            
            Action actionUnderTest = () => notifier.NotifyDeadLetterMessageAsync("ProcessOrder", partner, "ABC", "blue").GetAwaiter().GetResult();
            actionUnderTest.ShouldThrow<ArgumentException>("because the partner is required").And.ParamName.Should().Be(nameof(partner), "because the partner was invalid");
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="UnderTest.EmailNotifier.NotifyDeadLetterMessageAsync" />
        ///  method.
        /// </summary>
        /// 
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void NotifyDeadLetterMessageAsyncValidatesTheOrder(string orderId)
        {
            var notifier =  new UnderTest.EmailNotifier(new EmailNotifierConfiguration(), Mock.Of<ILogger>());
            
            Action actionUnderTest = () => notifier.NotifyDeadLetterMessageAsync("SubmitOrder", "Bob", orderId, "blue").GetAwaiter().GetResult();
            actionUnderTest.ShouldThrow<ArgumentException>("because the orderId is required").And.ParamName.Should().Be(nameof(orderId), "because the orderId was invalid");
        }
        
        /// <summary>
        ///  Verifies functionality of the <see cref="UnderTest.EmailNotifier.NotifyDeadLetterMessageAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task NotifyDeadLetterMessageAsyncReturnsTheExceptionResultIfCreatingTheMessageThrows()
        {
            var config     = new EmailNotifierConfiguration { Enabled = true };
            var mockLogger = new Mock<ILogger>();
            var notifier   = new Mock<UnderTest.EmailNotifier>(config, mockLogger.Object) { CallBase = true };

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            notifier
                .Protected()
                .Setup<MailMessage>("CreateMessage", ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<IEnumerable<string>>())
                .Throws(new MissingDependencyException());

            var result = await notifier.Object.NotifyDeadLetterMessageAsync("Submit", "ABC", "123");
            
            result.Should().NotBeNull("becuase a result should have been returned");
            result.ShouldBeEquivalentTo(OperationResult.ExceptionResult, "because the exception should short-circuit the processing");

        }

        /// <summary>
        ///  Verifies functionality of the <see cref="UnderTest.EmailNotifier.NotifyDeadLetterMessageAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task NotifyDeadLetterMessageAsyncReturnsTheExceptionResultIfCreatingTheClientThrows()
        {
            var config     = new EmailNotifierConfiguration { Enabled = true };
            var mockLogger = new Mock<ILogger>();
            var notifier   = new Mock<UnderTest.EmailNotifier>(config, mockLogger.Object) { CallBase = true };

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            notifier
                .Protected()
                .Setup<MailMessage>("CreateMessage", ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<IEnumerable<string>>())
                .Returns(new MailMessage());

            notifier
                .Protected()
                .Setup<SmtpClient>("CreateSmtpClient", ItExpr.IsAny<EmailNotifierConfiguration>())
                .Throws(new MissingDependencyException());

            var result = await notifier.Object.NotifyDeadLetterMessageAsync("Process-Order", "ABC", "123");
            
            result.Should().NotBeNull("becuase a result should have been returned");
            result.ShouldBeEquivalentTo(OperationResult.ExceptionResult, "because the exception should short-circuit the processing");

        }

        /// <summary>
        ///  Verifies functionality of the <see cref="UnderTest.EmailNotifier.NotifyDeadLetterMessageAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task NotifyDeadLetterMessageAsyncReturnsTheExceptionResultIfSendingTheMessageThrows()
        {
            var config     = new EmailNotifierConfiguration { Enabled = true };
            var mockLogger = new Mock<ILogger>();
            var notifier   = new Mock<UnderTest.EmailNotifier>(config, mockLogger.Object) { CallBase = true };

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            notifier
                .Protected()
                .Setup<MailMessage>("CreateMessage", ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<IEnumerable<string>>())
                .Returns(new MailMessage());

            notifier
                .Protected()
                .Setup<SmtpClient>("CreateSmtpClient", ItExpr.IsAny<EmailNotifierConfiguration>())
                .Returns(new SmtpClient());

            notifier
                .Protected()
                .Setup("SendMessage", ItExpr.IsAny<SmtpClient>(), ItExpr.IsAny<MailMessage>())
                .Throws(new NullReferenceException());

            var result = await notifier.Object.NotifyDeadLetterMessageAsync("Process", "ABC", "123");
            
            result.Should().NotBeNull("becuase a result should have been returned");
            result.ShouldBeEquivalentTo(OperationResult.ExceptionResult, "because the exception should short-circuit the processing");

        }

        /// <summary>
        ///  Verifies functionality of the <see cref="UnderTest.EmailNotifier.NotifyDeadLetterMessageAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task NotifyDeadLetterMessageAsyncHonorsTheRetryPolicyIfSendingTheMessageThrows()
        {
            var config     = new EmailNotifierConfiguration { Enabled = true, OperationRetryMaxCount = 4, OperationRetryExponentialSeconds = 0, OperationRetryJitterSeconds = 0 };
            var mockLogger = new Mock<ILogger>();
            var notifier   = new Mock<UnderTest.EmailNotifier>(config, mockLogger.Object) { CallBase = true };
            var retries    = 0;

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            notifier
                .Protected()
                .Setup<MailMessage>("CreateMessage", ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<IEnumerable<string>>())
                .Returns(new MailMessage());

            notifier
                .Protected()
                .Setup<SmtpClient>("CreateSmtpClient", ItExpr.IsAny<EmailNotifierConfiguration>())
                .Returns(new SmtpClient());

            notifier
                .Protected()
                .Setup("SendMessage", ItExpr.IsAny<SmtpClient>(), ItExpr.IsAny<MailMessage>())
                .Callback( () => ++retries)
                .Throws(new NullReferenceException());

            var result = await notifier.Object.NotifyDeadLetterMessageAsync("Process", "ABC", "123");
            
            retries.Should().Be(config.OperationRetryMaxCount + 1, "because a failure should trigger the retry policy");

        }

        /// <summary>
        ///  Verifies functionality of the <see cref="UnderTest.EmailNotifier.NotifyDeadLetterMessageAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task NotifyDeadLetterMessageAsyncRespectsTheEnabledFlag()
        {
            var config     = new EmailNotifierConfiguration { Enabled = false };
            var mockLogger = new Mock<ILogger>();
            var notifier   = new Mock<UnderTest.EmailNotifier>(config, mockLogger.Object) { CallBase = true };

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);
                
            var result = await notifier.Object.NotifyDeadLetterMessageAsync("Some-Queue", "ABC", "123");

            notifier
                .Protected()
                .Verify("CreateMessage", Times.Never(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<IEnumerable<string>>());

            notifier
                .Protected()
                .Verify("CreateSmtpClient", Times.Never(), ItExpr.IsAny<EmailNotifierConfiguration>());

            notifier
                .Protected()
                .Verify("SendMessage", Times.Never(), ItExpr.IsAny<SmtpClient>(), ItExpr.IsAny<MailMessage>());
            
            result.Should().NotBeNull("becuase a result should have been returned");
            result.Outcome.Should().Be(Outcome.Success, "because a skipped notification should be successful");
        }

        /// <summary>
        ///  Verifies functionality of the <see cref="UnderTest.EmailNotifier.NotifyDeadLetterMessageAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task NotifyDeadLetterMessageAsyncSendsTheEmailNotification()
        {
            var config     = new EmailNotifierConfiguration { Enabled = true };
            var mockLogger = new Mock<ILogger>();
            var notifier   = new Mock<UnderTest.EmailNotifier>(config, mockLogger.Object) { CallBase = true };
            var message    = new MailMessage();
            var client     = new SmtpClient();

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            notifier
                .Protected()
                .Setup<MailMessage>("CreateMessage", ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<IEnumerable<string>>())
                .Returns(message);

            notifier
                .Protected()
                .Setup<SmtpClient>("CreateSmtpClient", ItExpr.IsAny<EmailNotifierConfiguration>())
                .Returns(client);

            notifier
                .Protected()
                .Setup("SendMessage", ItExpr.Is<SmtpClient>(value => value == client), ItExpr.Is<MailMessage>(value => value == message))
                .Verifiable("The message should have been sent");

            var result = await notifier.Object.NotifyDeadLetterMessageAsync("Some-Queue", "ABC", "123");
            
            result.Should().NotBeNull("becuase a result should have been returned");
            result.Outcome.Should().Be(Outcome.Success, "because the notification should be successful");

            notifier.VerifyAll();
        }    
        
        /// <summary>
        ///  Verifies functionality of the <see cref="UnderTest.EmailNotifier.NotifyDeadLetterMessageAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task NotifyDeadLetterMessageAsyncBuildsTheCorrectMessage()
        {
            var config = new EmailNotifierConfiguration 
            { 
                Enabled                       = true,
                DeadLetterNotificationBody   = "{location}/{partner}/{orderId}/{correlationId}",
                DeadLetterNotificationSubject = "omg! subject",
                FromEmailAddress              = "someone@somewhere.com",
                ToEmailAddressList            = "one@here.com,two@here.com"                
            };

            var mockLogger  = new Mock<ILogger>();
            var notifier    = new Mock<UnderTest.EmailNotifier>(config, mockLogger.Object) { CallBase = true };
            var client      = new SmtpClient();
            var message     = default(MailMessage);
            var location    = "process-order";
            var partner     = "ABC";
            var order       = "345";
            var correlation = "DEF456";

            var expectedBody = config.DeadLetterNotificationBody
                .Replace("{location}", location)
                .Replace("{partner}", partner)
                .Replace("{orderId}", order)
                .Replace("{correlationId}", correlation);            

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            notifier
                .Protected()
                .Setup<SmtpClient>("CreateSmtpClient", ItExpr.IsAny<EmailNotifierConfiguration>())
                .Returns(client);

            notifier
                .Protected()
                .Setup("SendMessage", ItExpr.Is<SmtpClient>(value => value == client), ItExpr.IsAny<MailMessage>())
                .Callback<SmtpClient, MailMessage>( (clientParam, messageParam) => message = messageParam);

            var result = await notifier.Object.NotifyDeadLetterMessageAsync(location, partner, order, correlation);
            
            result.Should().NotBeNull("becuase a result should have been returned");
            result.Outcome.Should().Be(Outcome.Success, "because the notification should be successful");

            message.Should().NotBeNull("because the message should have been sent");
            message.Subject.Should().Be(config.DeadLetterNotificationSubject, "because the configuration subject should be used");
            message.Body.Should().Be(expectedBody, "because the body should have been populated from the configuration and parameers");
            message.From.Should().Be(config.FromEmailAddress);

            foreach (var recipient in config.ToEmailAddressList.Split(','))
            {
                message.To.Any(to => to.Address == recipient).Should().BeTrue("because the recipient {0} should be on the message", recipient);
            }
        }    

        /// <summary>
        ///  Verifies functionality of the <see cref="UnderTest.EmailNotifier.NotifyDeadLetterMessageAsync" />
        ///  method.
        /// </summary>
        /// 
        [Fact]
        public async Task NotifyDeadLetterMessageAsyncBuildsTheCorrectClient()
        {
            var config = new EmailNotifierConfiguration 
            { 
                Enabled                 = true,
                SmtpHostAddress         = "server.thing.com",
                SmtpPort                = 1234,
                SmtpUserName            = "birb",
                SmtpPasword             = "fly",
                SmtpTimeoutMilliseconds = 4
            };

            var mockLogger  = new Mock<ILogger>();
            var notifier    = new Mock<UnderTest.EmailNotifier>(config, mockLogger.Object) { CallBase = true };
            var client      = default(SmtpClient);
            var message     = new MailMessage();
            var location    = "process-order";
            var partner     = "ABC";
            var order       = "345";
            var correlation = "DEF456";      

            mockLogger
                .Setup(logger => logger.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(mockLogger.Object);

            notifier
                .Protected()
                .Setup<MailMessage>("CreateMessage", ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<IEnumerable<string>>())
                .Returns(message);

            notifier
                .Protected()
                .Setup("SendMessage", ItExpr.IsAny<SmtpClient>(), ItExpr.Is<MailMessage>(value => value == message))
                .Callback<SmtpClient, MailMessage>( (clientParam, messageParam) => client = clientParam);

            var result = await notifier.Object.NotifyDeadLetterMessageAsync(location, partner, order, correlation);
            
            result.Should().NotBeNull("becuase a result should have been returned");
            result.Outcome.Should().Be(Outcome.Success, "because the notification should be successful");

            client.Should().NotBeNull("because the message should have been sent using the client");
            client.Host.Should().Be(config.SmtpHostAddress, "because the host from configuration should be used");
            client.Port.Should().Be(config.SmtpPort, "because the port from configuration should be used");
            client.Timeout.Should().Be(config.SmtpTimeoutMilliseconds, "because the timeout from configuration should be used");
                        
            var credentials = client.Credentials as NetworkCredential;
            
            credentials.Should().NotBeNull("because the credentials should be set");
            credentials.UserName.Should().Be(config.SmtpUserName, "because the user from configuration should have been used");
            credentials.Password.Should().Be(config.SmtpPasword, "because the password from configuration should have been used");           
        }    
    }
}
