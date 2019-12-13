using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal class SignalRHubMethodExecutor
    {
        private const string OnConnectedTarget = "OnConnected";
        private const string OnDisconnectedTarget = "OnDisconnected";

        private readonly string _hub;
        private readonly Dictionary<string, SignalRListener> _listeners = new Dictionary<string, SignalRListener>(StringComparer.OrdinalIgnoreCase);

        public SignalRHubMethodExecutor(string hub)
        {
            _hub = hub ?? throw new ArgumentNullException(nameof(hub));
        }

        public void AddListener(string target, SignalRListener listener)
        {
            _listeners.Add(target, listener);
        }

        public Task ExecuteMethod(InvocationContext.ConnectionContext context, ISignalRServerlessMessage message)
        {
            string target;
            if (message is InvocationMessage invocation)
            {
                target = invocation.Target;
            }
            else if (message is OpenConnectionMessage)
            {
                target = OnConnectedTarget;
            }
            else if (message is CloseConnectionMessage)
            {
                target = OnDisconnectedTarget;
            }
            else
            {
                throw new Exception("Target not specified.");
            }

            if (_listeners.TryGetValue(target, out var listener))
            {
                var signalRTriggerEvent = new SignalRTriggerEvent
                {
                    Context = new InvocationContext
                    {
                        HubName = _hub,
                        Context = context,
                        Data = message,
                    },
                };
                // TODO: select out listener that match the pattern

                return listener.Executor.TryExecuteAsync(
                    new Host.Executors.TriggeredFunctionData
                    {
                        TriggerValue = signalRTriggerEvent
                    }, CancellationToken.None);

                // TODO: Support invokeAsync later
            }

            throw new Exception($"Target: {target} not found.");
        }
    }
}
