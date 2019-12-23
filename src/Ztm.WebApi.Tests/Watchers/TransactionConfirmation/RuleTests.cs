using System;
using System.Net;
using NBitcoin;
using Xunit;
using Ztm.WebApi.Callbacks;
using Ztm.WebApi.Watchers.TransactionConfirmation;

namespace Ztm.WebApi.Tests.Watchers.TransactionConfirmation
{
    public class RuleTests
    {
        [Fact]
        public void Construct_WithNullArgs_ShouldThrow()
        {
            var id = Guid.NewGuid();
            var tx = uint256.One;
            var confirmation = 1;
            var waitingTime = TimeSpan.FromDays(1);
            var successResponse = new CallbackResult("success", "");
            var timeoutResponse = new CallbackResult("timeout", "");
            var callback = new Callback(Guid.Empty, IPAddress.Loopback, DateTime.Now, false, new Uri("https://zcoin.io"));

            Assert.Throws<ArgumentNullException>(
                "transactionHash",
                () => new Rule(id, null, confirmation, waitingTime, successResponse, timeoutResponse, callback, DateTime.UtcNow)
            );

            Assert.Throws<ArgumentNullException>(
                "successResponse",
                () => new Rule(id, tx, confirmation, waitingTime, null, timeoutResponse, callback, DateTime.UtcNow)
            );

            Assert.Throws<ArgumentNullException>(
                "timeoutResponse",
                () => new Rule(id, tx, confirmation, waitingTime, successResponse, null, callback, DateTime.UtcNow)
            );

            Assert.Throws<ArgumentNullException>(
                "callback",
                () => new Rule(id, tx, confirmation, waitingTime, successResponse, timeoutResponse, null, DateTime.UtcNow)
            );
        }

        [Theory]
        [InlineData(int.MinValue)]
        [InlineData(-1)]
        [InlineData(0)]
        public void Construct_WithInvalidConfirmationNumber_ShouldThrow(int confirmation)
        {
            var id = Guid.NewGuid();
            var tx = uint256.One;
            var waitingTime = TimeSpan.FromDays(1);
            var successResponse = new CallbackResult("success", "");
            var timeoutResponse = new CallbackResult("timeout", "");
            var callback = new Callback(Guid.Empty, IPAddress.Loopback, DateTime.Now, false, new Uri("https://zcoin.io"));

            Assert.Throws<ArgumentException>(
                "confirmations",
                () => new Rule(id, tx, confirmation, waitingTime, successResponse, timeoutResponse,callback, DateTime.UtcNow)
            );
        }

        [Fact]
        public void Construct_WithWaitingTimeLessThanZero_ShouldThrow()
        {
            var id = Guid.NewGuid();
            var tx = uint256.One;
            var confirmation = 1;
            var successResponse = new CallbackResult("success", "");
            var timeoutResponse = new CallbackResult("timeout", "");
            var callback = new Callback(Guid.Empty, IPAddress.Loopback, DateTime.Now, false, new Uri("https://zcoin.io"));

            Assert.Throws<ArgumentException>(
                "originalWaitingTime",
                () => new Rule(id, tx, confirmation, TimeSpan.MinValue, successResponse, timeoutResponse, callback, DateTime.UtcNow)
            );

            Assert.Throws<ArgumentException>(
                "originalWaitingTime",
                () => new Rule(id, tx, confirmation, TimeSpan.FromTicks(-1), successResponse, timeoutResponse, callback, DateTime.UtcNow)
            );

            Assert.Throws<ArgumentException>(
                "originalWaitingTime",
                () => new Rule(id, tx, confirmation, TimeSpan.FromSeconds(-1), successResponse, timeoutResponse, callback, DateTime.UtcNow)
            );

            Assert.Throws<ArgumentException>(
                "originalWaitingTime",
                () => new Rule(id, tx, confirmation, TimeSpan.FromMinutes(-1), successResponse, timeoutResponse, callback, DateTime.UtcNow)
            );

            Assert.Throws<ArgumentException>(
                "originalWaitingTime",
                () => new Rule(id, tx, confirmation, TimeSpan.FromMilliseconds(-1), successResponse, timeoutResponse, callback, DateTime.UtcNow)
            );

            Assert.Throws<ArgumentException>(
                "originalWaitingTime",
                () => new Rule(id, tx, confirmation, TimeSpan.FromHours(-1), successResponse, timeoutResponse, callback, DateTime.UtcNow)
            );

            Assert.Throws<ArgumentException>(
                "originalWaitingTime",
                () => new Rule(id, tx, confirmation, TimeSpan.FromDays(-1), successResponse, timeoutResponse, callback, DateTime.UtcNow)
            );
        }

        [Fact]
        public void Construct_WithValidArgs_ShouldSuccess()
        {
            // Arrange.
            var id = Guid.NewGuid();
            var tx = uint256.One;
            var confirmations = 10;
            var waitingTime = TimeSpan.FromDays(1);
            var successResponse = new CallbackResult("success", "");
            var timeoutResponse = new CallbackResult("timeout", "");
            var callback = new Callback(Guid.Empty, IPAddress.Loopback, DateTime.Now, false, new Uri("https://zcoin.io"));
            var time = DateTime.UtcNow;

            // Act.
            var rule = new Rule(id, tx, confirmations, waitingTime, successResponse, timeoutResponse, callback, time);

            // Assert.
            Assert.Equal(id, rule.Id);
            Assert.Equal(tx, rule.TransactionHash);
            Assert.Equal(confirmations, rule.Confirmations);
            Assert.Equal(waitingTime, rule.OriginalWaitingTime);
            Assert.Equal(successResponse, rule.SuccessResponse);
            Assert.Equal(timeoutResponse, rule.TimeoutResponse);
            Assert.Equal(callback, rule.Callback);
            Assert.Equal(time, rule.CreatedAt);
        }
    }
}