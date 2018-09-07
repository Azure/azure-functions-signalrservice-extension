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
            var clientMock = new Mock<IAzureSignalRSender>();
            var collector = new SignalRMessageAsyncCollector(clientMock.Object, "foo");

            await collector.AddAsync(new SignalRMessage
            {
                Target = "newMessage",
                Arguments = new object[] { "arg1", "arg2" }
            });
            
            clientMock.Verify(c => c.SendToAll("foo", It.IsAny<SignalRData>()), Times.Once);
            clientMock.VerifyNoOtherCalls();
            var actualData = (SignalRData)clientMock.Invocations[0].Arguments[1];
            Assert.Equal("newMessage", actualData.Target);
            Assert.Equal("arg1", actualData.Arguments[0]);
            Assert.Equal("arg2", actualData.Arguments[1]);
        }

        [Fact]
        public async Task AddAsync_WithUserIds_CallsSendToUsers()
        {
            var clientMock = new Mock<IAzureSignalRSender>();
            var collector = new SignalRMessageAsyncCollector(clientMock.Object, "foo");

            await collector.AddAsync(new SignalRMessage
            {
                UserIds = new [] { "userId1", "userId2" },
                Target = "newMessage",
                Arguments = new object[] { "arg1", "arg2" }
            });

            clientMock.Verify(
                c => c.SendToUsers("foo", It.IsAny<IEnumerable<string>>(), It.IsAny<SignalRData>()),
                Times.Once);
            clientMock.VerifyNoOtherCalls();
            var actualUserIds = (IEnumerable<string>)clientMock.Invocations[0].Arguments[1];
            Assert.Equal(new [] { "userId1", "userId2" }, actualUserIds);
            var actualData = (SignalRData)clientMock.Invocations[0].Arguments[2];
            Assert.Equal("newMessage", actualData.Target);
            Assert.Equal("arg1", actualData.Arguments[0]);
            Assert.Equal("arg2", actualData.Arguments[1]);
        }
    }
}