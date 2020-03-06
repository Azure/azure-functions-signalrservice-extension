// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal class SignalRTriggerBindingProvider : ITriggerBindingProvider
    {
        private readonly ISignalRTriggerDispatcher _dispatcher;
        private readonly INameResolver _nameResolver;
        private readonly SignalROptions _options;

        public SignalRTriggerBindingProvider(ISignalRTriggerDispatcher dispatcher, INameResolver nameResolver, SignalROptions options)
        {
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            _nameResolver = nameResolver ?? throw new ArgumentNullException(nameof(nameResolver));
            _options = options ?? throw new ArgumentNullException(nameof(options));
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
            var resolvedAttribute = GetParameterResolvedAttribute(attribute);
            ValidateSignalRTriggerAttributeBinding(resolvedAttribute);
            
            return Task.FromResult<ITriggerBinding>(new SignalRTriggerBinding(parameterInfo, resolvedAttribute, _dispatcher));
        }

        private SignalRTriggerAttribute GetParameterResolvedAttribute(SignalRTriggerAttribute attribute)
        {
            //TODO: AutoResolve more properties in attribute
            var resolvedConnectionString = GetResolvedConnectionString(
                typeof(SignalRTriggerAttribute).GetProperty(nameof(attribute.ConnectionStringSetting)),
                attribute.ConnectionStringSetting);

            return new SignalRTriggerAttribute(attribute.HubName, attribute.Category, attribute.Event, attribute.ParameterNames){ConnectionStringSetting = resolvedConnectionString};
        }

        private string GetResolvedConnectionString(PropertyInfo property, string configurationName)
        {
            string resolvedConnectionString;
            if (!string.IsNullOrWhiteSpace(configurationName))
            {
                resolvedConnectionString = _nameResolver.Resolve(configurationName);
            }
            else
            {
                var attribute = property.GetCustomAttribute<AppSettingAttribute>();
                if (attribute == null)
                {
                    throw new InvalidOperationException($"Unable to get AppSettingAttribute on property {property.Name}");
                }
                resolvedConnectionString = _nameResolver.Resolve(attribute.Default);
            }

            if (string.IsNullOrEmpty(resolvedConnectionString))
            {
                resolvedConnectionString = _options.ConnectionString;
            }

            return resolvedConnectionString;
        }

        private void ValidateSignalRTriggerAttributeBinding(SignalRTriggerAttribute attribute)
        {
            if (string.IsNullOrEmpty(attribute.ConnectionStringSetting))
            {
                throw new InvalidOperationException(string.Format(ErrorMessages.EmptyConnectionStringErrorMessageFormat,
                    $"{nameof(SignalRTriggerAttribute)}.{nameof(SignalRConnectionInfoAttribute.ConnectionStringSetting)}"));
            }
            ValidateParameterNames(attribute.ParameterNames);
        }

        private void ValidateParameterNames(string[] parameterNames)
        {
            if (parameterNames == null || parameterNames.Length == 0)
            {
                return;
            }

            if (parameterNames.Length != parameterNames.Distinct(StringComparer.OrdinalIgnoreCase).Count())
            {
                throw new ArgumentException("Elements in ParameterNames should be ignore case unique.");
            }
        }
    }
}
