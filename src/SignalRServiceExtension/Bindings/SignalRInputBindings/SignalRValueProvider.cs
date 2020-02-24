// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal class SignalRValueProvider : IValueProvider
    {
        private object value;

        public SignalRValueProvider(object value)
        {
            this.value = value;
        }

        public Task<object> GetValueAsync()
        {
            return Task.FromResult(value);
        }

        public string ToInvokeString()
        {
            return value?.ToString();
        }

        public Type Type { get; }
    }
}
