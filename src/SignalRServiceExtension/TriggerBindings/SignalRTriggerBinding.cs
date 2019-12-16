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
        private const string ReturnParameterKey = "$return";

        private readonly ParameterInfo _parameterInfo;
        private readonly SignalRTriggerAttribute _attribute;
        private readonly SignalRTriggerRouter _router;

        public SignalRTriggerBinding(ParameterInfo parameterInfo, SignalRTriggerAttribute attribute, SignalRTriggerRouter router)
        {
            _parameterInfo = parameterInfo ?? throw new ArgumentNullException(nameof(parameterInfo));
            _attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));
            _router = router ?? throw new ArgumentNullException(nameof(router));
        }

        public Task<ITriggerData> BindAsync(object value, ValueBindingContext context)
        {
            var bindingData = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            if (value is SignalRTriggerEvent triggerEvent)
            {
                var bindingContext = triggerEvent.Context;
                bindingData.Add(HubNameKey, bindingContext.Context.HubName);
                
                return Task.FromResult<ITriggerData>(new TriggerData(new SignalRTriggerValueProvider(bindingContext), bindingData)
                {
                    ReturnValueProvider = new TriggerReturnValueProvider(triggerEvent.TaskCompletionSource),
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
            
            var functionNameAttribute = _parameterInfo.Member.GetCustomAttribute<FunctionNameAttribute>(false);
            var methodName = functionNameAttribute.Name;

            return Task.FromResult<IListener>(new SignalRListener(context.Executor, _router, _attribute.HubName, methodName));
        }

        public ParameterDescriptor ToParameterDescriptor()
        {
            return new ParameterDescriptor
            {
                Name = _parameterInfo.Name,
            };
        }

        /// <summary>
        /// Type of object in BindAsync
        /// </summary>
        public Type TriggerValueType => typeof(SignalRTriggerEvent);

        public IReadOnlyDictionary<string, Type> BindingDataContract => CreateBindingContract();

        /// <summary>
        /// Defined what other bindings can use and return value.
        /// </summary>
        private IReadOnlyDictionary<string, Type> CreateBindingContract()
        {
            var contract = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
            {
                // Functions can bind to parameter name "hubName" directly
                { HubNameKey, typeof(string) },
                { ReturnParameterKey, typeof(object).MakeByRefType() },
            };

            return contract;
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

            public Type Type => typeof(InvocationContext);

            public Task SetValueAsync(object value, CancellationToken cancellationToken)
            {
                _value = value;
                return Task.CompletedTask;
            }
        }

        private class TriggerReturnValueProvider : IValueBinder
        {
            private TaskCompletionSource<object> _tcs;

            public TriggerReturnValueProvider(TaskCompletionSource<object> tcs)
            {
                _tcs = tcs;
            }

            public Task<object> GetValueAsync()
            {
                // Unnecessary for return value provider
                return null;
            }

            public string ToInvokeString()
            {
                // Unnecessary for return value provider
                return string.Empty;
            }

            public Type Type => typeof(object).MakeByRefType();

            public Task SetValueAsync(object value, CancellationToken cancellationToken)
            {
                _tcs.TrySetResult(value);
                return Task.CompletedTask;
            }
        }
    }
}
