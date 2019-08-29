using Microsoft.Azure.WebJobs.Host.Triggers;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Newtonsoft.Json.Linq;
using Microsoft.Azure.WebJobs.Extensions.SignalRService.TriggerBindings;

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

            if (value is SignalRTriggerEvent triggerEvent)
            {
                var bindingContext = triggerEvent.Context;
                bindingData.Add("SignalRTrigger", bindingContext);

                return Task.FromResult<ITriggerData>(new TriggerData(new SignalRTriggerValueProvider(bindingContext), bindingData)
                {
                    ReturnValueProvider = new NegotiateResponseProvider(triggerEvent.ContextTcs)
                });
            }

            return Task.FromResult<ITriggerData>(null);
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

        public Type TriggerValueType => typeof(SignalRContext);

        public IReadOnlyDictionary<string, Type> BindingDataContract => CreateBindingContract();

        private IReadOnlyDictionary<string, Type> CreateBindingContract()
        {
            var contract = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
            {
                {"SignalRTrigger", typeof(SignalRContext)},
                { "$return", typeof(SignalRContext).MakeByRefType()}
            };

            return contract;
        }

        private class NegotiateResponseProvider : IValueBinder
        {
            private TaskCompletionSource<SignalRContext> _contextTcs;

            public NegotiateResponseProvider(TaskCompletionSource<SignalRContext> contextTcs)
            {
                _contextTcs = contextTcs;
            }

            public Task<object> GetValueAsync()
            {
                return null;
            }

            public string ToInvokeString()
            {
                return "SignalR Trigger";
            }

            public Type Type => typeof(SignalRContext).MakeByRefType();

            public Task SetValueAsync(object value, CancellationToken cancellationToken)
            {
                _contextTcs.TrySetResult(value as SignalRContext);
                return Task.CompletedTask;
            }
        }

        private class SignalRTriggerValueProvider : IValueBinder
        {
            private object _value;

            public SignalRTriggerValueProvider(object value)
            {
                _value = value;
            }

            public Task<object> GetValueAsync()
            {
                return Task.FromResult(_value);
            }

            public string ToInvokeString()
            {
                return _value.ToString();
            }

            public Type Type => typeof(SignalRContext);

            public Task SetValueAsync(object value, CancellationToken cancellationToken)
            {
                _value = value;
                return Task.CompletedTask;
            }
        }
    }

    
}
