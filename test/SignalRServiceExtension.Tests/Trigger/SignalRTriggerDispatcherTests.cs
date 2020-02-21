using System;
using System.Collections.Generic;
using System.Net.Http;
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
            var resolve = new TestRequestResolver();
            var dispatcher = new SignalRTriggerDispatcher(resolve);
            var key = (Guid.NewGuid().ToString(), "connections", Guid.NewGuid().ToString());
            var executor = new Mock<ITriggeredFunctionExecutor>().Object;
            dispatcher.Map(key, new ExecutionContext{Executor = executor, AccessKey = string.Empty});
        }

        private class TestRequestResolver : IRequestResolver
        {
            public InvocationContext Context { get; set; }

            public bool ValidateContentType(HttpRequestMessage request) => true;

            public bool ValidateSignature(HttpRequestMessage request, string accessKey) => true;

            public bool TryGetInvocationContext(HttpRequestMessage request, out InvocationContext context)
            {
                context = Context;
                return true;
            }
        }
    }
}
