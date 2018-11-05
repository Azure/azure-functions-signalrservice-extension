using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.WebJobs.Description;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    [AttributeUsage(AttributeTargets.Parameter)]
    [Binding]
    class SignalRTriggerAttribute : Attribute
    {
        public SignalRTriggerAttribute(string )
    }
}
