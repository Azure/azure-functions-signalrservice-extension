// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace SignalRServiceExtension.Tests
{
    public class DependencyInjectionExtensionFacts
    {
        [Fact]
        public void EmptyHubProtocolSetting_DoNothing()
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
            var services = new ServiceCollection()
                .SetHubProtocol(configuration);
            Assert.Empty(services);
        }

#if NETCOREAPP2_1

        [Fact]
        public void SetHubProtocol_Throw()
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
            configuration[Constants.AzureSignalRHubProtocol] = HubProtocol.SystemTextJson.ToString();
            var services = new ServiceCollection();
            Assert.Throws<InvalidOperationException>(() => services.SetHubProtocol(configuration));
        }

#endif

#if NETCOREAPP3_1

        [Fact]
        public void SetSystemTextJson_DoNothing()
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
            configuration[Constants.AzureSignalRHubProtocol] = HubProtocol.SystemTextJson.ToString();
            var services = new ServiceCollection()
                .SetHubProtocol(configuration);
            Assert.Empty(services);
        }

        [Fact]
        public void SetNewtonsoft()
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
            configuration[Constants.AzureSignalRHubProtocol] = HubProtocol.NewtonsoftJson.ToString();
            var services = new ServiceCollection()
                .SetHubProtocol(configuration);
            var hubProtocolImpl = services.Single().ImplementationType;
            Assert.Equal(typeof(NewtonsoftJsonHubProtocol), hubProtocolImpl);
        }
#endif
    }
}