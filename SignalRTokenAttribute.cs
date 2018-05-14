using System;
using Microsoft.Azure.WebJobs.Description;

namespace SignalRExtension
{
    [AttributeUsage(AttributeTargets.ReturnValue | AttributeTargets.Parameter)]
    [Binding]
    public class SignalRTokenAttribute : Attribute
    {
        [AppSetting(Default = "AzureSignalRConnectionString")]
        public string ConnectionString { get; set; }
        [AutoResolve]
        public string HubName { get; set; }
    }
}
