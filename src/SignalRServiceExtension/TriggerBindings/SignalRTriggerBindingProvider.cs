using Microsoft.Azure.WebJobs.Host.Triggers;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal class SignalRTriggerBindingProvider : ITriggerBindingProvider
    {
        private readonly SignalRConfigProvider _configProvider;

        public SignalRTriggerBindingProvider(SignalRConfigProvider configProvider)
        {
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
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

            return Task.FromResult<ITriggerBinding>(new SignalRTriggerBinding(parameterInfo, attribute, _configProvider));
        }
    }

    internal class SignalRTriggerBinding : ITriggerBinding
    {
        private readonly ParameterInfo _parameterInfo;
        private readonly SignalRTriggerAttribute _attribute;
        private readonly BindingDataProvider _bindingDataProvider;
        private readonly SignalRConfigProvider _configProvider;

        public SignalRTriggerBinding(ParameterInfo parameterInfo, SignalRTriggerAttribute attribute, SignalRConfigProvider configProvider)
        {
            _parameterInfo = parameterInfo ?? throw new ArgumentNullException(nameof(parameterInfo));
            _attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
            _bindingDataProvider = BindingDataProvider.FromTemplate(_attribute.HubName);
        }

        public Task<ITriggerData> BindAsync(object value, ValueBindingContext context)
        {
            var bindingData = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            if (value is SignalRTriggerContext data)
            {
                bindingData.Add("SignalRTrigger", data);

                var bindingDataFromHubName = _bindingDataProvider.GetBindingData(data.HubName);
                if (bindingDataFromHubName != null)
                {
                    foreach (var item in bindingDataFromHubName)
                    {
                        bindingData[item.Key] = item.Value;
                    }
                }
            }

            return Task.FromResult<ITriggerData>(new TriggerData(bindingData));
        }

        public Task<IListener> CreateListenerAsync(ListenerFactoryContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return Task.FromResult<IListener>(new SignalRListener(context.Executor, _configProvider, _attribute.HubName));
        }

        public ParameterDescriptor ToParameterDescriptor()
        {
            return new ParameterDescriptor
            {
                Name = _parameterInfo.Name,
                DisplayHints = new ParameterDisplayHints
                {
                    Prompt = "Enter a hub name."
                }
            };
        }

        public Type TriggerValueType => typeof(SignalRTriggerContext);

        public IReadOnlyDictionary<string, Type> BindingDataContract => CreateBindingContract();

        private IReadOnlyDictionary<string, Type> CreateBindingContract()
        {
            var contract = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
            contract.Add("SignalRTrigger", typeof(SignalRTriggerContext));
            //contract.Add("$return", typeof(JArray));

            if (_bindingDataProvider.Contract != null)
            {
                foreach (var item in _bindingDataProvider.Contract)
                {
                    // In case of conflict, binding data from the value type overrides the built-in binding data above.
                    contract[item.Key] = item.Value;
                }
            }

            return contract;
        }
    }
}
