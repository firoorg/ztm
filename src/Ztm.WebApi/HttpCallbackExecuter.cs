using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Ztm.WebApi
{
    public class HttpCallbackExecuter : ICallbackExecuter
    {
        readonly HttpClient client;

        public HttpCallbackExecuter(HttpClient client)
        {
            this.client = client;
        }

        public async Task<bool> Execute(Guid id, Uri url, CallbackResult result, CancellationToken cancellationToken)
        {
            var content = JsonConvert.SerializeObject(result.Data);

            using (var request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                request.Headers.Add("X-Callback-ID", id.ToString());
                request.Headers.Add("X-Callback-Status", result.Status);
                request.Content = new StringContent(content, Encoding.UTF8, "application/json");

                var response = await client.SendAsync(request);

                switch(response.StatusCode)
                {
                case HttpStatusCode.OK:
                case HttpStatusCode.Accepted:
                    return true;
                }
            }

            return false;
        }
    }
}