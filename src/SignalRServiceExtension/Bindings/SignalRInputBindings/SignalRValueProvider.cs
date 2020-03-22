// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal class SignalRValueProvider : IValueProvider
    {
        private readonly object value;

        private SignalRValueProvider(object value, Type valueType)
        {
            this.value = value;

            Type = valueType;
        }

        internal static IValueProvider Create<T>(T value) where T : class
            => value == null
                ? (IValueProvider) SignalRNullValueProvider<T>.Instance
                : new SignalRValueProvider(value, typeof(T));

        public Type Type { get; }

        public Task<object> GetValueAsync()
            => Task.FromResult(value);

        public string ToInvokeString()
            => value.ToString();
    }
}