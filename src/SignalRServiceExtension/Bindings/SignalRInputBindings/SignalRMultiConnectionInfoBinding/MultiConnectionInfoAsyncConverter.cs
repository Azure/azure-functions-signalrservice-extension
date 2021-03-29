// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.SignalR;
using Microsoft.Azure.SignalR.Management;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal class MultiConnectionInfoAsyncConverter : IAsyncConverter<SignalRMultiConnectionInfoAttribute, Dictionary<ServiceEndpoint, SignalRConnectionInfo>>
    {
        private readonly IServiceManagerStore _serviceManagerStore;

        public MultiConnectionInfoAsyncConverter(IServiceManagerStore serviceManagerStore)
        {
            _serviceManagerStore = serviceManagerStore;
        }

        public async Task<Dictionary<ServiceEndpoint, SignalRConnectionInfo>> ConvertAsync(
            SignalRMultiConnectionInfoAttribute input, CancellationToken cancellationToken)
        {
            var serviceHubContext = await _serviceManagerStore
                .GetOrAddByConnectionStringKey(input.ConnectionStringSetting)
                .GetAsync(input.HubName) as IInternalServiceHubContext;
            var endpoints = serviceHubContext.GetServiceEndpoints();
            var dict = endpoints.ToDictionary(e => e, async e =>
             {
                 var subHubContext = serviceHubContext.WithEndpoints(new ServiceEndpoint[] { e });
                 var azureSignalRClient = new AzureSignalRClient(subHubContext);
                 var negotiationRes = await azureSignalRClient.GetClientConnectionInfoAsync(input.UserId, input.IdToken, input.ClaimTypeList, null);
                 return negotiationRes;
             });
            _ = await Task.WhenAll(dict.Values);
            return dict.ToDictionary(pair => pair.Key, pair => pair.Value.Result);
        }
    }
}