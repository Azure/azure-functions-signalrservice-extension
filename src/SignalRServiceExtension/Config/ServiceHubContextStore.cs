// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.SignalR;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal class ServiceHubContextStore : IInternalServiceHubContextStore
    {
        private readonly ConcurrentDictionary<string, (Lazy<Task<IServiceHubContext>> lazy, IServiceHubContext value)> _weakTypedHubStore = new(StringComparer.OrdinalIgnoreCase);
        private readonly IServiceEndpointManager endpointManager;
        
        private readonly IServiceProvider _strongTypedHubServiceProvider;

        public IServiceManager ServiceManager { get; }

        public AccessKey[] AccessKeys => endpointManager.Endpoints.Keys.Select(endpoint => endpoint.AccessKey).ToArray();

        public ServiceHubContextStore(IServiceEndpointManager endpointManager, IServiceManager serviceManager)
        {
            this.endpointManager = endpointManager;
            ServiceManager = serviceManager;
            _strongTypedHubServiceProvider = new ServiceCollection()
                .AddSingleton(serviceManager as ServiceManager)
                .AddSingleton(typeof(ServerlessHubContext<,>))
                .BuildServiceProvider();
        }

        public ValueTask<IServiceHubContext> GetAsync(string hubName)
        {
            var pair = _weakTypedHubStore.GetOrAdd(hubName,
                (new Lazy<Task<IServiceHubContext>>(
                    () => ServiceManager.CreateHubContextAsync(hubName)), default));
            return GetAsyncCore(hubName, pair);
        }

        private ValueTask<IServiceHubContext> GetAsyncCore(string hubName, (Lazy<Task<IServiceHubContext>> lazy, IServiceHubContext value) pair)
        {
            if (pair.lazy == null)
            {
                return new ValueTask<IServiceHubContext>(pair.value);
            }
            else
            {
                return new ValueTask<IServiceHubContext>(GetFromLazyAsync(hubName, pair));
            }
        }

        private async Task<IServiceHubContext> GetFromLazyAsync(string hubName, (Lazy<Task<IServiceHubContext>> lazy, IServiceHubContext value) pair)
        {
            try
            {
                var value = await pair.lazy.Value;
                _weakTypedHubStore.TryUpdate(hubName, (null, value), pair);
                return value;
            }
            catch (Exception)
            {
                _weakTypedHubStore.TryRemove(hubName, out _);
                throw;
            }
        }

        private Task<ServiceHubContext<T>> GetAsync<THub, T>() where THub : ServerlessHub<T> where T : class
        {
            return _strongTypedHubServiceProvider.GetRequiredService<ServerlessHubContext<THub, T>>().HubContextTask;
        }

        ///<summary>
        /// The method actually does the following thing
        ///<code>
        /// private Task<ServiceHubContext<T>> GetAsync<THub, T>() where THub : ServerlessHub<T> where T : class
        ///{
        ///    return _serviceProvider.GetRequiredService<ServerlessHubContext<THub, T>>().HubContext;
        ///}
        /// </code>
        /// </summary>
        public dynamic GetAsync(Type THubType, Type TType) 
        {
            var genericType = typeof(ServerlessHubContext<,>);
            Type[] typeArgs = { THubType, TType };
            var serverlessHubContextType = genericType.MakeGenericType(typeArgs);
            dynamic serverlessHubContext =  _strongTypedHubServiceProvider.GetRequiredService(serverlessHubContextType);
            return serverlessHubContext.HubContextTask.GetAwaiter().GetResult();
        }
    }
}