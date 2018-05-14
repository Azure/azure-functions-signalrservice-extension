using System;
using Microsoft.Azure.WebJobs.Host.Config;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    public class SignalRConfiguration : IExtensionConfigProvider
    {
        public void Initialize(ExtensionConfigContext context)
        {
            context.AddConverter<string, JObject>(JObject.FromObject);
            context.AddConverter<JObject, SignalRMessage>(input => input.ToObject<SignalRMessage>());
            context.AddConverter<AzureSignalREndpoint, JObject>(JObject.FromObject);

            context.AddBindingRule<SignalRTokenAttribute>()
                .BindToInput<AzureSignalREndpoint>(BuildEndpoint);
            context.AddBindingRule<SignalRAttribute>()
                .BindToCollector<SignalRMessage>(attr => new SignalRMessageAsyncCollector(this, attr));
        }

        private AzureSignalREndpoint BuildEndpoint(SignalRTokenAttribute attribute)
        {
            var signalR = new AzureSignalR(attribute.ConnectionString);
            return signalR.GetClientEndpoint(attribute.HubName);
        }

    }
}