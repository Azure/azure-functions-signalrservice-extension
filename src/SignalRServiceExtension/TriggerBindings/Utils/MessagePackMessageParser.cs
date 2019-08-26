// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Microsoft.AspNetCore.SignalR.Protocol;
using MessagePack;
using Newtonsoft.Json;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    class MessagePackMessageParser : MessageParser
    {
        public override bool TryParseMessage(ref ReadOnlySequence<byte> buffer, out ISignalRServerlessMessage message)
        {
            var input = buffer.ToArray();
            var startOffset = 0;
            _ = MessagePackBinary.ReadArrayHeader(input, startOffset, out var readSize);
            startOffset += readSize;
            var messageType = ReadInt32(input, ref startOffset, "messageType");
            switch (messageType)
            {
                case ServerlessProtocolConstants.InvocationMessageType:
                    message = ConvertInvocationMessage(input, ref startOffset);
                    break;
                default:
                    // Future protocol changes can add message types, old clients can ignore them
                    message = null;
                    break;
            }

            return message != null;
        }

        public override IHubProtocol Protocol { get; } = new MessagePackHubProtocol();

        private static InvocationMessage ConvertInvocationMessage(byte[] input, ref int offset)
        {
            var invocationMessage = new InvocationMessage()
            {
                Type = ServerlessProtocolConstants.InvocationMessageType,
            };

            SkipHeaders(input, ref offset);
            invocationMessage.InvocationId = ReadInvocationId(input, ref offset);
            invocationMessage.Target = ReadTarget(input, ref offset);
            invocationMessage.Arguments = ReadArguments(input, ref offset);
            return invocationMessage;
        }

        private static void SkipHeaders(byte[] input, ref int offset)
        {
            var headerCount = ReadMapLength(input, ref offset, "headers");
            if (headerCount > 0)
            {
                for (var i = 0; i < headerCount; i++)
                {
                    ReadString(input, ref offset, $"headers[{i}].Key");
                    ReadString(input, ref offset, $"headers[{i}].Value");
                }
            }
        }

        private static string ReadInvocationId(byte[] input, ref int offset)
        {
            return ReadString(input, ref offset, "invocationId");
        }

        private static string ReadTarget(byte[] input, ref int offset)
        {
            return ReadString(input, ref offset, "target");
        }

        private static object[] ReadArguments(byte[] input, ref int offset)
        {
            var argumentCount = ReadArrayLength(input, ref offset, "arguments");
            var array = new object[argumentCount];
            for (int i = 0; i < argumentCount; i++)
            {
                array[i] = ReadObject(input, ref offset);
            }
            return array;
        }

        private static int ReadInt32(byte[] input, ref int offset, string field)
        {
            Exception msgPackException = null;
            try
            {
                var readInt = MessagePackBinary.ReadInt32(input, offset, out var readSize);
                offset += readSize;
                return readInt;
            }
            catch (Exception e)
            {
                msgPackException = e;
            }

            throw new InvalidDataException($"Reading '{field}' as Int32 failed.", msgPackException);
        }

        private static string ReadString(byte[] input, ref int offset, string field)
        {
            Exception msgPackException = null;
            try
            {
                var readString = MessagePackBinary.ReadString(input, offset, out var readSize);
                offset += readSize;
                return readString;
            }
            catch (Exception e)
            {
                msgPackException = e;
            }

            throw new InvalidDataException($"Reading '{field}' as String failed.", msgPackException);
        }

        private static bool ReadBoolean(byte[] input, ref int offset, string field)
        {
            Exception msgPackException = null;
            try
            {
                var readBool = MessagePackBinary.ReadBoolean(input, offset, out var readSize);
                offset += readSize;
                return readBool;
            }
            catch (Exception e)
            {
                msgPackException = e;
            }

            throw new InvalidDataException($"Reading '{field}' as Boolean failed.", msgPackException);
        }

        private static long ReadMapLength(byte[] input, ref int offset, string field)
        {
            Exception msgPackException = null;
            try
            {
                var readMap = MessagePackBinary.ReadMapHeader(input, offset, out var readSize);
                offset += readSize;
                return readMap;
            }
            catch (Exception e)
            {
                msgPackException = e;
            }

            throw new InvalidDataException($"Reading map length for '{field}' failed.", msgPackException);
        }

        private static long ReadArrayLength(byte[] input, ref int offset, string field)
        {
            Exception msgPackException = null;
            try
            {
                var readArray = MessagePackBinary.ReadArrayHeader(input, offset, out var readSize);
                offset += readSize;
                return readArray;
            }
            catch (Exception e)
            {
                msgPackException = e;
            }

            throw new InvalidDataException($"Reading array length for '{field}' failed.", msgPackException);
        }

        private static object ReadObject(byte[] input, ref int offset)
        {
            var type = MessagePackBinary.GetMessagePackType(input, offset);
            int size;
            switch (type)
            {
                case MessagePackType.Integer:
                    var intValue = MessagePackBinary.ReadInt64(input, offset, out size);
                    offset += size;
                    return intValue;
                case MessagePackType.Nil:
                    MessagePackBinary.ReadNil(input, offset, out size);
                    offset += size;
                    return null;
                case MessagePackType.Boolean:
                    var boolValue = MessagePackBinary.ReadBoolean(input, offset, out size);
                    offset += size;
                    return boolValue;
                case MessagePackType.Float:
                    var doubleValue = MessagePackBinary.ReadDouble(input, offset, out size);
                    offset += size;
                    return doubleValue;
                case MessagePackType.String:
                    var textValue = MessagePackBinary.ReadString(input, offset, out size);
                    offset += size;
                    return textValue;
                case MessagePackType.Binary:
                    var binaryValue = MessagePackBinary.ReadBytes(input, offset, out size);
                    offset += size;
                    return binaryValue;
                case MessagePackType.Array:
                    var argumentCount = ReadArrayLength(input, ref offset, "arguments");
                    var array = new object[argumentCount];
                    for (int i = 0; i < argumentCount; i++)
                    {
                        array[i] = ReadObject(input, ref offset);
                    }
                    return array;
                case MessagePackType.Map:
                    var propertyCount = MessagePackBinary.ReadMapHeader(input, offset, out size);
                    offset += size;
                    var map = new Dictionary<string, object>();
                    for (int i = 0; i < propertyCount; i++)
                    {
                        textValue = MessagePackBinary.ReadString(input, offset, out size);
                        offset += size;
                        var value = ReadObject(input, ref offset);
                        map[textValue] = value;
                    }
                    return map;
                case MessagePackType.Extension:
                case MessagePackType.Unknown:
                default:
                    return null;
            }
        }
    }
}
