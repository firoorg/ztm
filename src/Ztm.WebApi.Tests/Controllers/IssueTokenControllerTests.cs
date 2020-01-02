using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
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

namespace Ztm.WebApi.Controllers
{
    public sealed class IssueTokenControllerTests
    {
        readonly Mock<IRpcFactory> factory;
        readonly Mock<ITransactionConfirmationWatcher> watcher;
        readonly Mock<ICallbackRepository> callbackRepository;
        readonly Mock<IRuleRepository> ruleRepository;

        // RPCClients
        readonly Mock<IRawTransactionRpc> rawTransactionRpc;
        readonly Mock<IExodusInformationRpc> exodusInfomationRpc;
        readonly Mock<IPropertyManagementRpc> propertyManagementRpc;

        readonly IConfiguration configuration;
        readonly ZcoinConfiguration zcoinConfiguration;

        readonly IssueTokenController subject;

        public IssueTokenControllerTests()
        {
            this.factory = new Mock<IRpcFactory>();
            this.watcher = new Mock<ITransactionConfirmationWatcher>();
            this.callbackRepository = new Mock<ICallbackRepository>();
            this.ruleRepository = new Mock<IRuleRepository>();

            this.rawTransactionRpc = new Mock<IRawTransactionRpc>();
            this.exodusInfomationRpc = new Mock<IExodusInformationRpc>();
            this.propertyManagementRpc = new Mock<IPropertyManagementRpc>();

            var anyCancellationToken = It.IsAny<CancellationToken>();
            this.factory.Setup(f => f.CreateRawTransactionRpcAsync(anyCancellationToken)).ReturnsAsync(this.rawTransactionRpc.Object);
            this.factory.Setup(f => f.CreateExodusInformationRpcAsync(anyCancellationToken)).ReturnsAsync(this.exodusInfomationRpc.Object);
            this.factory.Setup(f => f.CreatePropertyManagementRpcAsync(anyCancellationToken)).ReturnsAsync(this.propertyManagementRpc.Object);

            var builder = new ConfigurationBuilder();

            builder.AddInMemoryCollection(new System.Collections.Generic.Dictionary<string, string>
            {
                {"Zcoin:Property:Id", "3"},
                {"Zcoin:Property:Type", "Divisible"},
                {"Zcoin:Property:Issuer", "Mainnet:a8ULhhDgfdSiXJhSZVdhb8EuDc6R3ogsaM"},
                {"Zcoin:Property:Distributor", "Mainnet:a8ULhhDgfdSiXJhSZVdhb8EuDc6R3ogsaM"},
                {"API:Global:TransactionTimeout", "00:10"},
                {"API:Global:RequiredConfirmation", "6"}
            });

            this.configuration = builder.Build();

            this.zcoinConfiguration = this.configuration.GetZcoinSection();

            this.subject = new IssueTokenController
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
            Assert.Throws<ArgumentNullException>(
                "factory",
                () => new IssueTokenController(null, this.configuration, this.watcher.Object, this.callbackRepository.Object, this.ruleRepository.Object)
            );

            Assert.Throws<ArgumentNullException>(
                "configuration",
                () => new IssueTokenController(this.factory.Object, null, this.watcher.Object, this.callbackRepository.Object, this.ruleRepository.Object)
            );

            Assert.Throws<ArgumentNullException>(
                "watcher",
                () => new IssueTokenController(this.factory.Object, this.configuration, null, this.callbackRepository.Object, this.ruleRepository.Object)
            );

            Assert.Throws<ArgumentNullException>(
                "callbackRepository",
                () => new IssueTokenController(this.factory.Object, this.configuration, this.watcher.Object, null, this.ruleRepository.Object)
            );

            Assert.Throws<ArgumentNullException>(
                "ruleRepository",
                () => new IssueTokenController(this.factory.Object, this.configuration, this.watcher.Object, this.callbackRepository.Object, null)
            );
        }

        [Fact]
        public async Task PostAsync_WithValidArgumentAndUrlWasNotSet_ShouldSuccessButNotAddCallback()
        {
            // Arrange.
            var amount = PropertyAmount.One;
            var note = "Test Issuing";
            var tx = NBitcoin.Transaction.Parse(TestTransaction.Raw1, ZcoinNetworks.Instance.Mainnet);
            var fee = Money.Satoshis(500);

            this.propertyManagementRpc.Setup
            (
                c => c.GrantAsync
                (
                    It.Is<Property>(p => p.Id == this.zcoinConfiguration.Property.Id && p.Type == this.zcoinConfiguration.Property.Type),
                    this.zcoinConfiguration.Property.Issuer.Address,
                    this.zcoinConfiguration.Property.Distributor.Address,
                    It.Is<PropertyAmount>(a => a.Equals(amount)),
                    note,
                    It.IsAny<CancellationToken>()
                )
            ).ReturnsAsync(tx).Verifiable();

            this.rawTransactionRpc.Setup
            (
                c => c.SendAsync
                (
                    tx,
                    It.IsAny<CancellationToken>()
                )
            ).ReturnsAsync(tx.GetHash()).Verifiable();

            var req = new IssueTokenRequest
            {
                Amount = amount,
                Note = note,
            };

            var httpContext = new DefaultHttpContext();
            this.subject.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Act.
            var result = await this.subject.PostAsync(req, CancellationToken.None);

            // Assert.
            this.propertyManagementRpc.Verify();
            this.rawTransactionRpc.Verify();

            var okObjectResult = Assert.IsType<OkObjectResult>(result);
            var returnedTx = okObjectResult.Value;
            var properties = returnedTx.GetType().GetProperties();
            var txVal = (uint256)properties.First(p => p.Name == "Tx").GetValue(returnedTx);

            Assert.Equal(tx.GetHash(), txVal);

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
        public async Task PostAsync_WithValidArgumentAndCallbackUrlIsSet_ShouldSuccess()
        {
            // Arrange.
            var amount = PropertyAmount.One;
            var note = "Test Issuing";
            var tx = NBitcoin.Transaction.Parse(TestTransaction.Raw1, ZcoinNetworks.Instance.Mainnet);
            var fee = Money.Satoshis(500);
            var rawCallbackUrl = "https://zcoin.io/callback";
            var callbackUrl = new Uri(rawCallbackUrl);

            // Setup Rpc client
            this.propertyManagementRpc.Setup
            (
                c => c.GrantAsync
                (
                    It.Is<Property>(p => p.Id == this.zcoinConfiguration.Property.Id && p.Type == this.zcoinConfiguration.Property.Type),
                    this.zcoinConfiguration.Property.Issuer.Address,
                    this.zcoinConfiguration.Property.Distributor.Address,
                    It.Is<PropertyAmount>(a => a.Equals(amount)),
                    note,
                    It.IsAny<CancellationToken>()
                )
            ).ReturnsAsync(tx).Verifiable();

            this.rawTransactionRpc.Setup
            (
                c => c.SendAsync
                (
                    tx,
                    It.IsAny<CancellationToken>()
                )
            ).ReturnsAsync(tx.GetHash()).Verifiable();

            // Construct payload
            var req = new IssueTokenRequest
            {
                Amount = amount,
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
            var result = await this.subject.PostAsync(req, CancellationToken.None);

            // Assert.
            this.propertyManagementRpc.Verify();
            this.rawTransactionRpc.Verify();

            var okObjectResult = Assert.IsType<OkObjectResult>(result);
            var returnedTx = okObjectResult.Value;
            var properties = returnedTx.GetType().GetProperties();
            var txVal = (uint256)properties.First(p => p.Name == "Tx").GetValue(returnedTx);

            Assert.Equal(tx.GetHash(), txVal);

            this.callbackRepository.Verify();
            this.watcher.Verify();

            Assert.Equal(callback.Id.ToString(), httpContext.Response.Headers.TryGet("X-Callback-ID"));
        }
    }
}