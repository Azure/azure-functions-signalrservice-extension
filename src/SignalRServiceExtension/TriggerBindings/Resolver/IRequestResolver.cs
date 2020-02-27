using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Azure.SignalR.Serverless.Protocols;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal interface IRequestResolver
    {
        bool ValidateContentType(HttpRequestMessage request);

        bool ValidateSignature(HttpRequestMessage request, string accessKey);

        bool TryGetInvocationContext(HttpRequestMessage request, out InvocationContext context);

        Task<(T, IHubProtocol)> GetMessageAsync<T>(HttpRequestMessage request) where T : ServerlessMessage, new();
    }
}
