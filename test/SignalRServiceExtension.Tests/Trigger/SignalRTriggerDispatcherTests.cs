using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Azure.SignalR.Serverless.Protocols;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Azure.WebJobs.Host.Executors;
using Moq;
using SignalRServiceExtension.Tests.Utils;
using Xunit;
using ExecutionContext = Microsoft.Azure.WebJobs.Extensions.SignalRService.ExecutionContext;

namespace SignalRServiceExtension.Tests
{
    public class SignalRTriggerDispatcherTests
    {
        public static IEnumerable<object[]> AttributeData()
        {
            yield return new object[] { "connections", "connect", false };
            yield return new object[] { "connections", "disconnect", false };
            yield return new object[] { "connections", Guid.NewGuid().ToString(), true };
            yield return new object[] { "messages", Guid.NewGuid().ToString(), false };
            yield return new object[] { Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), true };
        }

        [Theory]
        [MemberData(nameof(AttributeData))]
        public async Task DispatcherMappingTest(string category, string @event, bool throwException)
        {
            var resolve = new TestRequestResolver();
            var dispatcher = new SignalRTriggerDispatcher(resolve);
            var key = (hub: Guid.NewGuid().ToString(), category, @event);
            var tcs = new TaskCompletionSource<ITriggeredFunctionExecutor>(TaskCreationOptions.RunContinuationsAsynchronously);
            var executorMoc = new Mock<ITriggeredFunctionExecutor>();
            executorMoc.Setup(f => f.TryExecuteAsync(It.IsAny<TriggeredFunctionData>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new FunctionResult(true)));
            var executor = executorMoc.Object;
            if (throwException)
            {
                Assert.ThrowsAny<Exception>(() => dispatcher.Map(key, new ExecutionContext {Executor = executor, AccessKey = string.Empty}));
                return;
            }

            dispatcher.Map(key, new ExecutionContext {Executor = executor, AccessKey = string.Empty});
            var request = TestHelpers.CreateHttpRequestMessage(key.hub, key.category, key.@event, Guid.NewGuid().ToString());
            await dispatcher.ExecuteAsync(request);
            executorMoc.Verify(e => e.TryExecuteAsync(It.IsAny<TriggeredFunctionData>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        private class TestRequestResolver : IRequestResolver
        {
            public bool ValidateContentType(HttpRequestMessage request) => true;

            public bool ValidateSignature(HttpRequestMessage request, string accessKey) => true;

            public bool TryGetInvocationContext(HttpRequestMessage request, out InvocationContext context)
            {
                context = new InvocationContext();
                return true;
            }

            public Task<(T, IHubProtocol)> GetMessageAsync<T>(HttpRequestMessage request) where T : ServerlessMessage, new()
            {
                return Task.FromResult<(T, IHubProtocol)>((new T(), new JsonHubProtocol()));
            }
        }
    }
}
