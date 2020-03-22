// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.SignalRService.Internal;
using Microsoft.Azure.WebJobs.Host.Bindings;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal class SignalRNullValueProvider<T> : IValueProvider
    {
        internal static readonly SignalRNullValueProvider<T> Instance = new SignalRNullValueProvider<T>();

        public Type Type { get; }

        private SignalRNullValueProvider()
            => Type = typeof(T);

        public Task<object> GetValueAsync()
            => NullObjectTask.Result;

        public string ToInvokeString()
            => null;
    }
}