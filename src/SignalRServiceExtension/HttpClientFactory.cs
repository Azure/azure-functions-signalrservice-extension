using System;
using System.Net.Http;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal static class HttpClientFactory
    {
        private static Lazy<HttpClient> httpClient = new Lazy<HttpClient>(() => new HttpClient());
        internal static HttpClient GetClient()
        {
            return httpClient.Value;
        }
    }
}