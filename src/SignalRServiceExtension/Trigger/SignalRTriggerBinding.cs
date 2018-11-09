using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
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
        private readonly string _hubName;
        private readonly ILogger _logger;


        public SignalRTriggerBinding(ParameterInfo parameter, EventProcessorHost host, SignalROptions options, string hubName, ILogger logger)
        {
            _parameter = parameter;
            _host = host;
            _options = options;
            _hubName = hubName;
            _logger = logger;
        }

        public Type TriggerValueType => typeof(EventData);

        public IReadOnlyDictionary<string, Type> BindingDataContract => CreateBindingDataContract();

        public Task<ITriggerData> BindAsync(object value, ValueBindingContext context)
        {
            EventData eventData = value as EventData;

            IValueProvider valueProvider = new ValueProvider(new SignalRBaseMessage(){Hub = "A", Method = "B"}, typeof(SignalRBaseMessage));

            IReadOnlyDictionary<string, object> bindingData = CreateBindingData();
            ITriggerData result = new TriggerData(valueProvider, bindingData);
            return Task.FromResult(result);
        }

        private IReadOnlyDictionary<string, object> CreateBindingData()
        {
            var bindingData = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            bindingData.Add("Method", "B");
            bindingData.Add("ConnectionId", "123456");

            return bindingData;
        }

        public Task<IListener> CreateListenerAsync(ListenerFactoryContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            IListener listener =  new SignalRTriggerListener(context.Executor, _host, _options, _logger);
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
            contract.Add("Method", typeof(string));
            //contract.Add("Arguments", typeof(Array));
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
