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
        public SignalRExtensionMessage(EventData data)
        {
            data.Properties.TryGetValue(SignalRExtensionProtocolConstants.ConnectionId, out var connectionId);
            data.Properties.TryGetValue(SignalRExtensionProtocolConstants.Hub, out var hub);
            ConnectionId = connectionId as string;
            Hub = hub as string;
            Body = data.Body;
        }

        /// <summary>
        /// Gets or sets the client connection ID
        /// </summary>
        public string ConnectionId { get; set; }

        /// <summary>
        /// Gets or sets the hub name
        /// </summary>
        public string Hub { get; set; }

        /// <summary>
        /// Gets or sets the message type
        /// </summary>
        public int MessageType { get; set; }

        /// <summary>
        /// Gets or sets the body of message
        /// </summary>
        public ArraySegment<byte> Body { get; set; }
    }

    public class OpenConnectionExtensionMessage : SignalRExtensionMessage
    {
        public OpenConnectionExtensionMessage(EventData data) : base(data)
        {
            MessageType = SignalRExtensionProtocolConstants.OpenConnectionType;
        }
    }

    public class CloseConnectionExtensionMessage : SignalRExtensionMessage
    {
        public CloseConnectionExtensionMessage(EventData data) : base(data)
        {
            MessageType = SignalRExtensionProtocolConstants.CloseConnectionType;
        }
    }

    public class InvocationExtensionMessage : SignalRExtensionMessage
    {
        public InvocationExtensionMessage(EventData data) : base(data)
        {
            MessageType = SignalRExtensionProtocolConstants.InvocationType;
            var jsonBody = Encoding.UTF8.GetString(data.Body.Array);
            Target = JsonConvert.DeserializeAnonymousType(jsonBody, new {Target = ""}).Target;
        }

        public string Target { get; set; }
    }
}
