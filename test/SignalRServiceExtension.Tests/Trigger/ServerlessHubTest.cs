using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Moq;
using Xunit;

namespace SignalRServiceExtension.Tests.Trigger
{
    public class ServerlessHubTest
    {
        [Fact]
        private async Task ServerlessHubUnitTest()
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
            var myHub = new MyHub(serviceHubContextMoc.Object, serviceManagerMoc.Object);

            // Unit test negotiate
            var userId = Guid.NewGuid().ToString();
            myHub.Negotiate(userId);
            serviceManagerMoc.Verify(s => s.GetClientEndpoint(nameof(MyHub)), Times.Once);
            serviceManagerMoc.Verify(s => s.GenerateClientAccessToken(nameof(MyHub), userId, It.IsAny<IList<Claim>>(), It.IsAny<TimeSpan?>()), Times.Once);

            // Unit test broadcast method
            var target = Guid.NewGuid().ToString();
            var message = Guid.NewGuid().ToString();
            await myHub.Broadcast(new InvocationContext(), target, message);
            clientProxyMoc.Verify(c => c.SendCoreAsync(target, It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    public class MyHub : ServerlessHub
    {
        public MyHub(IServiceHubContext serviceHubContext, IServiceManager serviceManager): base(serviceHubContext, serviceManager)
        {
        }

        [FunctionName("negotiate")]
        public SignalRConnectionInfo Negotiate(string userId)
        {
            return base.Negotiate(userId);
        }

        [FunctionName(nameof(Broadcast))]
        public async Task Broadcast([SignalRTrigger] InvocationContext invocationContext, string target, string message)
        {
            await Clients.All.SendAsync(target, message);
        }
    }
}
