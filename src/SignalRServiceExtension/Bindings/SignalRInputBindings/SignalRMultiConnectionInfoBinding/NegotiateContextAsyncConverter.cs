// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.SignalR;
using Microsoft.Azure.SignalR.Management;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal class NegotiateContextAsyncConverter : IAsyncConverter<NegotiateContextAttribute, NegotiateContext>
    {
        private readonly IServiceManagerStore _serviceManagerStore;

        public NegotiateContextAsyncConverter(IServiceManagerStore serviceManagerStore)
        {
            _serviceManagerStore = serviceManagerStore;
        }

        public async Task<NegotiateContext> ConvertAsync(
            NegotiateContextAttribute input, CancellationToken cancellationToken)
        {
            var serviceHubContext = await _serviceManagerStore
                .GetOrAddByConnectionStringKey(input.ConnectionStringSetting)
                .GetAsync(input.HubName) as IInternalServiceHubContext;
            var endpoints = serviceHubContext.GetServiceEndpoints();
            var endpointConnectionInfo = await Task.WhenAll(endpoints.Select(async e =>
            {
                var subHubContext = serviceHubContext.WithEndpoints(new ServiceEndpoint[] { e });
                var azureSignalRClient = new AzureSignalRClient(subHubContext);
                var negotiationRes = await azureSignalRClient.GetClientConnectionInfoAsync(input.UserId, input.IdToken, input.ClaimTypeList, null);
                return new EndpointConnectionInfo
                {
                    ServiceEndpoint = e,
                    AccessToken = negotiationRes.AccessToken,
                    Url = negotiationRes.Url
                };
            }));
            return new NegotiateContext { ClientEndpoints = endpointConnectionInfo };
        }
    }
}