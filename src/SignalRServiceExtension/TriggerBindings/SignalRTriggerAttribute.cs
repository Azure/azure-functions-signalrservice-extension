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
        // TODO: Not been used now, but if we need to setup websocket in future, we need it.
        [AppSetting(Default = SignalRConfigProvider.AzureSignalRConnectionStringName)]
        public string ConnectionStringSetting { get; set; }

        [AutoResolve]
        public string HubName { get; set; }
    }
}
