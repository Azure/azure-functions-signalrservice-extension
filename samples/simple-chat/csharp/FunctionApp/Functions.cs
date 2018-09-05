using System;
using System.IO;
using System.Threading.Tasks;
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
        public static async Task<IActionResult> GetMessages(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")]HttpRequest req, 
            [SignalR(HubName = "simplechat")]IAsyncCollector<SignalRMessage> signalRMessages, 
            ILogger log)
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

            await signalRMessages.AddAsync(
                new SignalRMessage 
                {
                    Target = "newMessage", 
                    Arguments = new [] { bodyObject } 
                });

            return new OkResult();
        }

        [FunctionName("negotiate")]
        public static IActionResult GetSignalRInfo(
            [HttpTrigger(AuthorizationLevel.Anonymous)]HttpRequest req, 
            [SignalRConnectionInfo(HubName = "simplechat")]SignalRConnectionInfo connectionInfo, 
            ILogger log)
        {
            return new OkObjectResult(connectionInfo);
        }
    }
}
