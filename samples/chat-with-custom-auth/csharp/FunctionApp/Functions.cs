// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace FunctionApp
{
    public static class Functions
    {
        [FunctionName("negotiate")]
        public static Task<HttpResponseMessage> GetSignalRInfo(
            [HttpTrigger(AuthorizationLevel.Anonymous)] HttpRequestMessage req,
            [SignalRConnectionInfoV2(HubName = Constants.HubName)] SignalRConnectionInfoV2 connectionInfoV2)
        {
            return connectionInfoV2.AccessTokenResult.Status == AccessTokenStatus.Valid
                ? Task.FromResult(req.CreateResponse(HttpStatusCode.OK, connectionInfoV2.NegotiateResponse))
                : Task.FromResult(req.CreateErrorResponse(HttpStatusCode.Forbidden, connectionInfoV2.AccessTokenResult.Exception.Message));
        }

        [FunctionName("messages")]
        public static async Task<HttpResponseMessage> SendMessage(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")]HttpRequestMessage req,
            [SignalRConnectionInfoV2(HubName = Constants.HubName)] SignalRConnectionInfoV2 connectionInfoV2,
            [SignalR(HubName = Constants.HubName)]IAsyncCollector<SignalRMessage> signalRMessages)
        {
            if (!PassTokenValidation(req, connectionInfoV2.AccessTokenResult, out var forbiddenActionResult))
            {
                return forbiddenActionResult;
            }

            var message = new JsonSerializer().Deserialize<ChatMessage>(new JsonTextReader(new StreamReader(await req.Content.ReadAsStreamAsync())));

            return await BuildResponseAsync(req, signalRMessages.AddAsync(
                new SignalRMessage
                {
                    UserId = message.Recipient,
                    GroupName = message.Groupname,
                    Target = "newMessage",
                    Arguments = new[] { message }
                }));
        }

        [FunctionName("addToGroup")]
        public static async Task<HttpResponseMessage> AddToGroup(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")]HttpRequestMessage req,
            [SignalRConnectionInfoV2(HubName = Constants.HubName)] SignalRConnectionInfoV2 connectionInfoV2,
            [SignalR(HubName = Constants.HubName)]IAsyncCollector<SignalRGroupAction> signalRGroupActions)
        {
            if (!PassTokenValidation(req, connectionInfoV2.AccessTokenResult, out var forbiddenActionResult))
            {
                return forbiddenActionResult;
            }

            var message = new JsonSerializer().Deserialize<ChatMessage>(new JsonTextReader(new StreamReader(await req.Content.ReadAsStreamAsync())));

            var decodedfConnectionId = GetBase64DecodedString(message.ConnectionId);

            return await BuildResponseAsync(req, signalRGroupActions.AddAsync(
                new SignalRGroupAction
                {
                    ConnectionId = decodedfConnectionId,
                    UserId = message.Recipient,
                    GroupName = message.Groupname,
                    Action = GroupAction.Add
                }));
        }

        [FunctionName("removeFromGroup")]
        public static async Task<HttpResponseMessage> RemoveFromGroup(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")]HttpRequestMessage req,
            [SignalRConnectionInfoV2(HubName = Constants.HubName)] SignalRConnectionInfoV2 connectionInfoV2,
            [SignalR(HubName = Constants.HubName)]IAsyncCollector<SignalRGroupAction> signalRGroupActions)
        {
            if (!PassTokenValidation(req, connectionInfoV2.AccessTokenResult, out var forbiddenActionResult))
            {
                return forbiddenActionResult;
            }
            var message = new JsonSerializer().Deserialize<ChatMessage>(new JsonTextReader(new StreamReader(await req.Content.ReadAsStreamAsync())));

            return await BuildResponseAsync(req, signalRGroupActions.AddAsync(
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

        private static bool PassTokenValidation(HttpRequestMessage req, AccessTokenResult accessTokenResult, out HttpResponseMessage forbiddenResponseMessage)
        {
            if (accessTokenResult.Status != AccessTokenStatus.Valid)
            {
                // failed to pass auth check
                forbiddenResponseMessage =
                    req.CreateErrorResponse(HttpStatusCode.Forbidden, accessTokenResult.Exception.Message);
                return false;
            }

            forbiddenResponseMessage = null;
            return true;
        }

        private static async Task<HttpResponseMessage> BuildResponseAsync(HttpRequestMessage req, Task task)
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                return req.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message);
            }

            return req.CreateResponse(HttpStatusCode.Accepted);
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
