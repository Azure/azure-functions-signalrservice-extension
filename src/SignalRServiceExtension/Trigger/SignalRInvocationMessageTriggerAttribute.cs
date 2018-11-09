using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService.Trigger
{
    public sealed class SignalRInvocationMessageTriggerAttribute : SignalRTriggerAttribute
    {
        public SignalRInvocationMessageTriggerAttribute(string eventHubName, string hubName) : base(eventHubName,
            hubName)
        {
        }

        /// <summary>
        /// Optional name of target method. If missing, triggered by every target method
        /// </summary>
        public string MethodName { get; set; }
    }
}
