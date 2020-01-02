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
    [Route("issue-tokens")]
    [ApiController]
    public class IssueTokenController : ControllerBase
    {
        static readonly string StatusSuccess = "success";
        static readonly string StatusTimeout = "tokens-issuing-timeout";

        readonly IRpcFactory factory;
        readonly IConfiguration configuration;
        readonly ITransactionConfirmationWatcher watcher;
        readonly ICallbackRepository callbackRepository;
        readonly IRuleRepository ruleRepository;

        readonly ZcoinConfiguration zcoinConfig;
        readonly CallbackConfiguration callbackConfig;

        public IssueTokenController(
            IRpcFactory factory,
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

            this.zcoinConfig = this.configuration.GetZcoinSection();
            this.callbackConfig = this.configuration.GetCallbackSection();
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync([FromBody] IssueRequest issueing, CancellationToken cancellationToken)
        {
            using (var propertyManagementRpc = await this.factory.CreatePropertyManagementRpcAsync(cancellationToken))
            using (var rawTransactionRpc = await this.factory.CreateRawTransactionRpcAsync(cancellationToken))
            using (var exodusInformationRpc = await this.factory.CreateExodusInformationRpcAsync(cancellationToken))
            {
                var property = new Property(this.zcoinConfig.Property.Id, this.zcoinConfig.Property.Type);

                var tx = await propertyManagementRpc.GrantAsync
                (
                    property,
                    this.zcoinConfig.Property.Issuer.Address,
                    this.zcoinConfig.Property.Distributor.Address,
                    issueing.Amount,
                    issueing.Note,
                    cancellationToken
                );

                var id = await rawTransactionRpc.SendAsync(tx, cancellationToken);
                var info = await exodusInformationRpc.GetTransactionAsync(id, CancellationToken.None);

                var callback = await this.AddCallbackAsync(CancellationToken.None);
                if (callback != null)
                {
                    var callbackResult = new {Tx = id};

                    await this.WatchTransactionAsync
                    (
                        id,
                        new CallbackResult(StatusSuccess, callbackResult),
                        new CallbackResult(StatusTimeout, callbackResult),
                        callback,
                        CancellationToken.None
                    );
                }

                return Ok(new Transaction
                {
                    Tx = id,
                    Fee = info.Fee
                });
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

        Task<Rule> WatchTransactionAsync(uint256 id, CallbackResult success, CallbackResult timeout, Callback callback, CancellationToken cancellationToken)
        {
            return this.watcher.AddTransactionAsync(
                id,
                this.callbackConfig.TransactionConfirmation.RequiredConfirmation,
                this.callbackConfig.TransactionConfirmation.Timeout,
                callback,
                success,
                timeout,
                cancellationToken);
        }
    }
}