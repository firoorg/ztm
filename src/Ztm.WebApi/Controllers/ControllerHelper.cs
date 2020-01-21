using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Ztm.WebApi.Callbacks;

namespace Ztm.WebApi.Controllers
{
    public class ControllerHelper
    {
        readonly ICallbackRepository callbackRepository;

        public ControllerHelper(
            ICallbackRepository callbackRepository)
        {
            if (callbackRepository == null)
            {
                throw new ArgumentNullException(nameof(callbackRepository));
            }

            this.callbackRepository = callbackRepository;
        }

        public async Task<Callback> RegisterCallbackAsync(ControllerBase controller, CancellationToken cancellationToken)
        {
            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller));
            }

            if (!controller.TryGetCallbackUrl(out var url))
            {
                return null;
            }

            var callback = await this.callbackRepository.AddAsync(
                controller.HttpContext.Connection.RemoteIpAddress,
                url,
                cancellationToken);

            controller.SetCallbackId(callback.Id);

            return callback;
        }
    }
}
