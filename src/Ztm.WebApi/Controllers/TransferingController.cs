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
        readonly ICallbackRepository callbackRepository;
        readonly IRuleRepository ruleRepository;

        readonly ApiConfiguration apiConfiguration;
        readonly ZcoinConfiguration zcoinConfiguration;

        public TransferingController(
            IRpcFactory factory,
            ITransactionConfirmationWatcher watcher,
            ICallbackRepository callbackRepository,
            IRuleRepository ruleRepository,
            IConfiguration configuration)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
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

            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            this.factory = factory;
            this.watcher = watcher;
            this.callbackRepository = callbackRepository;
            this.ruleRepository = ruleRepository;

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
                    throw new InsufficientTokenException();
                }
                catch (RPCException ex) when ((int)ex.RPCResult.Error.Code == -212)
                {
                    throw new InputsChoosingException();
                }

                var id = await rawTransactionRpc.SendAsync(tx, cancellationToken);

                var callback = await this.AddCallbackAsync(CancellationToken.None);

                if (callback != null)
                {
                    var callbackResult = new {Tx = id};

                    await this.WatchTransactionAsync
                    (
                        id,
                        new CallbackResult(CallbackResult.StatusSuccess, callbackResult),
                        new CallbackResult("tokens-transfer-timeout", callbackResult),
                        callback,
                        CancellationToken.None
                    );
                }

                return Ok(new {Tx = id});
            }
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
                this.apiConfiguration.Default.RequiredConfirmation,
                this.apiConfiguration.Default.TransactionTimeout,
                callback,
                success,
                timeout,
                cancellationToken);
        }
    }
}