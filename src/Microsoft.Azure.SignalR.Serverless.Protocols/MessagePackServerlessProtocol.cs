// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;

using MessagePack;

namespace Microsoft.Azure.SignalR.Serverless.Protocols
{
    public class MessagePackServerlessProtocol : IServerlessProtocol
    {
        public int Version => 1;

        public bool TryParseMessage(ref ReadOnlySequence<byte> input, out ServerlessMessage message)
        {
            var reader = new MessagePackReader(input);
            _ = reader.ReadArrayHeader();
            var messageType = MessagePackHelper.ReadInt32(ref reader, "messageType");
            switch (messageType)
            {
                case ServerlessProtocolConstants.InvocationMessageType:
                    message = ConvertInvocationMessage(ref reader);
                    break;
                default:
                    // TODO:OpenConnectionMessage and CloseConnectionMessage only will be sent in JSON format. It can be added later.
                    message = null;
                    break;
            }

            return message != null;
        }

        private static InvocationMessage ConvertInvocationMessage(ref MessagePackReader reader)
        {
            var invocationMessage = new InvocationMessage()
            {
                Type = ServerlessProtocolConstants.InvocationMessageType,
            };

            MessagePackHelper.SkipHeaders(ref reader);
            invocationMessage.InvocationId = MessagePackHelper.ReadInvocationId(ref reader);
            invocationMessage.Target = MessagePackHelper.ReadTarget(ref reader);
            invocationMessage.Arguments = MessagePackHelper.ReadArguments(ref reader);
            return invocationMessage;
        }
    }
}
