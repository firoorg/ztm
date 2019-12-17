using System;
using System.Net;
using NBitcoin;
using Xunit;
using Ztm.WebApi.Callbacks;
using Ztm.WebApi.Tests.Watchers.TransactionConfirmation;
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
                "transaction",
                () => new Rule(id, null, confirmation, waitingTime, successResponse, timeoutResponse, callback)
            );

            Assert.Throws<ArgumentNullException>(
                "successResponse",
                () => new Rule(id, tx, confirmation, waitingTime, null, timeoutResponse, callback)
            );

            Assert.Throws<ArgumentNullException>(
                "timeoutResponse",
                () => new Rule(id, tx, confirmation, waitingTime, successResponse, null, callback)
            );

            Assert.Throws<ArgumentNullException>(
                "callback",
                () => new Rule(id, tx, confirmation, waitingTime, successResponse, timeoutResponse, null)
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
                () => new Rule(id, tx, confirmation, waitingTime, successResponse, timeoutResponse,callback)
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
                () => new Rule(id, tx, confirmation, TimeSpan.MinValue, successResponse, timeoutResponse,callback)
            );

            Assert.Throws<ArgumentException>(
                "originalWaitingTime",
                () => new Rule(id, tx, confirmation, TimeSpan.FromTicks(-1), successResponse, timeoutResponse,callback)
            );

            Assert.Throws<ArgumentException>(
                "originalWaitingTime",
                () => new Rule(id, tx, confirmation, TimeSpan.FromSeconds(-1), successResponse, timeoutResponse,callback)
            );

            Assert.Throws<ArgumentException>(
                "originalWaitingTime",
                () => new Rule(id, tx, confirmation, TimeSpan.FromMinutes(-1), successResponse, timeoutResponse,callback)
            );

            Assert.Throws<ArgumentException>(
                "originalWaitingTime",
                () => new Rule(id, tx, confirmation, TimeSpan.FromMilliseconds(-1), successResponse, timeoutResponse,callback)
            );

            Assert.Throws<ArgumentException>(
                "originalWaitingTime",
                () => new Rule(id, tx, confirmation, TimeSpan.FromHours(-1), successResponse, timeoutResponse,callback)
            );

            Assert.Throws<ArgumentException>(
                "originalWaitingTime",
                () => new Rule(id, tx, confirmation, TimeSpan.FromDays(-1), successResponse, timeoutResponse,callback)
            );
        }
    }
}