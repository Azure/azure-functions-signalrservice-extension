// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace FunctionApp
{
    public static class Functions
    {
        [FunctionName("negotiate")]
        public static Task<IActionResult> GetSignalRInfo(
            [HttpTrigger(AuthorizationLevel.Anonymous)] HttpRequest req,
            [SignalRConnectionInfoV2(HubName = Constants.HubName)] SignalRConnectionInfoV2 connectionInfoV2)
        {

            return connectionInfoV2.AccessTokenResult.Status == AccessTokenStatus.Valid ?
                Task.FromResult((IActionResult)new OkObjectResult(connectionInfoV2.NegotiateResponse)) :
                Task.FromResult((IActionResult)new ObjectResult(new { statusCode = StatusCodes.Status403Forbidden, message = connectionInfoV2.AccessTokenResult.Exception.Message }));
        }

        [FunctionName("messages")]
        public static async Task<IActionResult> SendMessage(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")]HttpRequest req,
            [SignalRConnectionInfoV2(HubName = Constants.HubName)] SignalRConnectionInfoV2 connectionInfoV2,
            [SignalR(HubName = Constants.HubName)]IAsyncCollector<SignalRMessage> signalRMessages)
        {
            if (!PassTokenValidation(connectionInfoV2.AccessTokenResult, out var forbiddenActionResult))
            {
                return forbiddenActionResult;
            }

            var message = new JsonSerializer().Deserialize<ChatMessage>(new JsonTextReader(new StreamReader(req.Body)));

            return await BuildResponseAsync(signalRMessages.AddAsync(
                new SignalRMessage
                {
                    UserId = message.Recipient,
                    GroupName = message.Groupname,
                    Target = "newMessage",
                    Arguments = new[] { message }
                }));
        }

        [FunctionName("addToGroup")]
        public static async Task<IActionResult> AddToGroup(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")]HttpRequest req,
            [SignalRConnectionInfoV2(HubName = Constants.HubName)] SignalRConnectionInfoV2 connectionInfoV2,
            [SignalR(HubName = Constants.HubName)]IAsyncCollector<SignalRGroupAction> signalRGroupActions)
        {
            if (!PassTokenValidation(connectionInfoV2.AccessTokenResult, out var forbiddenActionResult))
            {
                return forbiddenActionResult;
            }

            var message = new JsonSerializer().Deserialize<ChatMessage>(new JsonTextReader(new StreamReader(req.Body)));

            var decodedfConnectionId = GetBase64DecodedString(message.ConnectionId);

            return await BuildResponseAsync(signalRGroupActions.AddAsync(
                new SignalRGroupAction
                {
                    ConnectionId = decodedfConnectionId,
                    UserId = message.Recipient,
                    GroupName = message.Groupname,
                    Action = GroupAction.Add
                }));
        }

        [FunctionName("removeFromGroup")]
        public static async Task<IActionResult> RemoveFromGroup(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")]HttpRequest req,
            [SignalRConnectionInfoV2(HubName = Constants.HubName)] SignalRConnectionInfoV2 connectionInfoV2,
            [SignalR(HubName = Constants.HubName)]IAsyncCollector<SignalRGroupAction> signalRGroupActions)
        {
            if (!PassTokenValidation(connectionInfoV2.AccessTokenResult, out var forbiddenActionResult))
            {
                return forbiddenActionResult;
            }
            var message = new JsonSerializer().Deserialize<ChatMessage>(new JsonTextReader(new StreamReader(req.Body)));

            return await BuildResponseAsync(signalRGroupActions.AddAsync(
                new SignalRGroupAction
                {
                    ConnectionId = message.ConnectionId,
                    UserId = message.Recipient,
                    GroupName = message.Groupname,
                    Action = GroupAction.Remove
                }));
        }

        private static string GetBase64DecodedString(string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return source;
            }

            return Encoding.UTF8.GetString(Convert.FromBase64String(source));
        }

        private static bool PassTokenValidation(AccessTokenResult accessTokenResult, out IActionResult forbiddenActionResult)
        {
            if (accessTokenResult.Status != AccessTokenStatus.Valid)
            {
                // failed to pass auth check
                forbiddenActionResult = new ObjectResult(new
                {
                    statusCode = StatusCodes.Status403Forbidden,
                    message = accessTokenResult.Exception.Message
                });
                return false;
            }

            forbiddenActionResult = null;
            return true;
        }

        private static async Task<IActionResult> BuildResponseAsync(Task task)
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                // define your own response here
                return new ObjectResult(new
                {
                    statusCode = StatusCodes.Status500InternalServerError,
                    message = ex.Message
                });
            }

            return new AcceptedResult();
        }

        public static class Constants
        {
            public const string HubName = "simplechat";
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
