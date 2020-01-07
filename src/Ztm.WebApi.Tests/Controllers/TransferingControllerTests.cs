using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using NBitcoin;
using Xunit;
using Ztm.Configuration;
using Ztm.Testing;
using Ztm.WebApi.Callbacks;
using Ztm.WebApi.Controllers;
using Ztm.WebApi.Models;
using Ztm.WebApi.Watchers.TransactionConfirmation;
using Ztm.Zcoin.NBitcoin;
using Ztm.Zcoin.NBitcoin.Exodus;
using Ztm.Zcoin.Rpc;

namespace Ztm.WebApi.Tests.Controllers
{
    public sealed class TransferingControllerTests
    {
        readonly Mock<IRpcFactory> factory;
        readonly Mock<IPropertyManagementRpc> propertyManagementRpc;
        readonly Mock<IRawTransactionRpc> rawTransactionRpc;

        readonly Mock<ITransactionConfirmationWatcher> watcher;
        readonly Mock<ICallbackRepository> callbackRepository;
        readonly Mock<IRuleRepository> ruleRepository;
        readonly IConfiguration configuration;

        readonly TransferingController subject;

        readonly ZcoinConfiguration zcoinConfig;
        readonly ApiConfiguration apiConfig;

        public TransferingControllerTests()
        {
            this.factory = new Mock<IRpcFactory>();
            this.propertyManagementRpc = new Mock<IPropertyManagementRpc>();
            this.rawTransactionRpc = new Mock<IRawTransactionRpc>();

            var anyCancellationToken = It.IsAny<CancellationToken>();
            this.factory.Setup(f => f.CreatePropertyManagementRpcAsync(anyCancellationToken))
                .ReturnsAsync(this.propertyManagementRpc.Object);

            this.factory.Setup(f => f.CreateRawTransactionRpcAsync(anyCancellationToken))
                .ReturnsAsync(this.rawTransactionRpc.Object);

            this.watcher = new Mock<ITransactionConfirmationWatcher>();
            this.callbackRepository = new Mock<ICallbackRepository>();
            this.ruleRepository = new Mock<IRuleRepository>();

            var builder = new ConfigurationBuilder();

            builder.AddInMemoryCollection(new System.Collections.Generic.Dictionary<string, string>
            {
                {"Api:Default:TransactionTimeout", "00:10"},
                {"Api:Default:RequiredConfirmation", "6"},
                {"Zcoin:Network:Type", "Mainnet"},
                {"Zcoin:Property:Id", "3"},
                {"Zcoin:Property:Type", "Divisible"},
                {"Zcoin:Property:Issuer", "Mainnet:a8ULhhDgfdSiXJhSZVdhb8EuDc6R3ogsaM"},
                {"Zcoin:Property:Distributor", "Mainnet:a8ULhhDgfdSiXJhSZVdhb8EuDc6R3ogsaM"},
            });

            this.configuration = builder.Build();
            this.zcoinConfig = this.configuration.GetZcoinSection();
            this.apiConfig = this.configuration.GetApiSection();

            this.subject = new TransferingController
            (
                this.factory.Object,
                this.watcher.Object,
                this.callbackRepository.Object,
                this.ruleRepository.Object,
                this.configuration
            );
        }

        [Fact]
        public void Construct_WithNullArgument_ShouldThrow()
        {
            Action act;

            act = () => new TransferingController(null, this.watcher.Object, this.callbackRepository.Object, this.ruleRepository.Object, this.configuration);
            act.Should().Throw<ArgumentNullException>()
               .And.ParamName.Should().Be("factory");

            act = () => new TransferingController(this.factory.Object, null, this.callbackRepository.Object, this.ruleRepository.Object, this.configuration);
            act.Should().Throw<ArgumentNullException>()
               .And.ParamName.Should().Be("watcher");

            act = () => new TransferingController(this.factory.Object, this.watcher.Object, null, this.ruleRepository.Object, this.configuration);
            act.Should().Throw<ArgumentNullException>()
               .And.ParamName.Should().Be("callbackRepository");

            act = () => new TransferingController(this.factory.Object, this.watcher.Object, this.callbackRepository.Object, null, this.configuration);
            act.Should().Throw<ArgumentNullException>()
               .And.ParamName.Should().Be("ruleRepository");

            act = () => new TransferingController(this.factory.Object, this.watcher.Object, this.callbackRepository.Object, this.ruleRepository.Object, null);
            act.Should().Throw<ArgumentNullException>()
               .And.ParamName.Should().Be("configuration");
        }

        [Fact]
        public async Task PostAsync_WithoutCallback_ShouldSuccess()
        {
            // Arrange.
            var amount = new PropertyAmount(10);
            var destination = TestAddress.Mainnet1;
            var req = new TransferingRequest
            {
                Amount = amount,
                Destination = destination,
                ReferenceAmount = Money.Satoshis(100)
            };

            var network = ZcoinNetworks.Instance.GetNetwork(this.zcoinConfig.Network.Type);
            var tx = Transaction.Create(network);

            var property = new Property(this.zcoinConfig.Property.Id, this.zcoinConfig.Property.Type);

            var cancellationToken = new CancellationToken(false);

            // Build Mock.
            this.propertyManagementRpc.Setup(
                r => r.SendAsync
                (
                    this.zcoinConfig.Property.Distributor.Address,
                    destination,
                    property,
                    amount,
                    Money.Satoshis(100),
                    cancellationToken
                )).ReturnsAsync(tx).Verifiable();

            this.rawTransactionRpc.Setup(
                r => r.SendAsync
                (
                    tx,
                    cancellationToken
                )).ReturnsAsync(tx.GetHash()).Verifiable();

            // Mock context.
            var httpContext = new DefaultHttpContext();
            this.subject.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Act.
            var result = await this.subject.PostAsync(req, cancellationToken);

            // Assert.
            result.Should().NotBeNull();
            result.Should().BeOfType<OkObjectResult>()
                  .Which.Value.Should().BeEquivalentTo(new {Tx = tx.GetHash()});

            this.propertyManagementRpc.Verify();
            this.rawTransactionRpc.Verify();

            // Callback should not be created.
            this.callbackRepository.Verify
            (
                r => r.AddAsync
                (
                    It.IsAny<IPAddress>(),
                    It.IsAny<Uri>(),
                    It.IsAny<CancellationToken>()
                ),
                Times.Never()
            );

            // The transaction should not be watched.
            this.watcher.Verify
            (
                r => r.AddTransactionAsync
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
        }

        [Fact]
        public async Task PostAsync_WithCallback_ShouldSuccess()
        {
            // Arrange.
            var amount = new PropertyAmount(10);
            var destination = TestAddress.Mainnet1;
            var callerIP = IPAddress.Loopback;
            var rawCallbackUrl = "https://zcoin.io/callback";
            var callbackUrl = new Uri(rawCallbackUrl);
            var req = new TransferingRequest
            {
                Amount = amount,
                Destination = destination,
                ReferenceAmount = Money.Satoshis(100)
            };

            var network = ZcoinNetworks.Instance.GetNetwork(this.zcoinConfig.Network.Type);
            var tx = Transaction.Create(network);

            var property = new Property(this.zcoinConfig.Property.Id, this.zcoinConfig.Property.Type);

            var cancellationToken = new CancellationToken(false);

            // Build Mock.
            this.propertyManagementRpc.Setup(
                r => r.SendAsync
                (
                    this.zcoinConfig.Property.Distributor.Address,
                    destination,
                    property,
                    amount,
                    Money.Satoshis(100),
                    cancellationToken
                )).ReturnsAsync(tx).Verifiable();

            this.rawTransactionRpc.Setup(
                r => r.SendAsync
                (
                    tx,
                    cancellationToken
                )).ReturnsAsync(tx.GetHash()).Verifiable();

            var callback = new Callback(Guid.NewGuid(), callerIP, DateTime.UtcNow, false, callbackUrl);
            this.callbackRepository.Setup(
                r => r.AddAsync
                (
                    callerIP,
                    new Uri(rawCallbackUrl),
                    CancellationToken.None
                )).ReturnsAsync(callback).Verifiable();

            this.watcher.Setup
            (
                r => r.AddTransactionAsync
                (
                    tx.GetHash(),
                    this.apiConfig.Default.RequiredConfirmation,
                    this.apiConfig.Default.TransactionTimeout,
                    callback,
                    It.Is<CallbackResult>(c => c.Status == CallbackResult.StatusSuccess),
                    It.Is<CallbackResult>(c => c.Status == "tokens-transfer-timeout"),
                    CancellationToken.None
                )
            ).ReturnsAsync
            (
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
            ).Verifiable();

            // Mock context.
            var httpContext = new DefaultHttpContext();
            this.subject.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
            httpContext.Connection.RemoteIpAddress = IPAddress.Loopback;
            httpContext.Request.Headers.TryAdd("X-Callback-URL", rawCallbackUrl);

            // Act.
            var result = await this.subject.PostAsync(req, cancellationToken);

            // Assert.
            result.Should().NotBeNull();
            result.Should().BeOfType<OkObjectResult>()
                  .Which.Value.Should().BeEquivalentTo(new {Tx = tx.GetHash()});

            this.propertyManagementRpc.Verify();
            this.rawTransactionRpc.Verify();

            // Callback should be created.
            this.callbackRepository.Verify();

            // The transaction should be watched.
            this.ruleRepository.Verify();
        }
    }
}