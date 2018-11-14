using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.WebJobs.Description;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    [AttributeUsage(AttributeTargets.Parameter)]
    [Binding]
    public sealed class SignalRCloseConnectionTriggerAttribute : SignalRTriggerAttribute
    {
        public SignalRCloseConnectionTriggerAttribute(string eventHubName) : base(eventHubName)
        {
        }
    }
}
