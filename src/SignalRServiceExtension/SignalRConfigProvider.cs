using System;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Azure.WebJobs.Logging;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    public class SignalRConfigProvider : IExtensionConfigProvider
    {
        private readonly ILogger logger;

        public SignalRConfigProvider(ILoggerFactory loggerFactory)
        {
            logger = loggerFactory.CreateLogger("SignalR");
        }
        
        public void Initialize(ExtensionConfigContext context)
        {
            context.AddConverter<string, JObject>(JObject.FromObject);
            context.AddConverter<JObject, SignalRMessage>(input => input.ToObject<SignalRMessage>());
            context.AddConverter<SignalRConnectionInfo, JObject>(JObject.FromObject);

            context.AddBindingRule<SignalRConnectionInfoAttribute>()
                .BindToInput<SignalRConnectionInfo>(GetConnectionInfo);
            context.AddBindingRule<SignalRAttribute>()
                .BindToCollector<SignalRMessage>(attr => new SignalRMessageAsyncCollector(attr));

            logger.LogInformation("SignalRService binding initialized");
        }

        private SignalRConnectionInfo GetConnectionInfo(SignalRConnectionInfoAttribute attribute)
        {
            var signalR = new AzureSignalRClient(attribute.ConnectionStringSetting);
            return signalR.GetClientConnectionInfo(attribute.HubName);
        }

    }
}