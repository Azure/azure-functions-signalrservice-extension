using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.WebJobs.Description;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    [AttributeUsage(AttributeTargets.Parameter)]
    [Binding]
    public class SignalRTriggerAttribute : Attribute
    {
        public SignalRTriggerAttribute(string eventHubName, string hubName)
        {
            EventHubName = eventHubName;
            HubName = hubName;
        }

        /// <summary>
        /// Name of SignalR hub
        /// </summary>
        public string HubName { get; private set; }

        /// <summary>
        /// Name of the event hub. 
        /// </summary>
        public string EventHubName { get; private set; }

        /// <summary>
        /// Optional Name of the consumer group. If missing, then use the default name, "$Default"
        /// </summary>
        public string ConsumerGroup { get; set; }

        /// <summary>
        /// Gets or sets the optional app setting name that contains the Event Hub connection string. If missing, tries to use a registered event hub receiver.
        /// </summary>
        public string Connection { get; set; }
    }
}
