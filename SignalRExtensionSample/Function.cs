using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Azure.WebJobs.Extensions.SignalRService.Protocols;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace SignalRExtensionSample
{
    public static class Function
    {
        [FunctionName("OpenConnectionFunction")]
        public static void Open([SignalROpenConnectionTrigger("EventHub", Hub = "SignalRHub1", Connection = "ConnectionString")]
            string message, ILogger logger)
        {
            logger.LogInformation($"SignalRHub1 receive message: {message}");
        }

        [FunctionName("CloseConnectionFunction")]
        public static void Close([SignalRCloseConnectionTrigger("EventHub", Hub = "SignalRHub1", Connection = "ConnectionString")]
            string message, ILogger logger)
        {
            logger.LogInformation($"SignalRHub1 receive message: {message}");
        }

        [FunctionName("InvocationFunction")]
        public static void Invocation([SignalRInvocationMessageTrigger("EventHub", Hub = "SignalRHub1", Target = "TestMethod", Connection = "ConnectionString")]
            string message, string ConnectionId, ILogger logger)
        {
            logger.LogInformation($"SignalRHub1 receive message: {message}");
        }

        [FunctionName("InvocationFunction2")]
        public static void Invocation2([SignalRInvocationMessageTrigger("EventHub", Hub = "SignalRHub1", Target = "TestMethod", Connection = "ConnectionString")]
            SignalRExtensionMessage message, string ConnectionId, ILogger logger)
        {
            logger.LogInformation($"SignalRHub1 receive message: {Encoding.UTF8.GetString(message.Body.Array)}");
        }

        [FunctionName("InvocationFunction3")]
        public static void Invocation3([SignalRInvocationMessageTrigger("EventHub", Hub = "SignalRHub1", Target = "TestMethod", Connection = "ConnectionString")]
            CustomerClass message, ILogger logger)
        {
            logger.LogInformation($"SignalRHub1 receive message: {message.ConnectionId}");
        }

        public class CustomerClass
        {
            [JsonProperty]
            public string ConnectionId { get; set; }

            [JsonProperty]
            public string Target { get; set; }
        }
    }
}
