using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.SignalRService.Trigger;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    // A singleton dispatcher to diliver EventData to correct functions
    public class SignalRTriggerListenerDispatcher
    {
        private readonly ConcurrentDictionary<string, ListenerFactoryContext> _contextDictionary =
            new ConcurrentDictionary<string, ListenerFactoryContext>();

        private readonly ConcurrentDictionary<Type, HashSet<string>> _arrtibuteDictionary =
            new ConcurrentDictionary<Type, HashSet<string>>();

        public SignalRTriggerListenerDispatcher()
        {
            
        }

        public void RegisterFunction(string functionId, Type attributeType, ListenerFactoryContext context)
        {
            _arrtibuteDictionary.AddOrUpdate(attributeType, type => new HashSet<string>() {functionId}, (type, set) =>
                {
                    set.Add(functionId);
                    return set;
                });
            _contextDictionary.AddOrUpdate(functionId, context, (_, __) => context);
        }

        public async Task DispatchListener(SignalRTriggerInput input, CancellationTokenSource cts)
        {
            HashSet<string> relatedFunctions = null;
            switch (input.MessageType)
            {
                case "OpenConnection":
                    _arrtibuteDictionary.TryGetValue(typeof(SignalROpenConnectionTriggerAttribute),
                        out relatedFunctions);
                    break;
                case "CloseConnection":
                    _arrtibuteDictionary.TryGetValue(typeof(SignalRCloseConnectionTriggerAttribute),
                        out relatedFunctions);
                    break;
                case "InvocationMessage":
                    _arrtibuteDictionary.TryGetValue(typeof(SignalRCloseConnectionTriggerAttribute),
                        out relatedFunctions);
                    break;
            }

            if (relatedFunctions == null)
            {
                return;
            }

            foreach (var functionId in relatedFunctions)
            {
                _contextDictionary.TryGetValue(functionId, out var context);
                var triggeredInput = new TriggeredFunctionData()
                {
                    TriggerValue = input
                };
                if (context != null)
                {
                    await context.Executor.TryExecuteAsync(triggeredInput, cts.Token);
                }
            }
        }
    }
}
