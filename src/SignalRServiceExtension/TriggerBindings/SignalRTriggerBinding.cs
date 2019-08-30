using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Azure.WebJobs.Host.Triggers;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal class SignalRTriggerBinding : ITriggerBinding
    {
        private const string HubNameKey = "hubName";
        private const string UserIdKey = "userId";
        private const string ReturnParameterKey = "$return";

        private readonly ParameterInfo _parameterInfo;
        private readonly SignalRTriggerAttribute _attribute;
        private readonly SignalRConfigProvider _configProvider;

        public SignalRTriggerBinding(ParameterInfo parameterInfo, SignalRTriggerAttribute attribute, SignalRConfigProvider configProvider)
        {
            _parameterInfo = parameterInfo ?? throw new ArgumentNullException(nameof(parameterInfo));
            _attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
        }

        public Task<ITriggerData> BindAsync(object value, ValueBindingContext context)
        {
            var bindingData = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            if (value is SignalRTriggerEvent triggerEvent)
            {
                var bindingContext = triggerEvent.Context;
                bindingData.Add(HubNameKey, bindingContext.HubName);
                bindingData.Add(UserIdKey, bindingContext.UserId);

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
            };
        }

        public Type TriggerValueType => typeof(SignalRTriggerEvent);

        public IReadOnlyDictionary<string, Type> BindingDataContract => CreateBindingContract();

        private IReadOnlyDictionary<string, Type> CreateBindingContract()
        {
            var contract = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
            {
                // Functions can bind to parameter name "hubName", "userId" directly
                { HubNameKey, typeof(string) },
                { UserIdKey, typeof(string) },
                { ReturnParameterKey, typeof(SignalRContext).MakeByRefType() },
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
