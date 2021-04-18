using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Milou.Deployer.Web.Agent
{
    public static class HttpClientExtensions
    {
        public static async Task<HttpResponseMessage> PostAsJson<T>(this HttpClient httpClient, Uri url, T instance, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url);

            string? json = JsonConvert.SerializeObject(instance);

            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            return await httpClient.SendAsync(request, cancellationToken);
        }
    }
}