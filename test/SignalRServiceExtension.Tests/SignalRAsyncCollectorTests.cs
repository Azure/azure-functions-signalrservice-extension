// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Moq;
using Xunit;

namespace SignalRServiceExtension.Tests
{
    public class SignalRAsyncCollectorTests
    {
        [Fact]
        public async Task AddAsync_WithBroadcastMessage_CallsSendToAll()
        {
            var signalRSenderMock = new Mock<IAzureSignalRSender>();
            var collector = new SignalRAsyncCollector<SignalRMessage>(signalRSenderMock.Object, "chathub");

            await collector.AddAsync(new SignalRMessage
            {
                Target = "newMessage",
                Arguments = new object[] { "arg1", "arg2" }
            });

            signalRSenderMock.Verify(c => c.SendToAll("chathub", It.IsAny<SignalRData>()), Times.Once);
            signalRSenderMock.VerifyNoOtherCalls();
            var actualData = (SignalRData)signalRSenderMock.Invocations[0].Arguments[1];
            Assert.Equal("newMessage", actualData.Target);
            Assert.Equal("arg1", actualData.Arguments[0]);
            Assert.Equal("arg2", actualData.Arguments[1]);
        }

        [Fact]
        public async Task AddAsync_WithUserId_CallsSendToUser()
        {
            var signalRSenderMock = new Mock<IAzureSignalRSender>();
            var collector = new SignalRAsyncCollector<SignalRMessage>(signalRSenderMock.Object, "chathub");

            await collector.AddAsync(new SignalRMessage
            {
                UserId = "userId1",
                Target = "newMessage",
                Arguments = new object[] { "arg1", "arg2" }
            });

            signalRSenderMock.Verify(
                c => c.SendToUser("chathub", "userId1", It.IsAny<SignalRData>()),
                Times.Once);
            signalRSenderMock.VerifyNoOtherCalls();
            var actualData = (SignalRData)signalRSenderMock.Invocations[0].Arguments[2];
            Assert.Equal("newMessage", actualData.Target);
            Assert.Equal("arg1", actualData.Arguments[0]);
            Assert.Equal("arg2", actualData.Arguments[1]);
        }

        [Fact]
        public async Task AddAsync_WithUserId_CallsSendToGroup()
        {
            var signalRSenderMock = new Mock<IAzureSignalRSender>();
            var collector = new SignalRAsyncCollector<SignalRMessage>(signalRSenderMock.Object, "chathub");

            await collector.AddAsync(new SignalRMessage
            {
                GroupName = "group1",
                Target = "newMessage",
                Arguments = new object[] { "arg1", "arg2" }
            });

            signalRSenderMock.Verify(
                c => c.SendToGroup("chathub", "group1", It.IsAny<SignalRData>()),
                Times.Once);
            signalRSenderMock.VerifyNoOtherCalls();
            var actualData = (SignalRData)signalRSenderMock.Invocations[0].Arguments[2];
            Assert.Equal("newMessage", actualData.Target);
            Assert.Equal("arg1", actualData.Arguments[0]);
            Assert.Equal("arg2", actualData.Arguments[1]);
        }

        [Fact]
        public async Task AddAsync_WithUserId_CallsAddUserToGroup()
        {
            var signalRSenderMock = new Mock<IAzureSignalRSender>();
            var collector = new SignalRAsyncCollector<SignalRGroupAction>(signalRSenderMock.Object, "chathub");

            await collector.AddAsync(new SignalRGroupAction
            {
                UserId = "userId1",
                GroupName = "group1",
                Action = GroupAction.Add
            });

            signalRSenderMock.Verify(
                c => c.AddUserToGroup("chathub", "userId1", "group1"),
                Times.Once);
            signalRSenderMock.VerifyNoOtherCalls();
            var actualData = signalRSenderMock.Invocations[0];
            Assert.Equal("chathub", actualData.Arguments[0]);
            Assert.Equal("userId1", actualData.Arguments[1]);
            Assert.Equal("group1", actualData.Arguments[2]);
        }

        [Fact]
        public async Task AddAsync_WithUserId_CallsRemoveUserFromGroup()
        {
            var signalRSenderMock = new Mock<IAzureSignalRSender>();
            var collector = new SignalRAsyncCollector<SignalRGroupAction>(signalRSenderMock.Object, "chathub");

            await collector.AddAsync(new SignalRGroupAction
            {
                UserId = "userId1",
                GroupName = "group1",
                Action = GroupAction.Remove
            });

            signalRSenderMock.Verify(
                c => c.RemoveUserFromGroup("chathub", "userId1", "group1"),
                Times.Once);
            signalRSenderMock.VerifyNoOtherCalls();
            var actualData = signalRSenderMock.Invocations[0];
            Assert.Equal("chathub", actualData.Arguments[0]);
            Assert.Equal("userId1", actualData.Arguments[1]);
            Assert.Equal("group1", actualData.Arguments[2]);
        }
    }
}