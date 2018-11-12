using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Azure.WebJobs.Extensions.SignalRService.Trigger;
using Microsoft.Extensions.Logging;

namespace SignalRExtensionSample
{
    public static class Function
    {
        [FunctionName("Function1")]
        public static void Run([SignalRInvocationMessageTrigger("function1", "TestHub", Connection = "ConnectionString")]
            string message, string ConnectionId, ILogger logger)
        {
            //logger.LogInformation($"Receive message: {message.Method}, {message.Hub}, {message.Arguments}, {ConnectionId}");
            logger.LogInformation($"Receive message: {message}");
        }
    }
}
