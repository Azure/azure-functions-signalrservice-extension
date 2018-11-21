using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Azure.WebJobs.Extensions.SignalRService.Protocols;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace SignalRServiceExtension.Tests.Listener
{
    public class SignalRTriggerListenerDispatcherTests
    {
        private readonly ISignalRExtensionProtocols _protocols = new SignalRExtensionProtocols();

        [Fact]
        public async Task DispatchListenerTest()
        {
            var dispatcher = new SignalRTriggerListenerDispatcher(_protocols);
            var invocationWithHubAndTargetFunc = new TestFunctionHelper(typeof(SignalRInvocationMessageTriggerAttribute), "TestHub1", "TestTarget1").RegisterFunction(dispatcher);
            var invocationWithHub1Func = new TestFunctionHelper(typeof(SignalRInvocationMessageTriggerAttribute), "TestHub1").RegisterFunction(dispatcher);
            var invocationWithHub2Func = new TestFunctionHelper(typeof(SignalRInvocationMessageTriggerAttribute), "TestHub2").RegisterFunction(dispatcher);
            var openWithHubFunc = new TestFunctionHelper(typeof(SignalROpenConnectionTriggerAttribute), "TestHub1").RegisterFunction(dispatcher);
            var openFunc = new TestFunctionHelper(typeof(SignalROpenConnectionTriggerAttribute)).RegisterFunction(dispatcher);
            var closeWithHubFunc = new TestFunctionHelper(typeof(SignalRCloseConnectionTriggerAttribute), "TestHub1").RegisterFunction(dispatcher);
            var closeFunc = new TestFunctionHelper(typeof(SignalRCloseConnectionTriggerAttribute)).RegisterFunction(dispatcher);

            // Test open messages
            await dispatcher.DispatchListener(
                GetEventData(SignalRExtensionProtocolConstants.OpenConnectionType, "TestHub1"), CancellationToken.None);
            openWithHubFunc.VerifyCalledExactlyTimes(1);
            openFunc.VerifyCalledExactlyTimes(1);
            await dispatcher.DispatchListener(
                GetEventData(SignalRExtensionProtocolConstants.OpenConnectionType, "TestHub2"), CancellationToken.None);
            openWithHubFunc.VerifyCalledExactlyTimes(1);
            openFunc.VerifyCalledExactlyTimes(2);

            // Test close messages
            await dispatcher.DispatchListener(
                GetEventData(SignalRExtensionProtocolConstants.CloseConnectionType, "TestHub1"), CancellationToken.None);
            closeWithHubFunc.VerifyCalledExactlyTimes(1);
            closeFunc.VerifyCalledExactlyTimes(1);

            // Test invocation message
            await dispatcher.DispatchListener(
                GetEventData(SignalRExtensionProtocolConstants.InvocationType, "TestHub1", "TestTarget1"), CancellationToken.None);
            invocationWithHubAndTargetFunc.VerifyCalledExactlyTimes(1);
            invocationWithHub1Func.VerifyCalledExactlyTimes(1);
            invocationWithHub2Func.VerifyCalledExactlyTimes(0);

            await dispatcher.DispatchListener(
                GetEventData(SignalRExtensionProtocolConstants.InvocationType, "TestHub2", "TestTarget2"), CancellationToken.None);
            invocationWithHubAndTargetFunc.VerifyCalledExactlyTimes(1);
            invocationWithHub1Func.VerifyCalledExactlyTimes(1);
            invocationWithHub2Func.VerifyCalledExactlyTimes(1);
        }

        internal class TestFunctionHelper
        {
            private readonly Type _type;
            private readonly string _hub;
            private readonly string _target;
            private readonly string _functionId = Guid.NewGuid().ToString();
            private Mock<ITriggeredFunctionExecutor> _mock;
            private ListenerFactoryContext _context;

            public TestFunctionHelper(Type attributeType, string hub = null, string target = null)
            {
                _type = attributeType;
                _hub = hub;
                _target = target;
            }

            public TestFunctionHelper RegisterFunction(SignalRTriggerListenerDispatcher dispatcher)
            {
                _mock = new Mock<ITriggeredFunctionExecutor>();
                _mock
                    .Setup(executor => executor.TryExecuteAsync(It.IsAny<TriggeredFunctionData>(), CancellationToken.None))
                    .Returns(Task.FromResult(new FunctionResult(true)));
                _context = new ListenerFactoryContext(new FunctionDescriptor() { Id = _functionId }, _mock.Object, CancellationToken.None);
                dispatcher.RegisterFunction(_functionId, _type, _hub, _context, _target);
                return this;
            }

            public void VerifyCalledExactlyTimes(int times)
            {
                _mock.Verify(executor => executor.TryExecuteAsync(It.IsAny<TriggeredFunctionData>(), CancellationToken.None), Times.Exactly(times));
            }
        }

        private EventData GetEventData(int messageType, string hub, string target = null)
        {
            var obj = new
            {
                Target = target,
                Arguement = new[]
                {
                    "Argument1",
                    "Argument2"
                }
            };
            var bytes = Encoding.UTF8.GetBytes(JObject.FromObject(obj).ToString());
            return _protocols.BuildMessage(messageType, hub, Guid.NewGuid().ToString(), bytes);
        }
    }
}
