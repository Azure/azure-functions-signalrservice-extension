// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;

namespace FunctionApp
{
    public static class Functions
    {
        [FunctionName("send-message")]
        public static async Task SendMessage(
   [HttpTrigger(AuthorizationLevel.Anonymous, "post")]HttpRequestMessage request,
   [SignalR(HubName = "hubname")] IAsyncCollector<SignalRMessage> signalRMessage)
        {
            await signalRMessage.AddAsync(
                new SignalRMessage
                {
                    Target = "message-emitter",
                    Arguments = new[] { "xx", "xxxx" }
                });

        }
    }
}
