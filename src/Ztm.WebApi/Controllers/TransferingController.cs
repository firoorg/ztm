using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using NBitcoin;
using NBitcoin.RPC;
using Ztm.Configuration;
using Ztm.WebApi.ApiExceptions;
using Ztm.WebApi.Callbacks;
using Ztm.WebApi.Models;
using Ztm.WebApi.Watchers.TransactionConfirmation;
using Ztm.Zcoin.NBitcoin;
using Ztm.Zcoin.NBitcoin.Exodus;
using Ztm.Zcoin.Rpc;

namespace Ztm.WebApi.Controllers
{
    [Route("transfers")]
    [ApiController]
    public class TransferingController : ControllerBase
    {
        readonly IRpcFactory factory;
        readonly ITransactionConfirmationWatcher watcher;
        readonly IRuleRepository ruleRepository;

        readonly ApiConfiguration apiConfiguration;
        readonly ZcoinConfiguration zcoinConfiguration;

        readonly ControllerHelper helper;

        public TransferingController(
            IRpcFactory factory,
            ITransactionConfirmationWatcher watcher,
            IRuleRepository ruleRepository,
            IConfiguration configuration,
            ControllerHelper helper)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
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

            this.factory = factory;
            this.watcher = watcher;
            this.ruleRepository = ruleRepository;
            this.helper = helper;

            this.apiConfiguration = configuration.GetApiSection();
            this.zcoinConfiguration = configuration.GetZcoinSection();
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync([FromBody] TransferingRequest req, CancellationToken cancellationToken)
        {
            var network = ZcoinNetworks.Instance.GetNetwork(this.zcoinConfiguration.Network.Type);

            using (var propertyManagementRpc = await this.factory.CreatePropertyManagementRpcAsync(cancellationToken))
            using (var rawTransactionRpc = await this.factory.CreateRawTransactionRpcAsync(cancellationToken))
            {
                Transaction tx;
                try
                {
                    tx = await propertyManagementRpc.SendAsync
                    (
                        this.zcoinConfiguration.Property.Distributor.Address,
                        req.Destination,
                        new Property(this.zcoinConfiguration.Property.Id, this.zcoinConfiguration.Property.Type),
                        req.Amount,
                        req.ReferenceAmount,
                        cancellationToken
                    );
                }
                catch (RPCException ex) when (ex.RPCResult.Error.Code == RPCErrorCode.RPC_TYPE_ERROR
                    && ex.RPCResult.Error.Message == "Sender has insufficient balance")
                {
                    return this.InsufficientToken();
                }
                catch (RPCException ex) when ((int)ex.RPCResult.Error.Code == -212)
                {
                    return this.InsufficientFee();
                }

                var id = await rawTransactionRpc.SendAsync(tx, cancellationToken);
                var callback = await this.helper.TryAddCallbackAsync(this, CancellationToken.None);

                if (callback != null)
                {
                    var callbackResult = new {Tx = id};

                    await this.watcher.AddTransactionAsync
                    (
                        id,
                        this.apiConfiguration.Default.RequiredConfirmation,
                        this.apiConfiguration.Default.TransactionTimeout,
                        callback,
                        new CallbackResult(CallbackResult.StatusSuccess, callbackResult),
                        new CallbackResult("tokens-transfer-timeout", callbackResult),
                        CancellationToken.None
                    );
                }

                return Ok(new {Tx = id});
            }
        }
    }
}