// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal class SignalRValueProvider : IValueProvider
    {
        private readonly Task<object> value;

        public Type Type { get; }

        private SignalRValueProvider(object value, Type valueType)
        {
            this.value = Task.FromResult(value);

            Type = valueType;
        }

        internal static IValueProvider Create<T>(T value) where T : class
            => value == null
                ? NullOf<T>()
                : new SignalRValueProvider(value, value.GetType());

        internal static IValueProvider NullOf<T>() where T : class
            => SignalRNullValueProvider<T>.Instance;

        public Task<object> GetValueAsync()
            => value;

        public string ToInvokeString()
            => value.ToString();

        private class SignalRNullValueProvider<T> : IValueProvider
        {
            private readonly Task<object> nullObjectTask = Task.FromResult<object>(null);

            internal static readonly SignalRNullValueProvider<T> Instance = new SignalRNullValueProvider<T>();

            public Type Type { get; }

            private SignalRNullValueProvider()
                => Type = typeof(T);

            public Task<object> GetValueAsync()
                => nullObjectTask;

            public string ToInvokeString()
                => null;
        }
    }
}