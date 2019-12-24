using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NBitcoin;
using Xunit;
using Ztm.Configuration;
using Ztm.Testing;
using Ztm.WebApi.Callbacks;
using Ztm.WebApi.Models;
using Ztm.WebApi.Watchers.TransactionConfirmation;
using Ztm.Zcoin.NBitcoin;
using Ztm.Zcoin.NBitcoin.Exodus;
using Ztm.Zcoin.Rpc;
using Transaction = Ztm.WebApi.Models.Transaction;

namespace Ztm.WebApi.Controllers
{
    public sealed class TokenControllerTests
    {
        readonly Mock<IZcoinRpcClientFactory> factory;
        readonly Mock<IZcoinRpcClient> client;
        readonly Mock<ITransactionConfirmationWatcher> watcher;
        readonly Mock<ICallbackRepository> callbackRepository;
        readonly Mock<IRuleRepository> ruleRepository;
        readonly ZcoinConfiguration configuration;

        readonly TokenController subject;

        public TokenControllerTests()
        {
            this.factory = new Mock<IZcoinRpcClientFactory>();
            this.client = new Mock<IZcoinRpcClient>();
            this.watcher = new Mock<ITransactionConfirmationWatcher>();
            this.callbackRepository = new Mock<ICallbackRepository>();
            this.ruleRepository = new Mock<IRuleRepository>();

            this.factory.Setup(f => f.CreateRpcClientAsync(It.IsAny<CancellationToken>()))
                        .ReturnsAsync(this.client.Object);

            this.configuration = new ZcoinConfiguration
            {
                Property = new ZcoinPropertyConfiguration
                {
                    Distributor = new BitcoinAddressConfiguration
                    {
                        Address = TestAddress.Mainnet1,
                        Type = NetworkType.Mainnet
                    },
                    Id = new PropertyId(1),
                    Issuer = new BitcoinAddressConfiguration
                    {
                        Address = TestAddress.Mainnet1,
                        Type = NetworkType.Mainnet
                    }
                }
            };

            this.subject = new TokenController
            (
                this.factory.Object,
                this.configuration,
                this.watcher.Object,
                this.callbackRepository.Object,
                this.ruleRepository.Object
            );
        }

        [Fact]
        public void Construct_WithNullArguments_ShouldThrow()
        {
            var configuration = new ZcoinConfiguration();

            Assert.Throws<ArgumentNullException>(
                "factory",
                () => new TokenController(null, configuration, this.watcher.Object, this.callbackRepository.Object, this.ruleRepository.Object)
            );

            Assert.Throws<ArgumentNullException>(
                "configuration",
                () => new TokenController(this.factory.Object, null, this.watcher.Object, this.callbackRepository.Object, this.ruleRepository.Object)
            );

            Assert.Throws<ArgumentNullException>(
                "watcher",
                () => new TokenController(this.factory.Object, configuration, null, this.callbackRepository.Object, this.ruleRepository.Object)
            );

            Assert.Throws<ArgumentNullException>(
                "callbackRepository",
                () => new TokenController(this.factory.Object, configuration, this.watcher.Object, null, this.ruleRepository.Object)
            );

            Assert.Throws<ArgumentNullException>(
                "ruleRepository",
                () => new TokenController(this.factory.Object, configuration, this.watcher.Object, this.callbackRepository.Object, null)
            );
        }

        [Fact]
        public async Task Issue_WithValidArgumentAndUrlWasNotSet_ShouldSuccessButNotAddCallback()
        {
            // Arrange.
            var destination = TestAddress.Mainnet2;
            var amount = PropertyAmount.One;
            var note = "Test Issuing";
            var tx = NBitcoin.Transaction.Parse(TestTransaction.Raw1, ZcoinNetworks.Instance.Mainnet);
            var fee = Money.Satoshis(500);

            this.client.Setup
            (
                c => c.GrantPropertyAsync
                (
                    It.Is<Property>(p => p.Id == this.configuration.Property.Id && p.Type == this.configuration.Property.Type),
                    this.configuration.Property.Distributor.Address,
                    destination,
                    It.Is<PropertyAmount>(a => a.Equals(amount)),
                    note,
                    It.IsAny<CancellationToken>()
                )
            ).ReturnsAsync(tx).Verifiable();

            this.client.Setup
            (
                c => c.SendRawTransactionAsync
                (
                    tx,
                    It.IsAny<CancellationToken>()
                )
            ).ReturnsAsync(tx.GetHash()).Verifiable();

            this.client.Setup
            (
                c => c.GetExodusTransactionAsync(tx.GetHash(), It.IsAny<CancellationToken>())
            ).ReturnsAsync(new ExodusTransactionInformation{Fee = fee}).Verifiable();

            var payload = new Issuing
            {
                Amount = amount,
                Destination = destination,
                Note = note,
            };

            var httpContext = new DefaultHttpContext();
            this.subject.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Act.
            var result = await this.subject.Issue(payload);

            // Assert.
            var okObjectResult = Assert.IsType<OkObjectResult>(result);
            var returnedTx = Assert.IsType<Transaction>(okObjectResult.Value);

            Assert.Equal(tx.GetHash(), returnedTx.Tx);
            Assert.Equal(fee, returnedTx.Fee);

            this.client.Verify();

            this.watcher.Verify(
                w => w.AddTransactionAsync
                (
                    It.IsAny<uint256>(),
                    It.IsAny<int>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<Callback>(),
                    It.IsAny<CallbackResult>(),
                    It.IsAny<CallbackResult>(),
                    It.IsAny<CancellationToken>()
                ),
                Times.Never()
            );

            this.callbackRepository.Verify(
                r => r.AddAsync
                (
                    It.IsAny<IPAddress>(),
                    It.IsAny<Uri>(),
                    It.IsAny<CancellationToken>()
                ),
                Times.Never()
            );
        }

        [Fact]
        public async Task Issue_WithValidArgumentAndCallbackUrlIsSet_ShouldSuccess()
        {
            // Arrange.
            var destination = TestAddress.Mainnet2;
            var amount = PropertyAmount.One;
            var note = "Test Issuing";
            var tx = NBitcoin.Transaction.Parse(TestTransaction.Raw1, ZcoinNetworks.Instance.Mainnet);
            var fee = Money.Satoshis(500);
            var rawCallbackUrl = "https://zcoin.io/callback";
            var callbackUrl = new Uri(rawCallbackUrl);

            // Setup Rpc client
            this.client.Setup
            (
                c => c.GrantPropertyAsync
                (
                    It.Is<Property>(p => p.Id == this.configuration.Property.Id && p.Type == this.configuration.Property.Type),
                    this.configuration.Property.Distributor.Address,
                    destination,
                    It.Is<PropertyAmount>(a => a.Equals(amount)),
                    note,
                    It.IsAny<CancellationToken>()
                )
            ).ReturnsAsync(tx).Verifiable();

            this.client.Setup
            (
                c => c.SendRawTransactionAsync
                (
                    tx,
                    It.IsAny<CancellationToken>()
                )
            ).ReturnsAsync(tx.GetHash()).Verifiable();

            this.client.Setup
            (
                c => c.GetExodusTransactionAsync(tx.GetHash(), It.IsAny<CancellationToken>())
            ).ReturnsAsync(new ExodusTransactionInformation{Fee = fee}).Verifiable();

            // Construct payload
            var payload = new Issuing
            {
                Amount = amount,
                Destination = destination,
                Note = note,
            };

            // Mock and set url to request's header
            var httpContext = new DefaultHttpContext();
            this.subject.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
            httpContext.Connection.RemoteIpAddress = IPAddress.Loopback;
            httpContext.Request.Headers.TryAdd("X-Callback-URL", rawCallbackUrl);

            // Add callback and register tx to watcher
            var callback = new Callback(Guid.NewGuid(), IPAddress.Loopback, DateTime.UtcNow, false, callbackUrl);

            this.callbackRepository.Setup
            (
                r => r.AddAsync(IPAddress.Loopback, callbackUrl, It.IsAny<CancellationToken>())
            ).ReturnsAsync(callback).Verifiable();

            this.watcher.Setup(
                r => r.AddTransactionAsync
                (
                    tx.GetHash(),
                    It.Is<int>(c => c >= 1),
                    It.Is<TimeSpan>(wt => wt < TimeSpan.Zero),
                    callback,
                    It.Is<CallbackResult>(rs => rs.Status == "success"),
                    It.Is<CallbackResult>(rs => rs.Status == "tokens-issuing-timeout"),
                    It.IsAny<CancellationToken>()
                )
            ).ReturnsAsync(
                (uint256 _tx, int _confirmations, TimeSpan _waiting, Callback _callback, CallbackResult _success, CallbackResult _timeout, CancellationToken _) =>
                    new Rule
                    (
                        Guid.NewGuid(),
                        _tx,
                        _confirmations,
                        _waiting,
                        _success,
                        _timeout,
                        _callback,
                        DateTime.UtcNow
                    )
            );

            // Act.
            var result = await this.subject.Issue(payload);

            // Assert.
            var okObjectResult = Assert.IsType<OkObjectResult>(result);
            var returnedTx = Assert.IsType<Transaction>(okObjectResult.Value);

            Assert.Equal(tx.GetHash(), returnedTx.Tx);
            Assert.Equal(fee, returnedTx.Fee);

            this.client.Verify();
            this.callbackRepository.Verify();
            this.watcher.Verify();

            Assert.Equal(callback.Id.ToString(), httpContext.Response.Headers.TryGet("X-Callback-ID"));
        }
    }
}