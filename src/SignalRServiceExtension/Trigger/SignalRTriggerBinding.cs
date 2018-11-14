using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using Microsoft.Azure.WebJobs.Extensions.SignalRService.Protocols;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    class SignalRTriggerBinding : ITriggerBinding
    {
        private readonly SignalROptions _options;
        private readonly ParameterInfo _parameter;
        private readonly EventProcessorHost _host;
        private readonly Type _attributeType;
        private readonly string _hubName;
        private readonly ILogger _logger;
        private readonly string _target;


        public SignalRTriggerBinding(ParameterInfo parameter, EventProcessorHost host, Type attributeType, SignalROptions options, string hubName, ILogger logger, string target = null)
        {
            _parameter = parameter;
            _host = host;
            _attributeType = attributeType;
            _options = options;
            _hubName = hubName;
            _logger = logger;
            _target = target;
        }

        public Type TriggerValueType => typeof(SignalRExtensionMessage);

        public IReadOnlyDictionary<string, Type> BindingDataContract => CreateBindingDataContract();

        public Task<ITriggerData> BindAsync(object value, ValueBindingContext context)
        {
            SignalRExtensionMessage message = value as SignalRExtensionMessage;

            IReadOnlyDictionary<string, object> bindingData = CreateBindingData(message);
            ITriggerData result = new TriggerData(bindingData);
            return Task.FromResult(result);
        }

        private IReadOnlyDictionary<string, object> CreateBindingData(SignalRExtensionMessage message)
        {
            var bindingData = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            bindingData.Add("ConnectionId", message.ConnectionId);
            bindingData.Add("Hub", message.Hub);

            return bindingData;
        }

        public Task<IListener> CreateListenerAsync(ListenerFactoryContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            IListener listener =
                SignalRTriggerSingletonListenerFactory.Instance.CreateListener(_host, context, _attributeType, _hubName, _options,
                    _logger, _target);
            return Task.FromResult(listener);
        }

        public ParameterDescriptor ToParameterDescriptor()
        {
            return new ParameterDescriptor
            {
                Name = _parameter.Name,
                DisplayHints = new ParameterDisplayHints
                {
                    Description = "message"
                }
            };
        }

        private static IReadOnlyDictionary<string, Type> CreateBindingDataContract()
        {
            Dictionary<string, Type> contract = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
            contract.Add("Hub", typeof(string));
            contract.Add("ConnectionId", typeof(string));

            return contract;
        }

        private class ValueProvider : IValueProvider
        {
            private readonly object _value;
            private readonly Type _type;

            public ValueProvider(object value, Type type)
            {
                _value = value;
                _type = type;
            }

            public Type Type
            {
                get { return _type; }
            }

            public Task<object> GetValueAsync()
            {
                return Task.FromResult(_value);
            }

            public string ToInvokeString()
            {
                return String.Empty;
            }
        }

    }
}
