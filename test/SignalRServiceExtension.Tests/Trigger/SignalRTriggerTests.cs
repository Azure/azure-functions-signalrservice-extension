// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
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
            var dispatcher = new TestTriggerDispatcher();
            var binding = new SignalRTriggerBinding(parameterInfo, new SignalRTriggerAttribute(), dispatcher);
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
            var dispatcher = new TestTriggerDispatcher();
            var hub = Guid.NewGuid().ToString();
            var category = "connections";
            var method = Guid.NewGuid().ToString();
            var binding = new SignalRTriggerBinding(parameterInfo, new SignalRTriggerAttribute{HubName = hub, Category = category, Event = method}, dispatcher);
            await binding.CreateListenerAsync(listenerFactoryContext);
            Assert.Equal(executor, dispatcher.Executors[(hub, category, method)].Executor);
        }

        public void TestFunction(InvocationContext context)
        {
        }
    }
}
