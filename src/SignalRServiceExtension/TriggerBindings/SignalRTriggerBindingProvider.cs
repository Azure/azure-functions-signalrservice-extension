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
        public SignalRTriggerBindingProvider()
        {
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

            return Task.FromResult<ITriggerBinding>(new SignalRTriggerBinding(parameterInfo, attribute));
        }
    }

    internal class SignalRTriggerBinding : ITriggerBinding
    {
        private readonly ParameterInfo _parameterInfo;
        private readonly SignalRTriggerAttribute _attribute;
        private readonly BindingDataProvider _bindingDataProvider;

        public SignalRTriggerBinding(ParameterInfo parameterInfo, SignalRTriggerAttribute attribute)
        {
            _parameterInfo = parameterInfo ?? throw new ArgumentNullException(nameof(parameterInfo));
            _attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));
            _bindingDataProvider = BindingDataProvider.FromTemplate(_attribute.HubName);
        }

        public Task<ITriggerData> BindAsync(object value, ValueBindingContext context)
        {
            var bindingData = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            var data = value as SignalRTriggerContext;
            if (data != null)
            {

            }

            return Task.FromResult<ITriggerData>(new TriggerData(bindingData));
        }

        public Task<IListener> CreateListenerAsync(ListenerFactoryContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return Task.FromResult<IListener>(new SignalRListener());
        }

        public ParameterDescriptor ToParameterDescriptor()
        {
            throw new NotImplementedException();
        }

        public Type TriggerValueType => typeof(SignalRTriggerContext);

        public IReadOnlyDictionary<string, Type> BindingDataContract => CreateBindingContract();

        private IReadOnlyDictionary<string, Type> CreateBindingContract()
        {
            var contract = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
            contract.Add("SignalRTrigger", typeof(SignalRTriggerContext));
            contract.Add("$return", typeof(JArray));

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
