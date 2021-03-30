// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.SignalR;
using Microsoft.Azure.SignalR.Management;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal class MultiConnectionInfoAsyncConverter : IAsyncConverter<SignalRMultiConnectionInfoAttribute, EndpointConnectionInfo[]>
    {
        private readonly IServiceManagerStore _serviceManagerStore;

        public MultiConnectionInfoAsyncConverter(IServiceManagerStore serviceManagerStore)
        {
            _serviceManagerStore = serviceManagerStore;
        }

        public async Task<EndpointConnectionInfo[]> ConvertAsync(
            SignalRMultiConnectionInfoAttribute input, CancellationToken cancellationToken)
        {
            var serviceHubContext = await _serviceManagerStore
                .GetOrAddByConnectionStringKey(input.ConnectionStringSetting)
                .GetAsync(input.HubName) as IInternalServiceHubContext;
            var endpoints = serviceHubContext.GetServiceEndpoints();
            return await Task.WhenAll(endpoints.Select(async e =>
             {
                 var subHubContext = serviceHubContext.WithEndpoints(new ServiceEndpoint[] { e });
                 var azureSignalRClient = new AzureSignalRClient(subHubContext);
                 var negotiationRes = await azureSignalRClient.GetClientConnectionInfoAsync(input.UserId, input.IdToken, input.ClaimTypeList, null);
                 return new EndpointConnectionInfo
                 {
                     Endpoint = e,
                     ConnectionInfo = new SignalRConnectionInfo { AccessToken = negotiationRes.AccessToken, Url = negotiationRes.Url }
                 };
             }));
        }
    }
}