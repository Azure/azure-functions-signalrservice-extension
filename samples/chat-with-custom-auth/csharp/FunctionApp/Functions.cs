// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Authentication;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.SignalR.Common;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FunctionApp
{
    public static class Functions
    {
        /* sample:
           
           Request: send a request with user id "myuserid" in bearer token
           url (GET):  http://localhost:7071/api/negotiate
           headers: [
             "Authorization": "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaHR0cDovL3NjaGVtYXMueG1sc29hcC5vcmcvd3MvMjAwNS8wNS9pZGVudGl0eS9jbGFpbXMvbmFtZSI6Im15dXNlcmlkIiwiaWF0IjoxNTE2MjM5MDIyfQ.5GK9ykQfNGEz07VU_Lwd2QneT9gxEP44o7Zs1y63mcI",
             "myheader": "testclaim"
           ]
           Expected Response:
           {
                "url": "<YOUR ASRS ENDPOINT>/client/?hub=simplechat",
                "accessToken": "<payload contains "nameid": "myuserid" and "myheader": "testclaim">"
            }
        */
        [FunctionName("negotiate")]
        public static Task<IActionResult> GetSignalRInfo(
            [HttpTrigger(AuthorizationLevel.Anonymous)] HttpRequest req,
            [SignalRConnectionInfoV2(HubName = Constants.HubName)] SignalRConnectionInfoV2 connectionInfoV2) // todo: make HubName optional
        {

            return connectionInfoV2.Exception == null ? Task.FromResult((IActionResult)new OkObjectResult(connectionInfoV2.NegotiateResponse)) : Task.FromResult((IActionResult)new ObjectResult(new { statusCode = StatusCodes.Status403Forbidden, message = connectionInfoV2.Exception.Message }));
            //return connectionInfoV2;
        }

        public static class Constants
        {
            public const string HubName = "simplechat";
        }

        [FunctionName("broadcast")]
        public static async Task<IActionResult> Broadcast(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")]HttpRequest req,
            [SignalR(HubName = Constants.HubName)]IAsyncCollector<SignalRMessage> signalRMessages,
            [SignalRConnectionInfoV2(HubName = Constants.HubName)] SignalRConnectionInfoV2 connectionInfoV2)
        {
            var message = new JsonSerializer().Deserialize<ChatMessage>(new JsonTextReader(new StreamReader(req.Body)));
            var serviceHubContext = await StaticServiceHubContextStore.Get().GetAsync("simplechat");

            try
            {
                await signalRMessages.AddAsync(
                    new SignalRMessage
                    {
                        UserId = message.Recipient,
                        GroupName = message.Groupname,
                        Target = "newMessage",
                        Arguments = new[] { message }
                    });
            }
            catch (Exception e)
            {
                // todo: logging 
                return new ObjectResult(new { statusCode = StatusCodes.Status403Forbidden, message = e });
            }

            return new AcceptedResult();
        }

        [FunctionName("messages")]
        public static Task SendMessage(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")]HttpRequest req,
            [SignalR(HubName = Constants.HubName)]IAsyncCollector<SignalRMessage> signalRMessages)
        {
            var message = new JsonSerializer().Deserialize<ChatMessage>(new JsonTextReader(new StreamReader(req.Body)));

            return signalRMessages.AddAsync(
                new SignalRMessage
                {
                    UserId = message.Recipient,
                    GroupName = message.Groupname,
                    Target = "newMessage",
                    Arguments = new[] { message }
                });
        }

        [FunctionName("addToGroup")]
        public static Task AddToGroup(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")]HttpRequest req,
            [SignalR(HubName = Constants.HubName)]IAsyncCollector<SignalRGroupAction> signalRGroupActions)
        {

            var message = new JsonSerializer().Deserialize<ChatMessage>(new JsonTextReader(new StreamReader(req.Body)));

            var decodedfConnectionId = GetBase64DecodedString(message.ConnectionId);

            return signalRGroupActions.AddAsync(
                new SignalRGroupAction
                {
                    ConnectionId = decodedfConnectionId,
                    UserId = message.Recipient,
                    GroupName = message.Groupname,
                    Action = GroupAction.Add
                });
        }

        [FunctionName("removeFromGroup")]
        public static Task RemoveFromGroup(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")]HttpRequest req,
            [SignalR(HubName = Constants.HubName)]IAsyncCollector<SignalRGroupAction> signalRGroupActions)
        {

            var message = new JsonSerializer().Deserialize<ChatMessage>(new JsonTextReader(new StreamReader(req.Body)));

            return signalRGroupActions.AddAsync(
                new SignalRGroupAction
                {
                    ConnectionId = message.ConnectionId,
                    UserId = message.Recipient,
                    GroupName = message.Groupname,
                    Action = GroupAction.Remove
                });
        }

        private static string GetBase64DecodedString(string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return source;
            }

            return Encoding.UTF8.GetString(Convert.FromBase64String(source));
        }

        public class ChatMessage
        {
            public string Sender { get; set; }
            public string Text { get; set; }
            public string Groupname { get; set; }
            public string Recipient { get; set; }
            public string ConnectionId { get; set; }
            public bool IsPrivate { get; set; }
        }
    }
}
