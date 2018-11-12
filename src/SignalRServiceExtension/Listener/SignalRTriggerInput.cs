using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using Microsoft.Azure.EventHubs;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    public class SignalRTriggerInput
    {
        public EventData Data { get; set; }

        public string MessageType { get; set; }

        public string ConnectionId { get; set; }

        public static SignalRTriggerInput Parse(EventData rawData)
        {
            return new SignalRTriggerInput(){MessageType = "InvocationMessage"};
        }
    }
}
