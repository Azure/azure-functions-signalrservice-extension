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

        public async Task<Task<object>> ExecuteMethod(InvocationContext.ConnectionContext context, ISignalRServerlessMessage message)
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

            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            if (_listeners.TryGetValue(target, out var listener))
            {
                var signalRTriggerEvent = new SignalRTriggerEvent
                {
                    Context = new InvocationContext
                    {
                        Context = context,
                        Data = message,
                    },
                    TaskCompletionSource = tcs,
                };
                // TODO: select out listener that match the pattern

                await listener.Executor.TryExecuteAsync(
                    new Host.Executors.TriggeredFunctionData
                    {
                        TriggerValue = signalRTriggerEvent
                    }, CancellationToken.None);

                return tcs.Task;
            }

            throw new Exception($"Target: {target} not found.");
        }
    }
}
