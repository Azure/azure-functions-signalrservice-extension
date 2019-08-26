// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

using Microsoft.AspNetCore.SignalR.Protocol;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal class JsonMessageParser : MessageParser
    {
        private const string TypePropertyName = "type";

        public override bool TryParseMessage(ref ReadOnlySequence<byte> buffer, out ISignalRServerlessMessage message)
        {
            var jsonString = Encoding.UTF8.GetString(buffer.ToArray());
            var jObject = JObject.Parse(jsonString);
            if (jObject.TryGetValue(TypePropertyName, out var token))
            {
                var type = token.Value<int>();
                switch (type)
                {
                    case ServerlessProtocolConstants.InvocationMessageType:
                        message = SafeParseMessage<InvocationMessage>(jObject);
                        break;
                    case ServerlessProtocolConstants.OpenConnectionMessageType:
                        message = SafeParseMessage<OpenConnectionMessage>(jObject);
                        break;
                    case ServerlessProtocolConstants.CloseConnectionMessageType:
                        message = SafeParseMessage<CloseConnectionMessage>(jObject);
                        break;
                    default:
                        message = null;
                        break;
                }
                return message != null;
            }
            message = null;
            return false;
        }

        public override IHubProtocol Protocol { get; } = new JsonHubProtocol();

        private ISignalRServerlessMessage SafeParseMessage<T>(JObject jObject) where T : ISignalRServerlessMessage
        {
            try
            {
                return jObject.ToObject<T>();
            }
            catch
            {
                return null;
            }
        }
    }
}
