// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace SignalRServiceExtension.Tests
{
    public class SignalRAsyncCollectorExtensionsTests
    {
        [Fact]
        public async Task ClientsAll_CallsSendToAll()
        {
            var signalRSenderMock = new Mock<IAzureSignalRSender>();
            var collector = new SignalRAsyncCollector<SignalRMessage>(signalRSenderMock.Object, "chathub");

            await collector.GetHubClients().All.SendCoreAsync("newMessage", new[] { "arg1", "arg2" });

            signalRSenderMock.Verify(c => c.SendToAll("chathub", It.IsAny<SignalRData>()), Times.Once);
            signalRSenderMock.VerifyNoOtherCalls();
            var actualData = (SignalRData)signalRSenderMock.Invocations[0].Arguments[1];
            Assert.Equal("newMessage", actualData.Target);
            Assert.Equal("arg1", actualData.Arguments[0]);
            Assert.Equal("arg2", actualData.Arguments[1]);
        }

        [Fact]
        public async Task ClientsUser_CallsSendToUser()
        {
            var signalRSenderMock = new Mock<IAzureSignalRSender>();
            var collector = new SignalRAsyncCollector<SignalRMessage>(signalRSenderMock.Object, "chathub");

            await collector.GetHubClients().User("userId1").SendCoreAsync("newMessage", new[] { "arg1", "arg2" });

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
        public async Task ClientsUsers_CallsSendToUserForEachUser()
        {
            var signalRSenderMock = new Mock<IAzureSignalRSender>();
            var collector = new SignalRAsyncCollector<SignalRMessage>(signalRSenderMock.Object, "chathub");

            await collector.GetHubClients().Users(new[] { "userId1", "userId2" }).SendCoreAsync("newMessage", new[] { "arg1", "arg2" });

            signalRSenderMock.Verify(
                c => c.SendToUser("chathub", "userId1", It.IsAny<SignalRData>()),
                Times.Once);
            signalRSenderMock.Verify(
                c => c.SendToUser("chathub", "userId2", It.IsAny<SignalRData>()),
                Times.Once);
            signalRSenderMock.VerifyNoOtherCalls();
            var actualData1 = (SignalRData)signalRSenderMock.Invocations[0].Arguments[2];
            Assert.Equal("newMessage", actualData1.Target);
            Assert.Equal("arg1", actualData1.Arguments[0]);
            Assert.Equal("arg2", actualData1.Arguments[1]);
            var actualData2 = (SignalRData)signalRSenderMock.Invocations[1].Arguments[2];
            Assert.Equal("newMessage", actualData2.Target);
            Assert.Equal("arg1", actualData2.Arguments[0]);
            Assert.Equal("arg2", actualData2.Arguments[1]);
        }

        [Fact]
        public async Task ClientsGroups_CallsSendToGroup()
        {
            var signalRSenderMock = new Mock<IAzureSignalRSender>();
            var collector = new SignalRAsyncCollector<SignalRMessage>(signalRSenderMock.Object, "chathub");

            await collector.GetHubClients().Group("group1").SendCoreAsync("newMessage", new[] { "arg1", "arg2" });

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
        public async Task ClientsGroup_CallsSendToGroupForEachGroup()
        {
            var signalRSenderMock = new Mock<IAzureSignalRSender>();
            var collector = new SignalRAsyncCollector<SignalRMessage>(signalRSenderMock.Object, "chathub");

            await collector.GetHubClients().Groups(new[] { "group1", "group2" }).SendCoreAsync("newMessage", new[] { "arg1", "arg2" });

            signalRSenderMock.Verify(
                c => c.SendToGroup("chathub", "group1", It.IsAny<SignalRData>()),
                Times.Once);
            signalRSenderMock.Verify(
                c => c.SendToGroup("chathub", "group2", It.IsAny<SignalRData>()),
                Times.Once);
            signalRSenderMock.VerifyNoOtherCalls();
            var actualData1 = (SignalRData)signalRSenderMock.Invocations[0].Arguments[2];
            Assert.Equal("newMessage", actualData1.Target);
            Assert.Equal("arg1", actualData1.Arguments[0]);
            Assert.Equal("arg2", actualData1.Arguments[1]);
            var actualData2 = (SignalRData)signalRSenderMock.Invocations[1].Arguments[2];
            Assert.Equal("newMessage", actualData2.Target);
            Assert.Equal("arg1", actualData2.Arguments[0]);
            Assert.Equal("arg2", actualData2.Arguments[1]);
        }

        [Fact]
        public async Task AddToGroupAsync_CallsAddUserToGroup()
        {
            var signalRSenderMock = new Mock<IAzureSignalRSender>();
            var collector = new SignalRAsyncCollector<SignalRGroupAction>(signalRSenderMock.Object, "chathub");

            await collector.GetHubGroups().AddToGroupAsync("userId1", "group1");

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
        public async Task RemoveFromGroupAsync_CallsRemoveUserFromGroup()
        {
            var signalRSenderMock = new Mock<IAzureSignalRSender>();
            var collector = new SignalRAsyncCollector<SignalRGroupAction>(signalRSenderMock.Object, "chathub");

            await collector.GetHubGroups().RemoveFromGroupAsync("userId1", "group1");

            signalRSenderMock.Verify(
                c => c.RemoveUserFromGroup("chathub", "userId1", "group1"),
                Times.Once);
            signalRSenderMock.VerifyNoOtherCalls();
            var actualData = signalRSenderMock.Invocations[0];
            Assert.Equal("chathub", actualData.Arguments[0]);
            Assert.Equal("userId1", actualData.Arguments[1]);
            Assert.Equal("group1", actualData.Arguments[2]);
        }

        [Fact]
        public void NotImplementedClientsMethodsThrowNotImplementedExceptions()
        {
            var collector = new SignalRAsyncCollector<SignalRMessage>(Mock.Of<IAzureSignalRSender>(), "chathub");
            var clients = collector.GetHubClients();

            // A gentle reminder to add tests here for these methods if and when they're added
            Assert.Throws<NotImplementedException>(() => clients.Client(""));
            Assert.Throws<NotImplementedException>(() => clients.Clients(new[] { "" }));
            Assert.Throws<NotImplementedException>(() => clients.AllExcept(new[] { "" }));
            Assert.Throws<NotImplementedException>(() => clients.GroupExcept("", new[] { "" }));
        }
    }
}