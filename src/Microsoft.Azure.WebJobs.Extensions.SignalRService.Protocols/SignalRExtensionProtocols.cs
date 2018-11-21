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
            if (!input.Properties.TryGetValue(SignalRExtensionProtocolConstants.ProtocolVersion, out var protocolVersion) ||
                (int)protocolVersion != Version)
            {
                message = null;
                return false;
            }

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

        public EventData BuildMessage(int messageType, string hub, string connectionId, byte[] body)
        {
            var data = new EventData(body);
            data.Properties.Add(SignalRExtensionProtocolConstants.ConnectionId, connectionId);
            data.Properties.Add(SignalRExtensionProtocolConstants.MessageType, messageType);
            data.Properties.Add(SignalRExtensionProtocolConstants.Hub, hub);
            data.Properties.Add(SignalRExtensionProtocolConstants.ProtocolVersion, Version);

            return data;
        }
    }
}
