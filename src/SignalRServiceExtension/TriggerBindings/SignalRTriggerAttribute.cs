using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.WebJobs.Description;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    [AttributeUsage(AttributeTargets.ReturnValue | AttributeTargets.Parameter)]
    [Binding]
    public class SignalRTriggerAttribute : Attribute
    {
        [AutoResolve]
        public string HubName { get; set; }

        [AutoResolve]
        public string Target { get; set; }
    }
}
