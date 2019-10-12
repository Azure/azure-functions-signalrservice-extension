using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Triggers;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal class SignalRTriggerBindingProvider : ITriggerBindingProvider
    {
        private readonly SignalRConfigProvider _configProvider;
        private readonly SignalRTriggerRouter _router;

        public SignalRTriggerBindingProvider(SignalRConfigProvider configProvider, SignalRTriggerRouter router)
        {
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
            _router = router ?? throw new ArgumentNullException(nameof(router));
        }

        public Task<ITriggerBinding> TryCreateAsync(TriggerBindingProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            
            var parameterInfo = context.Parameter;
            var attribute = parameterInfo.GetCustomAttribute<SignalRTriggerAttribute>(false);
            if (attribute == null)
            {
                return Task.FromResult<ITriggerBinding>(null);
            }

            return Task.FromResult<ITriggerBinding>(new SignalRTriggerBinding(parameterInfo, attribute, _configProvider, _router));
        }
    }
}
