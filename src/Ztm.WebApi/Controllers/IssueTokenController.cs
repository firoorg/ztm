using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using NBitcoin;
using NBitcoin.RPC;
using Ztm.Configuration;
using Ztm.WebApi.Callbacks;
using Ztm.WebApi.Models;
using Ztm.WebApi.Watchers.TransactionConfirmation;
using Ztm.Zcoin.NBitcoin.Exodus;
using Ztm.Zcoin.Rpc;

namespace Ztm.WebApi.Controllers
{
    [Route("issue-tokens")]
    [ApiController]
    public class IssueTokenController : ControllerBase
    {
        readonly IRpcFactory factory;
        readonly IConfiguration configuration;
        readonly ITransactionConfirmationWatcher watcher;
        readonly IRuleRepository ruleRepository;

        readonly ZcoinConfiguration zcoinConfig;
        readonly ApiConfiguration apiConfig;

        readonly ControllerHelper helper;

        public IssueTokenController(
            IRpcFactory factory,
            IConfiguration configuration,
            ITransactionConfirmationWatcher watcher,
            IRuleRepository ruleRepository,
            ControllerHelper helper)
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

            if (ruleRepository == null)
            {
                throw new ArgumentNullException(nameof(ruleRepository));
            }

            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            this.factory = factory;
            this.configuration = configuration;
            this.watcher = watcher;
            this.ruleRepository = ruleRepository;
            this.helper = helper;

            this.zcoinConfig = this.configuration.GetZcoinSection();
            this.apiConfig = this.configuration.GetApiSection();
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync([FromBody] IssueTokenRequest req, CancellationToken cancellationToken)
        {
            using (var propertyManagementRpc = await this.factory.CreatePropertyManagementRpcAsync(cancellationToken))
            using (var rawTransactionRpc = await this.factory.CreateRawTransactionRpcAsync(cancellationToken))
            {
                var property = new Property(this.zcoinConfig.Property.Id, this.zcoinConfig.Property.Type);

                Transaction tx;
                try
                {
                    tx = await propertyManagementRpc.GrantAsync(
                        property,
                        this.zcoinConfig.Property.Issuer.Address,
                        this.zcoinConfig.Property.Distributor.Address,
                        req.Amount.Value,
                        req.Note,
                        cancellationToken);
                }
                catch (RPCException ex) when (ex.IsInsufficientFee())
                {
                    return this.InsufficientFee();
                }

                var id = await rawTransactionRpc.SendAsync(tx, cancellationToken);

                var callback = await this.helper.RegisterCallbackAsync(this, CancellationToken.None);
                if (callback != null)
                {
                    var callbackResult = new { Tx = id };

                    await this.watcher.AddTransactionAsync(
                        id,
                        this.apiConfig.Default.RequiredConfirmation,
                        this.apiConfig.Default.TransactionTimeout,
                        callback,
                        new CallbackResult(CallbackResult.StatusSuccess, callbackResult),
                        new CallbackResult("tokens-issuing-timeout", callbackResult),
                        CancellationToken.None);
                }

                return Accepted(new { Tx = id });
            }
        }
    }
}
