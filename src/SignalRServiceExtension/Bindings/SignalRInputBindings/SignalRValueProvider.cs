﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal class SignalRValueProvider : IValueProvider
    {
        private object value;
        private string invokeString;

        // todo: fix invoke string in another PR
        public SignalRValueProvider(object value, Type type, string invokeString)
        {
            this.value = value;
            this.invokeString = invokeString;
            this.Type = type;
        }

        public Task<object> GetValueAsync()
        {
            return Task.FromResult(value);
        }

        public string ToInvokeString()
        {
            return invokeString;
        }

        public Type Type { get; }
    }
}
