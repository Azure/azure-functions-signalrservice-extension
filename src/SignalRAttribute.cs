using System;
using Microsoft.Azure.WebJobs.Description;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    [AttributeUsage(AttributeTargets.ReturnValue | AttributeTargets.Parameter)]
    [Binding]
    public class SignalRAttribute : Attribute
    {

        [AppSetting(Default = "AzureSignalRConnectionString")]
        public string ConnectionStringSetting { get; set; }
        [AutoResolve]
        public string HubName { get; set; }
    }
}