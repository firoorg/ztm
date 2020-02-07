using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Ztm.WebApi.AddressPools;
using Ztm.WebApi.Models;

namespace Ztm.WebApi.Controllers
{
    [ApiController]
    [Route("receiving-addresses")]
    public sealed class ReceivingAddressesController : ControllerBase
    {
        readonly IReceivingAddressPool pool;

        public ReceivingAddressesController(IReceivingAddressPool pool)
        {
            if (pool == null)
            {
                throw new ArgumentNullException(nameof(pool));
            }

            this.pool = pool;
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync(
            [FromBody] CreateReceivingAddressesRequest req,
            CancellationToken cancellationToken)
        {
            var address = await this.pool.GenerateAddressAsync(cancellationToken);

            return Ok(new CreateReceivingAddressesResponse()
            {
                Address = address.Address,
            });
        }
    }
}
