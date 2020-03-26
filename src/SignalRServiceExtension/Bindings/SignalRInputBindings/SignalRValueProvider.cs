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
        private readonly string invokeString;

        public Type Type { get; }

        private SignalRValueProvider(object value, Type type, string invokeString)
        {
            this.value = value;
            Type = type;
            this.invokeString = invokeString;
        }

        internal static IValueProvider Create<T>(object value, string invokeString)
        {
            return new SignalRValueProvider(value, typeof(T), invokeString);
        }

        public Task<object> GetValueAsync()
            => Task.FromResult(value);

        public string ToInvokeString()
            => invokeString;
    }
}