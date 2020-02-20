using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Azure.WebJobs.Host.Executors;
using Moq;
using Xunit;

namespace SignalRServiceExtension.Tests
{
    public class SignalRTriggerDispatcherTests
    {
        [Fact]
        public void DispatcherMappingTest()
        {
            var dispatcher = new SignalRTriggerDispatcher();
            var key = (Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
            var executor = new Mock<ITriggeredFunctionExecutor>().Object;
            dispatcher.Map(key, executor);

        }
    }
}
