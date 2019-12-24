using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
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
    public class TokenController : ControllerBase
    {
        readonly IZcoinRpcClientFactory factory;
        readonly ZcoinConfiguration configuration;
        readonly ITransactionConfirmationWatcher watcher;
        readonly ICallbackRepository callbackRepository;
        readonly IRuleRepository ruleRepository;

        public TokenController(
            IZcoinRpcClientFactory factory,
            ZcoinConfiguration configuration,
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
        public async Task<IActionResult> Issue([FromBody] Issuing issuing)
        {
            using (var client = await this.factory.CreateRpcClientAsync(CancellationToken.None))
            {
                var property = new Property(this.configuration.Property.Id, this.configuration.Property.Type);

                var tx = await client.GrantPropertyAsync
                (
                    property,
                    configuration.Property.Distributor.Address,
                    issuing.Destination,
                    issuing.Amount,
                    issuing.Note,
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
            return this.ruleRepository.AddAsync(id, 10, TimeSpan.FromDays(1), success, timeout, callback, cancellationToken);
        }
    }

    struct IssuingCallback
    {
        public uint256 Tx { get; set; }
    }
}