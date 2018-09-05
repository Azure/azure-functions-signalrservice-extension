// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Xunit;

namespace SignalRServiceExtension.Tests
{
    public class SignalRMessageAsyncCollectorTests
    {
        [Fact]
        public async Task AddAsync_CallsAzureSignalRClient()
        {
            var client = new FakeAzureSignalRClient();
            var collector = new SignalRMessageAsyncCollector(client, "foo");

            await collector.AddAsync(new SignalRMessage
            {
                Target = "newMessage",
                Arguments = new object[] { "arg1", "arg2" }
            });

            var actualSendMessageParams = client.SendMessageParams;
            Assert.Equal("foo", actualSendMessageParams.hubName);
            Assert.Equal("newMessage", actualSendMessageParams.message.Target);
            Assert.Equal("arg1", actualSendMessageParams.message.Arguments[0]);
            Assert.Equal("arg2", actualSendMessageParams.message.Arguments[1]);
        }

        private class FakeAzureSignalRClient : IAzureSignalRClient
        {
            public (string hubName, SignalRMessage message) SendMessageParams { get; private set; }
            
            public Task SendMessage(string hubName, SignalRMessage message)
            {
                SendMessageParams = (hubName, message);
                return Task.CompletedTask;
            }
        }
    }
}