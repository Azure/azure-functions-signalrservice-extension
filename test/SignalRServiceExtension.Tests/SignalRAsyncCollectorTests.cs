// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace SignalRServiceExtension.Tests
{
    public class SignalRAsyncCollectorTests
    {
        private const string DefaultHubName = "chathub";

        [Fact]
        public async Task AddAsync_WithBroadcastMessage_CallsSendToAll()
        {
            var signalRSenderMock = new Mock<IAzureSignalRSender>();
            var collector = new SignalRAsyncCollector<SignalRMessage>(signalRSenderMock.Object);

            await collector.AddAsync(new SignalRMessage
            {
                Target = "newMessage",
                Arguments = new object[] { "arg1", "arg2" }
            });

            signalRSenderMock.Verify(c => c.SendToAll(It.IsAny<SignalRData>()), Times.Once);
            signalRSenderMock.VerifyNoOtherCalls();
            var actualData = (SignalRData)signalRSenderMock.Invocations[0].Arguments[0];
            Assert.Equal("newMessage", actualData.Target);
            Assert.Equal("arg1", actualData.Arguments[0]);
            Assert.Equal("arg2", actualData.Arguments[1]);
        }

        [Fact]
        public async Task AddAsync_WithUserId_CallsSendToUser()
        {
            var signalRSenderMock = new Mock<IAzureSignalRSender>();
            var collector = new SignalRAsyncCollector<SignalRMessage>(signalRSenderMock.Object);

            await collector.AddAsync(new SignalRMessage
            {
                UserId = "userId1",
                Target = "newMessage",
                Arguments = new object[] { "arg1", "arg2" }
            });

            signalRSenderMock.Verify(
                c => c.SendToUser("userId1", It.IsAny<SignalRData>()),
                Times.Once);
            signalRSenderMock.VerifyNoOtherCalls();
            var actualData = (SignalRData)signalRSenderMock.Invocations[0].Arguments[1];
            Assert.Equal("newMessage", actualData.Target);
            Assert.Equal("arg1", actualData.Arguments[0]);
            Assert.Equal("arg2", actualData.Arguments[1]);
        }

        [Fact]
        public async Task AddAsync_WithUserId_CallsSendToGroup()
        {
            var signalRSenderMock = new Mock<IAzureSignalRSender>();
            var collector = new SignalRAsyncCollector<SignalRMessage>(signalRSenderMock.Object);

            await collector.AddAsync(new SignalRMessage
            {
                GroupName = "group1",
                Target = "newMessage",
                Arguments = new object[] { "arg1", "arg2" }
            });

            signalRSenderMock.Verify(
                c => c.SendToGroup("group1", It.IsAny<SignalRData>()),
                Times.Once);
            signalRSenderMock.VerifyNoOtherCalls();
            var actualData = (SignalRData)signalRSenderMock.Invocations[0].Arguments[1];
            Assert.Equal("newMessage", actualData.Target);
            Assert.Equal("arg1", actualData.Arguments[0]);
            Assert.Equal("arg2", actualData.Arguments[1]);
        }

        [Fact]
        public async Task AddAsync_WithUserId_CallsAddUserToGroup()
        {
            var signalRSenderMock = new Mock<IAzureSignalRSender>();
            var collector = new SignalRAsyncCollector<SignalRGroupAction>(signalRSenderMock.Object);

            await collector.AddAsync(new SignalRGroupAction
            {
                UserId = "userId1",
                GroupName = "group1",
                Action = GroupAction.Add
            });

            signalRSenderMock.Verify(
                c => c.AddUserToGroup("userId1", "group1"),
                Times.Once);
            signalRSenderMock.VerifyNoOtherCalls();
            var actualData = signalRSenderMock.Invocations[0];
            Assert.Equal("userId1", actualData.Arguments[0]);
            Assert.Equal("group1", actualData.Arguments[1]);
        }

        [Fact]
        public async Task AddAsync_WithUserId_CallsRemoveUserFromGroup()
        {
            var signalRSenderMock = new Mock<IAzureSignalRSender>();
            var collector = new SignalRAsyncCollector<SignalRGroupAction>(signalRSenderMock.Object);

            await collector.AddAsync(new SignalRGroupAction
            {
                UserId = "userId1",
                GroupName = "group1",
                Action = GroupAction.Remove
            });

            signalRSenderMock.Verify(
                c => c.RemoveUserFromGroup("userId1", "group1"),
                Times.Once);
            signalRSenderMock.VerifyNoOtherCalls();
            var actualData = signalRSenderMock.Invocations[0];
            Assert.Equal("userId1", actualData.Arguments[0]);
            Assert.Equal("group1", actualData.Arguments[1]);
        }

        [Fact]
        public async Task AddAsync_WithUserId_CallsRemoveUserFromAllGroups()
        {
            var signalRSenderMock = new Mock<IAzureSignalRSender>();
            var collector = new SignalRAsyncCollector<SignalRGroupAction>(signalRSenderMock.Object);

            await collector.AddAsync(new SignalRGroupAction
            {
                UserId = "userId1",
                Action = GroupAction.RemoveAll
            });

            signalRSenderMock.Verify(
                c => c.RemoveUserFromAllGroups("userId1"),
                Times.Once);
            signalRSenderMock.VerifyNoOtherCalls();
            var actualData = signalRSenderMock.Invocations[0];
            Assert.Equal("userId1", actualData.Arguments[0]);
        }

        [Fact]
        public async Task AddAsync_InvalidTypeThrowException()
        {
            var signalRSenderMock = new Mock<IAzureSignalRSender>();
            var collector = new SignalRAsyncCollector<object[]>(signalRSenderMock.Object);

            var item = new object[] { "arg1", "arg2" };

            await Assert.ThrowsAsync<ArgumentException>(() => collector.AddAsync(item));
        }

        [Fact]
        public async Task AddAsync_SendMessage_WithBothUserIdAndGroupName_UsePriorityOrder()
        {
            var signalRSenderMock = new Mock<IAzureSignalRSender>();
            var collector = new SignalRAsyncCollector<SignalRMessage>(signalRSenderMock.Object);

            await collector.AddAsync(new SignalRMessage
            {
                UserId = "user1",
                GroupName = "group1",
                Target = "newMessage",
                Arguments = new object[] { "arg1", "arg2" }
            });

            signalRSenderMock.Verify(
                c => c.SendToUser("user1", It.IsAny<SignalRData>()),
                Times.Once);
            signalRSenderMock.VerifyNoOtherCalls();
            var actualData = (SignalRData)signalRSenderMock.Invocations[0].Arguments[1];
            Assert.Equal("newMessage", actualData.Target);
            Assert.Equal("arg1", actualData.Arguments[0]);
            Assert.Equal("arg2", actualData.Arguments[1]);
        }

        [Fact]
        public async Task AddAsync_WithConnectionId_CallsSendToUser()
        {
            var signalRSenderMock = new Mock<IAzureSignalRSender>();
            var collector = new SignalRAsyncCollector<SignalRMessage>(signalRSenderMock.Object);

            await collector.AddAsync(new SignalRMessage
            {
                ConnectionId = "connection1",
                Target = "newMessage",
                Arguments = new object[] { "arg1", "arg2" }
            });

            signalRSenderMock.Verify(
                c => c.SendToConnection("connection1", It.IsAny<SignalRData>()),
                Times.Once);
            signalRSenderMock.VerifyNoOtherCalls();
            var actualData = (SignalRData)signalRSenderMock.Invocations[0].Arguments[1];
            Assert.Equal("newMessage", actualData.Target);
            Assert.Equal("arg1", actualData.Arguments[0]);
            Assert.Equal("arg2", actualData.Arguments[1]);
        }

        [Fact]
        public async Task AddAsync_WithConnectionId_CallsAddConnectionToGroup()
        {
            var signalRSenderMock = new Mock<IAzureSignalRSender>();
            var collector = new SignalRAsyncCollector<SignalRGroupAction>(signalRSenderMock.Object);

            await collector.AddAsync(new SignalRGroupAction
            {
                ConnectionId = "connection1",
                GroupName = "group1",
                Action = GroupAction.Add
            });

            signalRSenderMock.Verify(
                c => c.AddConnectionToGroup("connection1", "group1"),
                Times.Once);
            signalRSenderMock.VerifyNoOtherCalls();
            var actualData = signalRSenderMock.Invocations[0];
            Assert.Equal("connection1", actualData.Arguments[0]);
            Assert.Equal("group1", actualData.Arguments[1]);
        }

        [Fact]
        public async Task AddAsync_WithConnectionId_CallsRemoveConnectionFromGroup()
        {
            var signalRSenderMock = new Mock<IAzureSignalRSender>();
            var collector = new SignalRAsyncCollector<SignalRGroupAction>(signalRSenderMock.Object);

            await collector.AddAsync(new SignalRGroupAction
            {
                ConnectionId = "connection1",
                GroupName = "group1",
                Action = GroupAction.Remove
            });

            signalRSenderMock.Verify(
                c => c.RemoveConnectionFromGroup("connection1", "group1"),
                Times.Once);
            signalRSenderMock.VerifyNoOtherCalls();
            var actualData = signalRSenderMock.Invocations[0];
            Assert.Equal("connection1", actualData.Arguments[0]);
            Assert.Equal("group1", actualData.Arguments[1]);
        }

        [Fact]
        public async Task AddAsync_GroupOperation_WithoutParametersThrowException()
        {
            var signalRSenderMock = new Mock<IAzureSignalRSender>();
            var collector = new SignalRAsyncCollector<SignalRGroupAction>(signalRSenderMock.Object);

            var item = new SignalRGroupAction
            {
                GroupName = "group1",
                Action = GroupAction.Add
            };

            await Assert.ThrowsAsync<ArgumentException>(() => collector.AddAsync(item));
        }
    }
}
