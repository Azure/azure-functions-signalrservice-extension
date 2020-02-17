// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MessagePack;

namespace Microsoft.Azure.SignalR.Serverless.Protocols
{
    internal class MessagePackHelper
    {
        public static void SkipHeaders(ref MessagePackReader reader)
        {
            var headerCount = ReadMapLength(ref reader, "headers");
            if (headerCount > 0)
            {
                for (var i = 0; i < headerCount; i++)
                {
                    ReadString(ref reader, $"headers[{i}].Key");
                    ReadString(ref reader, $"headers[{i}].Value");
                }
            }
        }

        public static string ReadInvocationId(ref MessagePackReader reader)
        {
            return ReadString(ref reader, "invocationId");
        }

        public static string ReadTarget(ref MessagePackReader reader)
        {
            return ReadString(ref reader, "target");
        }

        public static object[] ReadArguments(ref MessagePackReader reader)
        {
            var argumentCount = ReadArrayLength(ref reader, "arguments");
            var array = new object[argumentCount];
            for (int i = 0; i < argumentCount; i++)
            {
                array[i] = ReadObject(ref reader);
            }
            return array;
        }

        public static object ReadObject(ref MessagePackReader reader)
        {
            var type = reader.NextMessagePackType;
            switch (type)
            {
                case MessagePackType.Integer:
                    return reader.ReadInt64();
                case MessagePackType.Nil:
                    reader.ReadNil();
                    return null;
                case MessagePackType.Boolean:
                    return reader.ReadBoolean();
                case MessagePackType.Float:
                    return reader.ReadDouble();
                case MessagePackType.String:
                    return reader.ReadString();
                case MessagePackType.Binary:
                    return reader.ReadBytes()?.ToArray() ?? Array.Empty<byte>();
                case MessagePackType.Array:
                    var argumentCount = ReadArrayLength(ref reader, "arguments");
                    var array = new object[argumentCount];
                    for (int i = 0; i < argumentCount; i++)
                    {
                        array[i] = ReadObject(ref reader);
                    }
                    return array;
                case MessagePackType.Map:
                    var propertyCount = reader.ReadMapHeader();
                    var map = new Dictionary<string, object>();
                    for (int i = 0; i < propertyCount; i++)
                    {
                        var textValue = reader.ReadString();
                        var value = ReadObject(ref reader);
                        map[textValue] = value;
                    }
                    return map;
                case MessagePackType.Extension:
                case MessagePackType.Unknown:
                default:
                    return null;
            }
        }

        public static int ReadInt32(ref MessagePackReader reader, string field)
        {
            try
            {
                return reader.ReadInt32();
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"Reading '{field}' as Int32 failed.", ex);
            }

        }

        private static string ReadString(ref MessagePackReader reader, string field)
        {
            try
            {
                return reader.ReadString();
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"Reading '{field}' as String failed.", ex);
            }
        }

        private static string[] ReadStringArray(ref MessagePackReader reader, string field)
        {
            var arrayLength = ReadArrayLength(ref reader, field);
            if (arrayLength > 0)
            {
                var array = new string[arrayLength];
                for (int i = 0; i < arrayLength; i++)
                {
                    array[i] = ReadString(ref reader, $"{field}[{i}]");
                }

                return array;
            }

            return null;
        }

        private static byte[] ReadBytes(ref MessagePackReader reader, string field)
        {
            try
            {
                return reader.ReadBytes()?.ToArray() ?? Array.Empty<byte>();
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"Reading '{field}' as Byte[] failed.", ex);
            }

        }

        private static long ReadMapLength(ref MessagePackReader reader, string field)
        {
            try
            {
                return reader.ReadMapHeader();
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"Reading map length for '{field}' failed.", ex);
            }
        }

        private static long ReadArrayLength(ref MessagePackReader reader, string field)
        {
            try
            {
                return reader.ReadArrayHeader();
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"Reading array length for '{field}' failed.", ex);
            }
        }
    }
}
