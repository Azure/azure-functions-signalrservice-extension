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
        [AppSetting(Default = SignalRConfigProvider.AzureSignalRConnectionStringName)]
        public string ConnectionStringSetting { get; set; }

        [AutoResolve]
        public string HubName { get; set; }

        public string[] ClaimTypeList { get; set; }
    }
}
