using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService.Trigger
{
    public sealed class SignalROpenConnectionTriggerAttribute : SignalRTriggerAttribute
    {
        public SignalROpenConnectionTriggerAttribute(string eventHubName, string hubName) : base(eventHubName, hubName)
        {

        }
    }
}
