using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using NBitcoin;
using Ztm.Configuration;
using Ztm.WebApi.Callbacks;
using Ztm.WebApi.Models;
using Ztm.WebApi.Watchers.TransactionConfirmation;
using Ztm.Zcoin.NBitcoin.Exodus;
using Ztm.Zcoin.Rpc;
using Transaction = Ztm.WebApi.Models.Transaction;

namespace Ztm.WebApi.Controllers
{
    [ApiController]
    public class IssueTokenController : ControllerBase
    {
        readonly IZcoinRpcClientFactory factory;
        readonly IConfiguration configuration;
        readonly ITransactionConfirmationWatcher watcher;
        readonly ICallbackRepository callbackRepository;
        readonly IRuleRepository ruleRepository;

        public IssueTokenController(
            IZcoinRpcClientFactory factory,
            IConfiguration configuration,
            ITransactionConfirmationWatcher watcher,
            ICallbackRepository callbackRepository,
            IRuleRepository ruleRepository)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (watcher == null)
            {
                throw new ArgumentNullException(nameof(watcher));
            }

            if (callbackRepository == null)
            {
                throw new ArgumentNullException(nameof(callbackRepository));
            }

            if (ruleRepository == null)
            {
                throw new ArgumentNullException(nameof(ruleRepository));
            }

            this.factory = factory;
            this.configuration = configuration;
            this.watcher = watcher;
            this.callbackRepository = callbackRepository;
            this.ruleRepository = ruleRepository;
        }

        [HttpPost("issue-tokens")]
        public async Task<IActionResult> Issue([FromBody] Issuing issueing)
        {
            var zcoinConfig = this.configuration.GetZcoinSection();

            using (var client = await this.factory.CreateRpcClientAsync(CancellationToken.None))
            {
                var property = new Property(zcoinConfig.Property.Id, zcoinConfig.Property.Type);

                var tx = await client.GrantPropertyAsync
                (
                    property,
                    zcoinConfig.Property.Distributor.Address,
                    issueing.Destination,
                    issueing.Amount,
                    issueing.Note,
                    CancellationToken.None
                );

                var id = await client.SendRawTransactionAsync(tx, CancellationToken.None);
                var info = await client.GetExodusTransactionAsync(id, CancellationToken.None);

                var callback = await this.AddCallbackAsync(CancellationToken.None);
                if (callback != null)
                {
                    var callbackResult = new IssuingCallback{Tx=id};

                    await this.AddRuleAsync
                    (
                        id,
                        new CallbackResult("success", callbackResult),
                        new CallbackResult("tokens-issuing-timeout", callbackResult),
                        callback,
                        CancellationToken.None
                    );
                }

                return Ok(new Transaction{Tx = id, Fee = info.Fee});
            };
        }

        async Task<Callback> AddCallbackAsync(CancellationToken cancellationToken)
        {
            if (!this.TryGetCallbackUrl(out var url))
            {
                return null;
            }

            var callback = await this.callbackRepository.AddAsync(this.HttpContext.Connection.RemoteIpAddress, url, cancellationToken);
            this.SetCallbackId(callback.Id);

            return callback;
        }

        Task<Rule> AddRuleAsync(uint256 id, CallbackResult success, CallbackResult timeout, Callback callback, CancellationToken cancellationToken)
        {
            var config = this.configuration.GetCallbackSection();
            return this.watcher.AddTransactionAsync(
                id, config.TransactionConfirmation.RequiredConfirmation, config.TransactionConfirmation.Timeout, callback, success, timeout, cancellationToken);
        }
    }

    struct IssuingCallback
    {
        public uint256 Tx { get; set; }
    }
}