// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.SignalR;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Azure.SignalR.Tests.Common;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Constants = Microsoft.Azure.WebJobs.Extensions.SignalRService.Constants;

namespace SignalRServiceExtension.Tests
{
    public class MultiConnectionInfoAsyncConverterTest
    {
        private const string HubName = "hub1";
        private static int count = 3;

        private static readonly IEnumerable<ServiceEndpoint> PrimaryEndpoints = FakeEndpointUtils.GetFakeConnectionString(count).Zip(Enumerable.Range(0, count))
            .Select(pair => new ServiceEndpoint(pair.First, EndpointType.Primary, $"p{pair.Second}"));

        private static readonly IEnumerable<ServiceEndpoint> SecondaryEndpoints = FakeEndpointUtils.GetFakeConnectionString(count).Zip(Enumerable.Range(0, count))
            .Select(pair => new ServiceEndpoint(pair.First, EndpointType.Secondary, $"s{pair.Second}"));

        private static readonly IEnumerable<ServiceEndpoint> Endpoints = PrimaryEndpoints.Concat(SecondaryEndpoints);

        [Fact]
        public async Task EndpointsEqualFact()
        {
            var configuration = CreateTestConfiguration();
            var serviceManagerStore = new ServiceManagerStore(configuration, NullLoggerFactory.Instance);
            var converter = new MultiConnectionInfoAsyncConverter(serviceManagerStore);
            var attribute = new SignalRMultiConnectionInfoAttribute { HubName = HubName };

            var dict = await converter.ConvertAsync(attribute, default);
            foreach (var expectedEndpoint in Endpoints)
            {
                Assert.Contains(expectedEndpoint, dict.Keys);
            }
        }

        private static IConfiguration CreateTestConfiguration()
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
            configuration[Constants.ServiceTransportTypeName] = ServiceTransportType.Persistent.ToString();
            foreach (var e in Endpoints)
            {
                AddEndpointsToConfiguration(configuration, e);
            }
            return configuration;
        }

        private static void AddEndpointsToConfiguration(IConfiguration configuration, ServiceEndpoint endpoint)
        {
            configuration[$"{Constants.AzureSignalREndpoints}:{endpoint.Name}:{endpoint.EndpointType}"] = endpoint.ConnectionString;
        }
    }
}