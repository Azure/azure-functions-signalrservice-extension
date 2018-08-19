using System;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FunctionApp
{
    public static class Functions
    {
        [FunctionName("messages")]
        public static async System.Threading.Tasks.Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]HttpRequest req, [SignalR(HubName = @"simplechat")]IAsyncCollector<SignalRMessage> signalRmessages, ILogger log)
        {
            JToken bodyObject;
            try
            {
                bodyObject = JToken.ReadFrom(new JsonTextReader(new StreamReader(req.Body)));
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.ToString());
            }

            await signalRmessages.AddAsync(new SignalRMessage { Target = @"newMessage", Arguments = new object[] { bodyObject } });

            return new OkResult();
        }

        [FunctionName("negotiate")]
        public static IActionResult RunNegotiate([HttpTrigger(AuthorizationLevel.Anonymous, Route = null)]HttpRequest req, [SignalRConnectionInfo(HubName = @"simplechat")]AzureSignalRConnectionInfo connectionInfo, ILogger log)
        {
            return new OkObjectResult(JsonConvert.SerializeObject(connectionInfo));
        }
    }
}
