// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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
        public static SignalRConnectionInfo GetSignalRInfo(
            [HttpTrigger(AuthorizationLevel.Anonymous)] HttpRequest req,
            [SignalRConnectionInfo(HubName = "simplechat", UserId = "{headers.x-ms-signalr-userid}")] SignalRConnectionInfo connectionInfo)
        {
            return connectionInfo;
        }

        [FunctionName("messages")]
        public static Task SendMessage(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")]HttpRequest req,
            [SignalR(HubName = "simplechat")]IAsyncCollector<SignalRMessage> signalRMessages)
        {

            var message = new JsonSerializer().Deserialize<ChatMessage>(new JsonTextReader(new StreamReader(req.Body)));

            return signalRMessages.AddAsync(
                new SignalRMessage
                {
                    UserId = message.recipient,
                    GroupName = message.groupname,
                    Target = "newMessage",
                    Arguments = new[] { message }
                });
        }

        [FunctionName("group")]
        public static Task Group(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")]HttpRequest req,
            [SignalR(HubName = "simplechat")]IAsyncCollector<SignalRGroupAction> signalRGroupActions)
        {

            var message = new JsonSerializer().Deserialize<ChatMessage>(new JsonTextReader(new StreamReader(req.Body)));
            var action = message.action.Equals("add", StringComparison.OrdinalIgnoreCase) ? GroupAction.Add : GroupAction.Remove;


            return signalRGroupActions.AddAsync(
                new SignalRGroupAction
                {
                    UserId = message.recipient,
                    GroupName = message.groupname,
                    Action = action
                });
        }

        public class ChatMessage
        {
            public string sender { get; set; }
            public string text { get; set; }
            public string groupname { get; set; }
            public string action { get; set; }
            public string recipient { get; set; }
            public bool isPrivate { get; set; }
        }
    }
}
