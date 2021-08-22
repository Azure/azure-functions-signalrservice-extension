// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.SignalR;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Moq;
using Xunit;

namespace SignalRServiceExtension.Tests.Trigger
{
    public class GenericServerlessHubTest
    {
        [Fact]
        public async Task GenericServerlessHubUnitTest()
        {
            var clientProxyMoc = new Mock<IClientProxy>();
            clientProxyMoc
                .Setup(c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            var serviceHubContextMoc = new Mock<IServiceHubContext>();
            var hubClientsMoc = new Mock<IHubClients>();
            hubClientsMoc.SetupGet(h => h.All).Returns(clientProxyMoc.Object);
            serviceHubContextMoc.SetupGet(s => s.Clients).Returns(hubClientsMoc.Object);

            var serviceManagerMoc = new Mock<IServiceManager>();
            var myHub = new MyGenericHub(serviceHubContextMoc.Object, serviceManagerMoc.Object);

            // Unit test broadcast method
            var message = "Hello World";
            await myHub.Broadcast(new InvocationContext(), message);
            clientProxyMoc.Verify(c => c.SendCoreAsync(nameof(IHubMethods.OnBroadcast), It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    public interface IHubMethods
    {
        public Task OnBroadcast(string message);
    }

    public class MyGenericHub : ServerlessHub<IHubMethods>
    {
        public MyGenericHub(IServiceHubContext serviceHubContext, IServiceManager serviceManager) : base(serviceHubContext, serviceManager)
        {
        }

        [FunctionName(nameof(Broadcast))]
        public async Task Broadcast([SignalRTrigger] InvocationContext invocationContext, string message)
        {
            await Clients.All.OnBroadcast(message);
        }
    }
}
