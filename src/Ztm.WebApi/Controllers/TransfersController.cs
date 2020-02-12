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
    [Route("transfers")]
    [ApiController]
    public class TransfersController : ControllerBase
    {
        readonly IRpcFactory rpc;
        readonly ITransactionConfirmationWatcher watcher;
        readonly IRuleRepository ruleRepository;

        readonly ApiConfiguration apiConfiguration;
        readonly ZcoinConfiguration zcoinConfiguration;

        readonly ControllerHelper helper;

        public TransfersController(
            IRpcFactory rpc,
            ITransactionConfirmationWatcher watcher,
            IRuleRepository ruleRepository,
            IConfiguration configuration,
            ControllerHelper helper)
        {
            if (rpc == null)
            {
                throw new ArgumentNullException(nameof(rpc));
            }

            if (watcher == null)
            {
                throw new ArgumentNullException(nameof(watcher));
            }

            if (ruleRepository == null)
            {
                throw new ArgumentNullException(nameof(ruleRepository));
            }

            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            this.rpc = rpc;
            this.watcher = watcher;
            this.ruleRepository = ruleRepository;
            this.helper = helper;

            this.apiConfiguration = configuration.GetApiSection();
            this.zcoinConfiguration = configuration.GetZcoinSection();
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync([FromBody] TransferRequest req, CancellationToken cancellationToken)
        {
            using (var propertyManagementRpc = await this.rpc.CreatePropertyManagementRpcAsync(cancellationToken))
            using (var rawTransactionRpc = await this.rpc.CreateRawTransactionRpcAsync(cancellationToken))
            {
                Transaction tx;
                try
                {
                    tx = await propertyManagementRpc.SendAsync(
                        this.zcoinConfiguration.Property.Distributor.Address,
                        req.Destination,
                        new Property(this.zcoinConfiguration.Property.Id, this.zcoinConfiguration.Property.Type),
                        req.Amount.Value,
                        req.ReferenceAmount,
                        cancellationToken);
                }
                catch (RPCException ex) when (ex.IsInsufficientToken())
                {
                    return this.InsufficientToken();
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
                        this.apiConfiguration.Default.RequiredConfirmation,
                        this.apiConfiguration.Default.TransactionTimeout,
                        callback,
                        new CallbackResult(CallbackResult.StatusSuccess, callbackResult),
                        new CallbackResult("tokens-transfer-timeout", callbackResult),
                        CancellationToken.None);
                }

                return Accepted(new { Tx = id });
            }
        }
    }
}
