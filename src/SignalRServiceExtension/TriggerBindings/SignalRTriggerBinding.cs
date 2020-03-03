// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal class SignalRTriggerBinding : ITriggerBinding
    {
        private const string ReturnParameterKey = "$return";

        private readonly ParameterInfo _parameterInfo;
        private readonly SignalRTriggerAttribute _attribute;
        private readonly ISignalRTriggerDispatcher _dispatcher;

        public SignalRTriggerBinding(ParameterInfo parameterInfo, SignalRTriggerAttribute attribute, ISignalRTriggerDispatcher dispatcher)
        {
            _parameterInfo = parameterInfo ?? throw new ArgumentNullException(nameof(parameterInfo));
            _attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            BindingDataContract = CreateBindingContract(_attribute, _parameterInfo);
        }

        public Task<ITriggerData> BindAsync(object value, ValueBindingContext context)
        {
            var bindingData = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            if (value is SignalRTriggerEvent triggerEvent)
            {
                var bindingContext = triggerEvent.Context;

                // If ParameterNames are set, bind them in order.
                // To reduce undefined situation, number of arguments should keep consist with that of ParameterNames
                if (_attribute.ParameterNames != null && _attribute.ParameterNames.Length != 0)
                {
                    if (bindingContext.Arguments == null ||
                        bindingContext.Arguments.Length != _attribute.ParameterNames.Length)
                    {
                        throw new SignalRTriggerParametersNotMatchException(_attribute.ParameterNames.Length, bindingContext.Arguments?.Length ?? 0);
                    }

                    var length = _attribute.ParameterNames.Length;
                    for (var i = 0; i < length; i++)
                    {
                        bindingData.Add(_attribute.ParameterNames[i], bindingContext.Arguments[i]);
                    }
                }

                return Task.FromResult<ITriggerData>(new TriggerData(new SignalRTriggerValueProvider(_parameterInfo, bindingContext), bindingData)
                {
                    ReturnValueProvider = triggerEvent.TaskCompletionSource == null ? null : new TriggerReturnValueProvider(triggerEvent.TaskCompletionSource),
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

            // It's not a real listener, and it doesn't need a start or close.
            _dispatcher.Map((_attribute.HubName, _attribute.Category, _attribute.Event),
                new ExecutionContext{Executor = context.Executor, AccessKey = SignalRTriggerUtils.GetAccessKey(_attribute.ConnectionStringSetting)});

            return Task.FromResult<IListener>(new NullListener());
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

        // TODO: Use dynamic contract to deal with parameterName
        public IReadOnlyDictionary<string, Type> BindingDataContract { get; }

        /// <summary>
        /// Defined what other bindings can use and return value.
        /// </summary>
        private IReadOnlyDictionary<string, Type> CreateBindingContract(SignalRTriggerAttribute attribute, ParameterInfo parameter)
        {
            var contract = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
            {
                { ReturnParameterKey, typeof(object).MakeByRefType() },
            };

            // Add names in ParameterNames to binding contract, that user can bind to Functions' parameter directly
            if (attribute.ParameterNames != null)
            {
                var parameters = ((MethodInfo)parameter.Member).GetParameters().ToDictionary(p => p.Name, p => p.ParameterType, StringComparer.OrdinalIgnoreCase);
                foreach (var parameterName in attribute.ParameterNames)
                {
                    if (parameters.ContainsKey(parameterName))
                    {
                        contract.Add(parameterName, parameters[parameterName]);
                    }
                    else
                    {
                        contract.Add(parameterName, typeof(object));
                    }
                }
            }
            
            return contract;
        }

        // TODO: Add more supported type
        /// <summary>
        /// A provider that responsible for providing value in various type to be bond to function method parameter.
        /// </summary>
        private class SignalRTriggerValueProvider : IValueBinder
        {
            private readonly InvocationContext _value;
            private readonly ParameterInfo _parameter;

            public SignalRTriggerValueProvider(ParameterInfo parameter, InvocationContext value)
            {
                _parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
                _value = value ?? throw new ArgumentNullException(nameof(value));
            }

            public Task<object> GetValueAsync()
            {
                if (_parameter.ParameterType == typeof(InvocationContext))
                {
                    return Task.FromResult<object>(_value);
                }
                else if (_parameter.ParameterType == typeof(object))
                {
                    return Task.FromResult<object>(JObject.FromObject(_value));
                }

                return Task.FromResult<object>(null);
            }

            public string ToInvokeString()
            {
                return _value.ToString();
            }

            public Type Type => _parameter.GetType();

            // No use here
            public Task SetValueAsync(object value, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// A provider to handle return value.
        /// </summary>
        private class TriggerReturnValueProvider : IValueBinder
        {
            private readonly TaskCompletionSource<object> _tcs;

            public TriggerReturnValueProvider(TaskCompletionSource<object> tcs)
            {
                _tcs = tcs;
            }

            public Task<object> GetValueAsync()
            {
                // Useless for return value provider
                return null;
            }

            public string ToInvokeString()
            {
                // Useless for return value provider
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
