// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Moq;
using Xunit;

namespace SignalRServiceExtension.Tests
{
    public class SignalRMessageAsyncCollectorTests
    {
        [Fact]
        public async Task AddAsync_WithBroadcastMessage_CallsSendToAll()
        {
            var signalRSenderMock = new Mock<IAzureSignalRSender>();
            var collector = new SignalRMessageAsyncCollector(signalRSenderMock.Object, "chathub");

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
            var collector = new SignalRMessageAsyncCollector(signalRSenderMock.Object, "chathub");

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
    }
}