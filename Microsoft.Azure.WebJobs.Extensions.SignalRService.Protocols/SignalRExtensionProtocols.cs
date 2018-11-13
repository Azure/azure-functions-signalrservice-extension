using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.EventHubs;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService.Protocols
{
    public class SignalRExtensionProtocols : ISignalRExtensionProtocols
    {
        public int Version => 1;

        public bool TryParseMessage(EventData input, out SignalRExtensionMessage message)
        {
            if (!input.Properties.TryGetValue(SignalRExtensionProtocolConstants.MessageType, out var messageType))
            {
                message = null;
                return false;
            }

            switch (messageType)
            {
                case SignalRExtensionProtocolConstants.CloseConnectionType:
                    message = new CloseConnectionExtensionMessage(input);
                    return true;
                case SignalRExtensionProtocolConstants.OpenConnectionType:
                    message = new OpenConnectionExtensionMessage(input);
                    return true;
                case SignalRExtensionProtocolConstants.InvocationType:
                    message = new InvocationExtensionMessage(input);
                    return true;
                default:
                    message = null;
                    return false;
            }
            
        }

        public EventData WriteMessage()
        {
            JObject o = JObject.FromObject(new
            {
                Target = "TestMethod",
                Arguments = new[]
                {
                    "Args1",
                    "Args2"
                }
            });
            
            var date = new EventData(Encoding.UTF8.GetBytes(o.ToString()));
            date.Properties.Add(SignalRExtensionProtocolConstants.ConnectionId, "ConnectionID");
            date.Properties.Add(SignalRExtensionProtocolConstants.MessageType, SignalRExtensionProtocolConstants.InvocationType);
            date.Properties.Add(SignalRExtensionProtocolConstants.Hub, "Hub");

            return date;
        }
    }
}
