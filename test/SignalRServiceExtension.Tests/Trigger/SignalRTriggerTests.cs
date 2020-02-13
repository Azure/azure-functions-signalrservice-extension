using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Moq;
using SignalRServiceExtension.Tests.Utils;
using Xunit;

namespace SignalRServiceExtension.Tests
{
    public class SignalRTriggerTests
    {
        [Fact]
        public async Task BindAsyncTest()
        {
            var parameterInfo = this.GetType().GetMethod(nameof(TestFunction)).GetParameters()[0];
            var router = new TestTriggerRouter();
            var binding = new SignalRTriggerBinding(parameterInfo, new SignalRTriggerAttribute(), router);
            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var context = new InvocationContext();
            var triggerContext = new SignalRTriggerEvent {Context = context, TaskCompletionSource = tcs};
            var result = await binding.BindAsync(triggerContext, null);
            Assert.Equal(context, await result.ValueProvider.GetValueAsync());
        }

        [Fact]
        public async Task CreateListenerTest()
        {
            var executor = new Mock<ITriggeredFunctionExecutor>().Object;
            var listenerFactoryContext =
                new ListenerFactoryContext(new FunctionDescriptor(), executor, CancellationToken.None);
            var parameterInfo = this.GetType().GetMethod(nameof(TestFunction)).GetParameters()[0];
            var router = new TestTriggerRouter();
            var hub = Guid.NewGuid().ToString();
            var method = Guid.NewGuid().ToString();
            var binding = new SignalRTriggerBinding(parameterInfo, new SignalRTriggerAttribute{HubName = hub, Target = method}, router);
            await binding.CreateListenerAsync(listenerFactoryContext);
            Assert.Equal(executor, router.Executors[(hub, method)]);
        }

        public void TestFunction(InvocationContext context)
        {
        }
    }
}
