using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

using MessagePack;
using MessagePack.Formatters;
using Microsoft.AspNetCore.SignalR.Protocol;
using Newtonsoft.Json;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    public class MessagePackToJsonConverter
    {
        private const string ResultPropertyName = "result";
        private const string ItemPropertyName = "item";
        private const string InvocationIdPropertyName = "invocationId";
        private const string TypePropertyName = "type";
        private const string ErrorPropertyName = "error";
        private const string TargetPropertyName = "target";
        private const string ArgumentsPropertyName = "arguments";
        private const string HeadersPropertyName = "headers";

        public static (bool success, int type) Convert(ArraySegment<byte> message, JsonWriter writer, Dictionary<string, object> properties)
        {
            return ConvertMessage(message.Array, message.Offset, writer, properties);
        }

        private static (bool success, int type) ConvertMessage(byte[] input, int startOffset, JsonWriter writer, Dictionary<string, object> properties)
        {
            _ = MessagePackBinary.ReadArrayHeader(input, startOffset, out var readSize);
            startOffset += readSize;
            var messageType = ReadInt32(input, ref startOffset, "messageType");
            switch (messageType)
            {
                case HubProtocolConstants.InvocationMessageType:
                    ConvertInvocationMessage(input, ref startOffset, writer, properties);
                    break;
                case HubProtocolConstants.CloseMessageType:
                //return CreateCloseMessage(input, ref startOffset);
                case HubProtocolConstants.PingMessageType:
                // do not convert ping message.
                case HubProtocolConstants.StreamInvocationMessageType:
                case HubProtocolConstants.StreamItemMessageType:
                case HubProtocolConstants.CompletionMessageType:
                case HubProtocolConstants.CancelInvocationMessageType:
                default:
                    // Future protocol changes can add message types, old clients can ignore them
                    return (false, messageType);
            }
            return (true, messageType);
        }

        private static void ConvertInvocationMessage(byte[] input, ref int offset, JsonWriter writer, Dictionary<string, object> properties)
        {

            writer.WriteStartObject();
            foreach (var property in properties)
            {
                writer.WritePropertyName(property.Key);
                writer.WriteValue(property.Value);
            }
            writer.WritePropertyName(TypePropertyName);
            //writer.WriteValue(SignalRExtensionProtocolConstants.InvocationType);
            ConvertHeaders(input, ref offset, writer);
            ConvertInvocationId(input, ref offset, writer);
            ConvertTarget(input, ref offset, writer);
            ConvertArguments(input, ref offset, writer);
            writer.WriteEndObject();
        }

        private static void WritePingMessage(JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName(TypePropertyName);
            writer.WriteValue(HubProtocolConstants.InvocationMessageType);
            writer.WriteEndObject();
        }

        private static void ConvertHeaders(byte[] input, ref int offset, JsonWriter writer)
        {
            var headerCount = ReadMapLength(input, ref offset, "headers");
            if (headerCount > 0)
            {
                writer.WritePropertyName(HeadersPropertyName);
                writer.WriteStartObject();

                for (var i = 0; i < headerCount; i++)
                {
                    var key = ReadString(input, ref offset, $"headers[{i}].Key");
                    writer.WritePropertyName(key);
                    var value = ReadString(input, ref offset, $"headers[{i}].Value");
                    writer.WriteValue(value);
                }
            }
        }

        private static void ConvertInvocationId(byte[] input, ref int offset, JsonWriter writer)
        {
            var invocationId = ReadString(input, ref offset, "invocationId");

            // For MsgPack, we represent an empty invocation ID as an empty string,
            // so we need to normalize that to "null", which is what indicates a non-blocking invocation.
            if (!string.IsNullOrEmpty(invocationId))
            {
                writer.WritePropertyName(InvocationIdPropertyName);
                writer.WriteValue(invocationId);
            }
        }

        private static void ConvertTarget(byte[] input, ref int offset, JsonWriter writer)
        {
            var target = ReadString(input, ref offset, "target");
            writer.WritePropertyName(TargetPropertyName);
            writer.WriteValue(target);
        }

        private static void ConvertArguments(byte[] input, ref int offset, JsonWriter writer)
        {
            var argumentCount = ReadArrayLength(input, ref offset, "arguments");
            writer.WritePropertyName(ArgumentsPropertyName);
            writer.WriteStartArray();
            for (int i = 0; i < argumentCount; i++)
            {
                ConvertObject(input, ref offset, writer);
            }
            writer.WriteEndArray();
        }

        private static void ConvertObject(byte[] input, ref int offset, JsonWriter writer)
        {
            var type = MessagePackBinary.GetMessagePackType(input, offset);
            int size;
            switch (type)
            {
                case MessagePackType.Integer:
                    var intValue = MessagePackBinary.ReadInt64(input, offset, out size);
                    offset += size;
                    writer.WriteValue(intValue);
                    break;
                case MessagePackType.Nil:
                    MessagePackBinary.ReadNil(input, offset, out size);
                    offset += size;
                    writer.WriteNull();
                    break;
                case MessagePackType.Boolean:
                    var boolValue = MessagePackBinary.ReadBoolean(input, offset, out size);
                    offset += size;
                    writer.WriteValue(boolValue);
                    break;
                case MessagePackType.Float:
                    var doubleValue = MessagePackBinary.ReadDouble(input, offset, out size);
                    offset += size;
                    writer.WriteValue(doubleValue);
                    break;
                case MessagePackType.String:
                    var textValue = MessagePackBinary.ReadString(input, offset, out size);
                    offset += size;
                    writer.WriteValue(textValue);
                    break;
                case MessagePackType.Binary:
                    var binaryValue = MessagePackBinary.ReadBytes(input, offset, out size);
                    offset += size;
                    writer.WriteValue(System.Convert.ToBase64String(binaryValue));
                    break;
                case MessagePackType.Array:
                    var argumentCount = ReadArrayLength(input, ref offset, "arguments");
                    writer.WriteStartArray();
                    for (int i = 0; i < argumentCount; i++)
                    {
                        ConvertObject(input, ref offset, writer);
                    }
                    writer.WriteEndArray();
                    break;
                case MessagePackType.Map:
                    var propertyCount = MessagePackBinary.ReadMapHeader(input, offset, out size);
                    offset += size;
                    writer.WriteStartObject();
                    for (int i = 0; i < propertyCount; i++)
                    {
                        textValue = MessagePackBinary.ReadString(input, offset, out size);
                        offset += size;
                        writer.WritePropertyName(textValue);
                        ConvertObject(input, ref offset, writer);
                    }
                    writer.WriteEndObject();
                    break;
                case MessagePackType.Extension:
                case MessagePackType.Unknown:
                default:
                    break;
            }
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


        internal static List<IFormatterResolver> CreateDefaultFormatterResolvers()
        {
            // Copy to allow users to add/remove resolvers without changing the static SignalRResolver list
            return new List<IFormatterResolver>(SignalRResolver.Resolvers);
        }

        internal class SignalRResolver : IFormatterResolver
        {
            public static readonly IFormatterResolver Instance = new SignalRResolver();

            public static readonly IList<IFormatterResolver> Resolvers = new[]
            {
                MessagePack.Resolvers.DynamicEnumAsStringResolver.Instance,
                MessagePack.Resolvers.ContractlessStandardResolver.Instance,
            };

            public IMessagePackFormatter<T> GetFormatter<T>()
            {
                return Cache<T>.Formatter;
            }

            private static class Cache<T>
            {
                public static readonly IMessagePackFormatter<T> Formatter;

                static Cache()
                {
                    foreach (var resolver in Resolvers)
                    {
                        Formatter = resolver.GetFormatter<T>();
                        if (Formatter != null)
                        {
                            return;
                        }
                    }
                }
            }
        }

        // Support for users making their own Formatter lists
        internal class CombinedResolvers : IFormatterResolver
        {
            private readonly IList<IFormatterResolver> _resolvers;

            public CombinedResolvers(IList<IFormatterResolver> resolvers)
            {
                _resolvers = resolvers;
            }

            public IMessagePackFormatter<T> GetFormatter<T>()
            {
                foreach (var resolver in _resolvers)
                {
                    var formatter = resolver.GetFormatter<T>();
                    if (formatter != null)
                    {
                        return formatter;
                    }
                }

                return null;
            }
        }
    }
}
