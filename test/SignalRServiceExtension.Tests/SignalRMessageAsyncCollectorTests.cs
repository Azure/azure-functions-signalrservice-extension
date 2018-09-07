// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
            clientMock
                .Setup(c => c.SendToAll(It.Is<string>(val => val == "foo"), It.IsAny<SignalRData>()))
                .Returns(() => Task.CompletedTask);
            var collector = new SignalRMessageAsyncCollector(clientMock.Object, "foo");

            await collector.AddAsync(new SignalRMessage
            {
                Target = "newMessage",
                Arguments = new object[] { "arg1", "arg2" }
            });

            Assert.Equal(1, clientMock.Invocations.Count);
            var actualData = (SignalRData)clientMock.Invocations[0].Arguments[1];
            Assert.Equal("newMessage", actualData.Target);
            Assert.Equal("arg1", actualData.Arguments[0]);
            Assert.Equal("arg2", actualData.Arguments[1]);
        }
    }
}