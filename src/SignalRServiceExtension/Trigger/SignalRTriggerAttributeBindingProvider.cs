using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
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
        private readonly IOptions<SignalROptions> _options;
        private readonly IConverterManager _converterManager;

        public SignalRTriggerAttributeBindingProvider(
            IConfiguration configuration,
            INameResolver nameResolver,
            IConverterManager converterManager,
            IOptions<SignalROptions> options,
            ILoggerFactory loggerFactory)
        {
            _config = configuration;
            nameResolver = _nameResolver;
            converterManager = _converterManager;
            _options = options;
            _logger = loggerFactory?.CreateLogger(LogCategories.CreateTriggerCategory("SignalR"));
        }

        public async Task<ITriggerBinding> TryCreateAsync(TriggerBindingProviderContext context)
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





        }
    }
}
