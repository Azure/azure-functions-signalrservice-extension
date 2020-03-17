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
using Microsoft.Extensions.Logging;

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
            var resolvedAttribute = GetParameterResolvedAttribute(attribute, parameterInfo);
            ValidateSignalRTriggerAttributeBinding(resolvedAttribute);
            
            return Task.FromResult<ITriggerBinding>(new SignalRTriggerBinding(parameterInfo, resolvedAttribute, _dispatcher));
        }

        internal SignalRTriggerAttribute GetParameterResolvedAttribute(SignalRTriggerAttribute attribute, ParameterInfo parameterInfo)
        {
            //TODO: AutoResolve more properties in attribute
            var hubName = attribute.HubName;
            var category = attribute.Category;
            var @event = attribute.Event;
            var parameterNames = attribute.ParameterNames ?? Array.Empty<string>();

            // We have two models for C#, one is function based model which also work in multiple language
            // Another one is class based model, which is highly close to SignalR itself but must keep some conventions.
            var method = (MethodInfo)parameterInfo.Member;
            var declaredType = method.DeclaringType;
            string[] parameterNamesFromAttribute;

            if (declaredType != null && declaredType.IsSubclassOf(typeof(ServerlessHub)))
            {
                // Class based model
                if (!string.IsNullOrEmpty(hubName) ||
                    !string.IsNullOrEmpty(category) ||
                    !string.IsNullOrEmpty(@event) ||
                    parameterNames.Length != 0)
                {
                    throw new ArgumentException($"{nameof(SignalRTriggerAttribute)} must use No-Arg Constructor in class based model.");
                }
                parameterNamesFromAttribute = method.GetParameters().Where(IsLegalClassBasedParameter).Select(p => p.Name).ToArray();
                hubName = declaredType.Name;
                category = GetCategoryFromMethodName(method.Name);
                @event = GetEventFromMethodName(method.Name, category);
            }
            else
            {
                parameterNamesFromAttribute = method.GetParameters().
                    Where(p => p.GetCustomAttribute<SignalRParameterAttribute>(false) != null).
                    Select(p => p.Name).ToArray();

                if (parameterNamesFromAttribute.Length != 0 && parameterNames.Length != 0)
                {
                    throw new InvalidOperationException(
                        $"{nameof(SignalRTriggerAttribute)}.{nameof(SignalRTriggerAttribute.ParameterNames)} and {nameof(SignalRParameterAttribute)} can not be set in the same Function.");
                }
            }

            parameterNames = parameterNamesFromAttribute.Length != 0
                ? parameterNamesFromAttribute
                : parameterNames;

            var resolvedConnectionString = GetResolvedConnectionString(
                typeof(SignalRTriggerAttribute).GetProperty(nameof(attribute.ConnectionStringSetting)),
                attribute.ConnectionStringSetting);

            return new SignalRTriggerAttribute(hubName, category, @event, parameterNames) {ConnectionStringSetting = resolvedConnectionString};
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

            return string.IsNullOrEmpty(resolvedConnectionString)
                ? _options.ConnectionString
                : resolvedConnectionString;
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

        private string GetCategoryFromMethodName(string name)
        {
            if (string.Equals(name, Constants.OnConnected, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, Constants.OnDisconnected, StringComparison.OrdinalIgnoreCase))
            {
                return Category.Connections;
            }

            return Category.Messages;
        }

        private string GetEventFromMethodName(string name, string category)
        {
            if (category == Category.Connections)
            {
                if (string.Equals(name, Constants.OnConnected, StringComparison.OrdinalIgnoreCase))
                {
                    return Event.Connected;
                }
                if (string.Equals(name, Constants.OnDisconnected, StringComparison.OrdinalIgnoreCase))
                {
                    return Event.Disconnected;
                }
            }

            return name;
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

        private bool IsLegalClassBasedParameter(ParameterInfo parameter)
        {
            // In class based model, we treat all the parameters as a legal parameter except the cases below
            // 1. Parameter decorated by [SignalRIgnore]
            // 2. Parameter decorated Attribute that has BindingAttribute
            // 3. Two special type ILogger and CancellationToken

            if (parameter.ParameterType.IsAssignableFrom(typeof(ILogger)) ||
                parameter.ParameterType.IsAssignableFrom(typeof(CancellationToken)))
            {
                return false;
            }
            if (parameter.GetCustomAttribute<SignalRIgnoreAttribute>() != null)
            {
                return false;
            }
            if (HasBindingAttribute(parameter.GetCustomAttributes()))
            {
                return false;
            }

            return true;
        }

        private bool HasBindingAttribute(IEnumerable<Attribute> attributes)
        {
            return attributes.Any(attribute => attribute.GetType().GetCustomAttribute<BindingAttribute>(false) != null);
        }
    }
}
