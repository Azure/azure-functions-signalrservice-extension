using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.WebJobs.Description;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService.Trigger
{
    [AttributeUsage(AttributeTargets.Parameter)]
    [Binding]
    public sealed class SignalROpenConnectionTriggerAttribute : SignalRTriggerAttribute
    {
        public SignalROpenConnectionTriggerAttribute(string eventHubName, string hubName) : base(eventHubName, hubName)
        {

        }
    }
}
