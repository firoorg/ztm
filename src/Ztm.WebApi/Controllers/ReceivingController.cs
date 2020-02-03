using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Ztm.Configuration;
using Ztm.WebApi.AddressPools;
using Ztm.WebApi.Models;
using Ztm.WebApi.Watchers.TokenReceiving;

namespace Ztm.WebApi.Controllers
{
    [ApiController]
    [Route("receiving")]
    public sealed class ReceivingController : ControllerBase
    {
        public const string TimeoutStatus = "tokens-receive-timeout";

        readonly ApiConfiguration config;
        readonly ControllerHelper helper;
        readonly IReceivingAddressPool pool;
        readonly ITokenReceivingWatcher watcher;

        public ReceivingController(
            IConfiguration config,
            IReceivingAddressPool pool,
            ITokenReceivingWatcher watcher,
            ControllerHelper helper)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (pool == null)
            {
                throw new ArgumentNullException(nameof(pool));
            }

            if (watcher == null)
            {
                throw new ArgumentNullException(nameof(watcher));
            }

            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            this.pool = pool;
            this.watcher = watcher;
            this.helper = helper;
            this.config = config.GetApiSection();
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync(
            [FromBody] ReceivingRequest req,
            CancellationToken cancellationToken)
        {
            var timeout = this.config.Default.TransactionTimeout;
            var reserve = await this.pool.TryLockAddressAsync(cancellationToken);

            if (reserve == null)
            {
                return StatusCode((int)HttpStatusCode.ServiceUnavailable);
            }

            try
            {
                var callback = await this.helper.RegisterCallbackAsync(this, CancellationToken.None);

                await this.watcher.StartWatchAsync(
                    reserve,
                    req.TargetAmount.Value,
                    this.config.Default.RequiredConfirmation,
                    timeout,
                    callback != null ? new TokenReceivingCallback(callback, TimeoutStatus) : null,
                    CancellationToken.None);
            }
            catch
            {
                await this.pool.ReleaseAddressAsync(reserve.Id, CancellationToken.None);
                throw;
            }

            return Accepted(new ReceivingResponse()
            {
                Address = reserve.Address.Address,
                Deadline = DateTime.UtcNow + timeout,
            });
        }
    }
}
