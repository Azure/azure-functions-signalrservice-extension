// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Newtonsoft.Json;

namespace FunctionApp
{
    public static class Functions
    {
        [FunctionName("negotiate")]
        public static IActionResult GetSignalRInfo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", "options")]HttpRequest req, 
            [SignalRConnectionInfo(HubName = "simplechat")]SignalRConnectionInfo connectionInfo)
        {
            // Azure function doesn't support CORS well, workaround it by explicitly return CORS headers
            req.HttpContext.Response.Headers.Add("Access-Control-Allow-Credentials", "true");
            if (req.Headers["Origin"].Count > 0) req.HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", req.Headers["Origin"][0]);
            if (req.Headers["Access-Control-Request-Headers"].Count > 0) req.HttpContext.Response.Headers.Add("Access-Control-Allow-Headers", req.Headers["access-control-request-headers"][0]);

            return new OkObjectResult(connectionInfo);
        }

        [FunctionName("messages")]
        public static async Task<IActionResult> SendMessage(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", "options")]HttpRequest req,
            [SignalR(HubName = "simplechat")]IAsyncCollector<SignalRMessage> signalRMessages)
        {
            // Azure function doesn't support CORS well, workaround it by explicitly return CORS headers
            req.HttpContext.Response.Headers.Add("Access-Control-Allow-Credentials", "true");
            if (req.Headers["Origin"].Count > 0) req.HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", req.Headers["Origin"][0]);
            if (req.Headers["Access-Control-Request-Headers"].Count > 0) req.HttpContext.Response.Headers.Add("Access-Control-Allow-Headers", req.Headers["access-control-request-headers"][0]);

            if (req.Method == "POST")
            {
                var message = new JsonSerializer().Deserialize(new JsonTextReader(new StreamReader(req.Body)));

                await signalRMessages.AddAsync(
                    new SignalRMessage
                    {
                        Target = "newMessage",
                        Arguments = new[] { message }
                    });
            }

            return new OkResult();
        }
    }
}
