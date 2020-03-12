using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.AspNetCore.SignalR;

namespace FunctionApp
{
    public class SimpleChat : ServerlessHub
    {
        private const string Hub = nameof(SimpleChat);
        private const string NewMessageTarget = "newMessage";
        private const string NewConnectionTarget = "newConnection";

        [FunctionName("negotiate")]
        public static SignalRConnectionInfo GetSignalRInfo(
            [HttpTrigger(AuthorizationLevel.Anonymous)] HttpRequest req,
            [SignalRConnectionInfo(HubName = Hub, UserId = "{headers.x-ms-signalr-user-id}")] SignalRConnectionInfo connectionInfo)
        {
            return connectionInfo;
        }

        [FunctionName(nameof(Connected))]
        public async Task Connected([SignalRTrigger]InvocationContext invocationContext)
        {
            await Clients.All.SendAsync(NewConnectionTarget, new NewConnection(invocationContext.ConnectionId));
        }

        [FunctionAuthorize]
        [FunctionName(nameof(Broadcast))]
        public async Task Broadcast([SignalRTrigger]InvocationContext invocationContext, [SignalRParameter]string message)
        {
            await Clients.All.SendAsync(NewMessageTarget, new NewMessage(invocationContext, message));
        }

        [FunctionName(nameof(SendToGroup))]
        public async Task SendToGroup([SignalRTrigger]InvocationContext invocationContext,
            [SignalRParameter]string groupName,
            [SignalRParameter]string message)
        {
            await Clients.Group(groupName).SendAsync(NewMessageTarget, new NewMessage(invocationContext, message));
        }

        [FunctionName(nameof(SendToUser))]
        public async Task SendToUser([SignalRTrigger]InvocationContext invocationContext,
            [SignalRParameter]string userName,
            [SignalRParameter]string message)
        {
            await Clients.User(userName).SendAsync(NewMessageTarget, new NewMessage(invocationContext, message));
        }

        [FunctionName(nameof(SendToConnection))]
        public async Task SendToConnection([SignalRTrigger]InvocationContext invocationContext,
            [SignalRParameter]string connectionId,
            [SignalRParameter]string message)
        {
            await Clients.Client(connectionId).SendAsync(NewMessageTarget, new NewMessage(invocationContext, message));
        }

        [FunctionName(nameof(JoinGroup))]
        public async Task JoinGroup([SignalRTrigger]InvocationContext invocationContext,
            [SignalRParameter]string connectionId,
            [SignalRParameter]string groupName)
        {
            await Groups.AddToGroupAsync(connectionId, groupName);
        }

        [FunctionName(nameof(LeaveGroup))]
        public async Task LeaveGroup([SignalRTrigger]InvocationContext invocationContext,
            [SignalRParameter]string connectionId,
            [SignalRParameter]string groupName)
        {
            await Groups.RemoveFromGroupAsync(connectionId, groupName);
        }

        [FunctionName(nameof(JoinUserToGroup))]
        public async Task JoinUserToGroup([SignalRTrigger]InvocationContext invocationContext,
            [SignalRParameter]string userName,
            [SignalRParameter]string groupName)
        {
            await UserGroups.AddToGroupAsync(userName, groupName);
        }

        [FunctionName(nameof(LeaveUserFromGroup))]
        public async Task LeaveUserFromGroup([SignalRTrigger]InvocationContext invocationContext,
            [SignalRParameter]string userName,
            [SignalRParameter]string groupName)
        {
            await UserGroups.RemoveFromGroupAsync(userName, groupName);
        }

        [FunctionName(nameof(Disconnect))]
        public void Disconnect([SignalRTrigger]InvocationContext invocationContext)
        {
        }

        private class NewConnection
        {
            public string ConnectionId { get; }

            public NewConnection(string connectionId)
            {
                ConnectionId = connectionId;
            }
        }

        private class NewMessage
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
