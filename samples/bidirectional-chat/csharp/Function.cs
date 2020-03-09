using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;

namespace bidirectional_chat
{
    public static class Function
    {
        private const string Hub = "simplechat";
        private const string NewMessageTarget = "newMessage";
        private const string NewConnectionTarget = "newConnection";

        [FunctionName("negotiate")]
        public static SignalRConnectionInfo GetSignalRInfo(
            [HttpTrigger(AuthorizationLevel.Anonymous)] HttpRequest req,
            [SignalRConnectionInfo(HubName = Hub, UserId = "{headers.x-ms-signalr-user-id}")] SignalRConnectionInfo connectionInfo)
        {
            return connectionInfo;
        }

        [FunctionName("connect")]
        public static async Task Connect([SignalRTrigger(Hub, Category.Connections, Event.Connect)]InvocationContext invocationContext,
            [SignalR(HubName = Hub)]IAsyncCollector<SignalRMessage> signalRMessages)
        {
            await signalRMessages.AddAsync(new SignalRMessage()
            {
                Target = NewConnectionTarget,
                Arguments = new object[] { new NewConnection(invocationContext.ConnectionId) }
            });
        }

        [FunctionAuthorize]
        [FunctionName("broadcast")]
        public static async Task Broadcast([SignalRTrigger(Hub, Category.Messages, "broadcast", "message")]InvocationContext invocationContext,
            [SignalR(HubName = Hub)]IAsyncCollector<SignalRMessage> signalRMessages,
            string message)
        {
            await signalRMessages.AddAsync(new SignalRMessage()
            {
                Target = NewMessageTarget,
                Arguments = new object[] { new NewMessage(invocationContext, message) }
            });
        }

        [FunctionName("sendToGroup")]
        public static async Task SendToGroup([SignalRTrigger(Hub, Category.Messages, "sendToGroup", "groupName", "message")]InvocationContext invocationContext,
            [SignalR(HubName = Hub)]IAsyncCollector<SignalRMessage> signalRMessages,
            string message, string groupName)
        {
            await signalRMessages.AddAsync(new SignalRMessage()
            {
                GroupName = groupName,
                Target = NewMessageTarget,
                Arguments = new object[] { new NewMessage(invocationContext, message) }
            });
        }

        [FunctionName("sendToUser")]
        public static async Task SendToUser([SignalRTrigger(Hub, Category.Messages, "sendToUser", "userName", "message")]InvocationContext invocationContext,
            [SignalR(HubName = Hub)]IAsyncCollector<SignalRMessage> signalRMessages,
            string message, string userName)
        {
            await signalRMessages.AddAsync(new SignalRMessage()
            {
                UserId = userName,
                Target = NewMessageTarget,
                Arguments = new[] { new NewMessage(invocationContext, message) }
            });
        }

        [FunctionName("sendToConnection")]
        public static async Task SendToConnection([SignalRTrigger(Hub, Category.Messages, "sendToConnection", "connectionId", "message")]InvocationContext invocationContext,
            [SignalR(HubName = Hub)]IAsyncCollector<SignalRMessage> signalRMessages,
            string message, string connectionId)
        {
            await signalRMessages.AddAsync(new SignalRMessage()
            {
                ConnectionId = connectionId,
                Target = NewMessageTarget,
                Arguments = new[] { new NewMessage(invocationContext, message) }
            });
        }

        [FunctionName("joinGroup")]
        public static async Task JoinGroup([SignalRTrigger(Hub, Category.Messages, "joinGroup", "connectionId", "groupName")]InvocationContext invocationContext,
            [SignalR(HubName = Hub)]IAsyncCollector<SignalRGroupAction> signalRMessages,
            string connectionId, string groupName)
        {
            await signalRMessages.AddAsync(new SignalRGroupAction()
            {
                ConnectionId = connectionId,
                GroupName = groupName,
                Action = GroupAction.Add
            });
        }

        [FunctionName("leaveGroup")]
        public static async Task LeaveGroup([SignalRTrigger(Hub, Category.Messages, "leaveGroup", "connectionId", "group")]InvocationContext invocationContext,
            [SignalR(HubName = Hub)]IAsyncCollector<SignalRGroupAction> signalRMessages,
            string connectionId, string group)
        {
            await signalRMessages.AddAsync(new SignalRGroupAction()
            {
                ConnectionId = connectionId,
                GroupName = group,
                Action = GroupAction.Remove
            });
        }

        [FunctionName("joinUserToGroup")]
        public static async Task JoinUserToGroup([SignalRTrigger(Hub, Category.Messages, "joinUserToGroup", "userName", "groupName")]InvocationContext invocationContext,
            [SignalR(HubName = Hub)]IAsyncCollector<SignalRGroupAction> signalRMessages,
            string userName, string groupName)
        {
            await signalRMessages.AddAsync(new SignalRGroupAction()
            {
                UserId = userName,
                GroupName = groupName,
                Action = GroupAction.Add
            });
        }

        [FunctionName("leaveUserFromGroup")]
        public static async Task LeaveUserFromGroup([SignalRTrigger(Hub, Category.Messages, "leaveUserFromGroup", "userName", "group")]InvocationContext invocationContext,
            [SignalR(HubName = Hub)]IAsyncCollector<SignalRGroupAction> signalRMessages,
            string userName, string group)
        {
            await signalRMessages.AddAsync(new SignalRGroupAction()
            {
                UserId = userName,
                GroupName = group,
                Action = GroupAction.Remove
            });
        }

        [FunctionName("disconnect")]
        public static void Disconnect([SignalRTrigger(Hub, Category.Connections, Event.Disconnect)]InvocationContext invocationContext,
            [SignalR(HubName = Hub)]IAsyncCollector<SignalRMessage> signalRMessages)
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
