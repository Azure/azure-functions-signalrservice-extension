using System;
using System.Buffers;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR.Protocol;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Azure.SignalR.Serverless.Protocols.Tests
{
    public class MessagePackParseTests
    {
        public static IEnumerable<object[]> GetParameters()
        {
            yield return new object[] {null, Guid.NewGuid().ToString(), new object[0]};
            yield return new object[] {Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), new object[0]};
            yield return new object[]
            {
                Guid.NewGuid().ToString(), Guid.NewGuid().ToString(),
                new object[] {Guid.NewGuid().ToString(), Guid.NewGuid().ToString()}
            };
            yield return new object[]
            {
                Guid.NewGuid().ToString(), Guid.NewGuid().ToString(),
                new object[] {new object[] {Guid.NewGuid().ToString()}, Guid.NewGuid().ToString()}
            };
            yield return new object[]
            {
                Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), new object[] { new Dictionary<string, string>
                {
                    [Guid.NewGuid().ToString()] = Guid.NewGuid().ToString(),
                    [Guid.NewGuid().ToString()] = Guid.NewGuid().ToString(),
                }}
            };
            yield return new object[]
            {
                Guid.NewGuid().ToString(), Guid.NewGuid().ToString(),
                new object[] {new object[] { null, Guid.NewGuid().ToString() }}
            };
        }

        [Theory]
        [MemberData(nameof(GetParameters))]
        public void MessagePackParseTest(string invocationId, string target, object[] arguments)
        {
            var message = new AspNetCore.SignalR.Protocol.InvocationMessage(invocationId, target, arguments);
            var protocol = new MessagePackHubProtocol();
            var bytes = new ReadOnlySequence<byte>(protocol.GetMessageBytes(message));
            BinaryMessageParser.TryParseMessage(ref bytes, out var payload);

            var serverlessProtocol = new MessagePackServerlessProtocol();
            Assert.True(serverlessProtocol.TryParseMessage(ref payload, out var parsedMessage));
            var invocationMessage = (InvocationMessage) parsedMessage;
            Assert.Equal(1, invocationMessage.Type);
            Assert.Equal(invocationId, invocationMessage.InvocationId);
            Assert.Equal(target, invocationMessage.Target);
            var expected = JsonConvert.SerializeObject(arguments);
            var actual = JsonConvert.SerializeObject(invocationMessage.Arguments);
            Assert.Equal(expected, actual);
        }
    }
}
