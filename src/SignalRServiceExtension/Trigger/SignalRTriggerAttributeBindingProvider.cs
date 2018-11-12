using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs.EventHubs;
using Microsoft.Azure.WebJobs.Extensions.SignalRService.Trigger;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Azure.WebJobs.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    class SignalRTriggerAttributeBindingProvider : ITriggerBindingProvider
    {
        private readonly INameResolver _nameResolver;
        private readonly ILogger _logger;
        private readonly IConfiguration _config;
        private readonly SignalROptions _options;
        private readonly IConverterManager _converterManager;

        public SignalRTriggerAttributeBindingProvider(
            IConfiguration configuration,
            INameResolver nameResolver,
            IConverterManager converterManager,
            SignalROptions options,
            ILogger logger)
        {
            _config = configuration;
            _nameResolver = nameResolver;
            _converterManager = converterManager;
            _options = options;
            _logger = logger;
        }

        public Task<ITriggerBinding> TryCreateAsync(TriggerBindingProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            ParameterInfo parameter = context.Parameter;
            SignalRTriggerAttribute attribute = parameter.GetCustomAttribute<SignalRTriggerAttribute>(false);

            if (attribute == null)
            {
                return Task.FromResult<ITriggerBinding>(null);
            }

            string resolvedEventHubName = _nameResolver.ResolveWholeString(attribute.EventHubName);
            string consumerGroup = attribute.ConsumerGroup ?? PartitionReceiver.DefaultConsumerGroupName;
            string resolvedConsumerGroup = _nameResolver.ResolveWholeString(consumerGroup);
            string resolvedHubName = _nameResolver.ResolveWholeString(attribute.HubName);

            if (string.IsNullOrWhiteSpace(attribute.Connection))
            {
                return Task.FromResult<ITriggerBinding>(null);
            }

            var connectionString = _config.GetConnectionStringOrSetting(attribute.Connection);
            var eventHostListener =
                _options.GetEventProcessorHost(_config, resolvedEventHubName, connectionString, resolvedConsumerGroup);
            return Task.FromResult<ITriggerBinding>(new SignalRTriggerBinding(parameter, eventHostListener, attribute.GetType(), _options, resolvedHubName, _logger));
        }
    }
}
