// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal abstract class MessageParser
    {
        public static readonly MessageParser Json = new JsonMessageParser();
        public static readonly MessageParser MessagePack = new MessagePackMessageParser();

        public static MessageParser GetParser(string protocol)
        {
            switch (protocol)
            {
                case Constants.JsonContentType:
                    return Json;
                case Constants.MessagePackContentType:
                    return MessagePack;
                default:
                    return null;
            }
        }

        public abstract bool TryParseMessage(ref ReadOnlySequence<byte> buffer, out ISignalRServerlessMessage message);

        public abstract IHubProtocol Protocol { get; }
    }
}
