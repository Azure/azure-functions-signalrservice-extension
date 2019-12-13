using Microsoft.AspNetCore.SignalR.Protocol;
using Newtonsoft.Json.Linq;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal class JsonMessageParser : MessageParser
    {
        private const string TypePropertyName = "type";

        public override bool TryParseMessage(ref ReadOnlySequence<byte> buffer, out ISignalRServerlessMessage message)
        {
            if (TextMessageParser.TryParseMessage(ref buffer, out var payload))
            {
                var jsonString = Encoding.UTF8.GetString(payload.ToArray());
                var jObject = JObject.Parse(jsonString);
                if (jObject.TryGetValue(TypePropertyName, out var token))
                {
                    var type = token.Value<int>();
                    if (type == HubProtocolConstants.InvocationMessageType)
                    {
                        message = ParseInvocationMessage(jObject);
                        return message != null;
                    }
                    //TODO: openconnection / closeconnection
                    message = null;
                    return false;
                }
            }
            message = null;
            return false;
        }

        private InvocationMessage ParseInvocationMessage(JObject jObject)
        {
            try
            {
                return jObject.ToObject<InvocationMessage>();
            }
            catch
            {
                return null;
            }
        }
    }
}
