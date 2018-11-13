using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using Microsoft.Azure.EventHubs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService.Protocols
{
    /// <summary>
    /// A base class of message between Azure SignalR Service and SignalR Webjob Extension
    /// </summary>
    public abstract class SignalRExtensionMessage
    {
        /// <summary>
        /// Gets or sets the client connection ID
        /// </summary>
        public string ConnectionId { get; set; }

        /// <summary>
        /// Gets or sets the hub name
        /// </summary>
        public string Hub { get; set; }

        public int MessageType { get; set; }

        /// <summary>
        /// Gets or sets the body of message
        /// </summary>
        public ArraySegment<byte> Body { get; set; }
    }

    public class OpenConnectionExtensionMessage : SignalRExtensionMessage
    {
        public OpenConnectionExtensionMessage(EventData data)
        {
            data.Properties.TryGetValue(SignalRExtensionProtocolConstants.ConnectionId, out var connectionId);
            data.Properties.TryGetValue(SignalRExtensionProtocolConstants.Hub, out var hub);
            ConnectionId = connectionId as string;
            Hub = hub as string;
            Body = data.Body;
            MessageType = SignalRExtensionProtocolConstants.OpenConnectionType;
        }
    }

    public class CloseConnectionExtensionMessage : SignalRExtensionMessage
    {
        public CloseConnectionExtensionMessage(EventData data)
        {
            data.Properties.TryGetValue(SignalRExtensionProtocolConstants.ConnectionId, out var connectionId);
            data.Properties.TryGetValue(SignalRExtensionProtocolConstants.Hub, out var hub);
            ConnectionId = connectionId as string;
            Hub = hub as string;
            Body = data.Body;
            MessageType = SignalRExtensionProtocolConstants.CloseConnectionType;
        }
    }

    public class InvocationExtensionMessage : SignalRExtensionMessage
    {
        public InvocationExtensionMessage(EventData data)
        {
            data.Properties.TryGetValue(SignalRExtensionProtocolConstants.ConnectionId, out var connectionId);
            data.Properties.TryGetValue(SignalRExtensionProtocolConstants.Hub, out var hub);
            ConnectionId = connectionId as string;
            Hub = hub as string;
            MessageType = SignalRExtensionProtocolConstants.InvocationType;
            Body = data.Body;
            var jsonBody = Encoding.UTF8.GetString(data.Body.Array);
            var obj1 = JsonConvert.DeserializeObject(jsonBody, typeof(JsonObject));
            var obj = obj1 as JsonObject;
            //var internalObject = JsonConvert.DeserializeObject(jsonBody, typeof(InvocationExtensionMessage)) as InvocationExtensionMessage;
            Target = obj.Target;
            Arguments = obj.Arguments;
        }

        internal class JsonObject
        {
            public string Target { get; set; }
            public object[] Arguments { get; set; }
        }

        public string Target { get; set; }
        public object[] Arguments { get; set; }
    }
}
