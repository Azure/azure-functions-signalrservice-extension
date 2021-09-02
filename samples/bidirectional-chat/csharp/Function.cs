// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Extensions.Logging;

namespace FunctionApp
{
    public class SimpleChat : ServerlessHub<SimpleChat.IChatClient>
    {
        private const string NewMessageTarget = "newMessage";
        private const string NewConnectionTarget = "newConnection";

        public interface IChatClient
        {
            public Task newConnection(NewConnection newConnection);
            public Task newMessage(NewMessage newMessage);
        }

        [FunctionName("negotiate")]
        public async Task<SignalRConnectionInfo> NegotiateAsync([HttpTrigger(AuthorizationLevel.Anonymous)] HttpRequest req)
        {
            var claims = GetClaims(req.Headers["Authorization"]);
            var result = await  NegotiateAsync(new NegotiationOptions
            {
                UserId = claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value,
                Claims = claims
            });
            return result;
        }

        [FunctionName(nameof(OnConnected))]
        public async Task OnConnected([SignalRTrigger]InvocationContext invocationContext, ILogger logger)
        {
            await Clients.All.newConnection(new NewConnection(invocationContext.ConnectionId));
            logger.LogInformation($"{invocationContext.ConnectionId} has connected");
        }

        [FunctionAuthorize]
        [FunctionName(nameof(Broadcast))]
        public async Task Broadcast([SignalRTrigger]InvocationContext invocationContext, string message, ILogger logger)
        {
            await Clients.All.newMessage(new NewMessage(invocationContext, message));
            logger.LogInformation($"{invocationContext.ConnectionId} broadcast {message}");
        }

        [FunctionName(nameof(SendToGroup))]
        public async Task SendToGroup([SignalRTrigger]InvocationContext invocationContext, string groupName, string message)
        {
            await Clients.Group(groupName).newMessage(new NewMessage(invocationContext, message));
        }

        [FunctionName(nameof(SendToUser))]
        public async Task SendToUser([SignalRTrigger]InvocationContext invocationContext, string userName, string message)
        {
            await Clients.User(userName).newMessage(new NewMessage(invocationContext, message));
        }

        [FunctionName(nameof(SendToConnection))]
        public async Task SendToConnection([SignalRTrigger]InvocationContext invocationContext, string connectionId, string message)
        {
            await Clients.Client(connectionId).newMessage(new NewMessage(invocationContext, message));
        }

        [FunctionName(nameof(JoinGroup))]
        public async Task JoinGroup([SignalRTrigger]InvocationContext invocationContext, string connectionId, string groupName)
        {
            await Groups.AddToGroupAsync(connectionId, groupName);
        }

        [FunctionName(nameof(LeaveGroup))]
        public async Task LeaveGroup([SignalRTrigger]InvocationContext invocationContext, string connectionId, string groupName)
        {
            await Groups.RemoveFromGroupAsync(connectionId, groupName);
        }

        [FunctionName(nameof(JoinUserToGroup))]
        public async Task JoinUserToGroup([SignalRTrigger]InvocationContext invocationContext, string userName, string groupName)
        {
            await UserGroups.AddToGroupAsync(userName, groupName);
        }

        [FunctionName(nameof(LeaveUserFromGroup))]
        public async Task LeaveUserFromGroup([SignalRTrigger]InvocationContext invocationContext, string userName, string groupName)
        {
            await UserGroups.RemoveFromGroupAsync(userName, groupName);
        }

        [FunctionName(nameof(OnDisconnected))]
        public void OnDisconnected([SignalRTrigger]InvocationContext invocationContext)
        {
        }

        public class NewConnection
        {
            public string ConnectionId { get; }

            public NewConnection(string connectionId)
            {
                ConnectionId = connectionId;
            }
        }

        public class NewMessage
        {
            public string ConnectionId { get; }
            public string Sender { get; }
            public string Text { get; }

            public NewMessage(InvocationContext invocationContext, string message)
            {
                Sender = string.IsNullOrEmpty(invocationContext.UserId) ? string.Empty : invocationContext.UserId;
                ConnectionId = invocationContext.ConnectionId;
                Text = message;
            }
        }
    }
}
