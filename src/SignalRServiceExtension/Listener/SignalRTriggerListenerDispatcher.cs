using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs.Extensions.SignalRService.Protocols;
using Microsoft.Azure.WebJobs.Extensions.SignalRService.Trigger;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    // A app scoped singleton dispatcher to diliver EventData to correct functions
    public class SignalRTriggerListenerDispatcher
    {
        private readonly ConcurrentDictionary<string, ListenerFactoryContext> _contextDictionary =
            new ConcurrentDictionary<string, ListenerFactoryContext>();

        private readonly ConcurrentDictionary<Type, HashSet<string>> _arrtibuteDictionary =
            new ConcurrentDictionary<Type, HashSet<string>>();

        private readonly ISignalRExtensionProtocols _protocols;

        public SignalRTriggerListenerDispatcher(ISignalRExtensionProtocols protocols)
        {
            _protocols = protocols;
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

        public async Task DispatchListener(EventData input, CancellationTokenSource cts)
        {
            HashSet<string> relatedFunctions = null;
            if (!_protocols.TryParseMessage(input, out var message))
            {
                return;
            }

            switch (message.MessageType)
            {
                case SignalRExtensionProtocolConstants.OpenConnectionType:
                    _arrtibuteDictionary.TryGetValue(typeof(SignalROpenConnectionTriggerAttribute),
                        out relatedFunctions);
                    break;
                case SignalRExtensionProtocolConstants.CloseConnectionType:
                    _arrtibuteDictionary.TryGetValue(typeof(SignalRCloseConnectionTriggerAttribute),
                        out relatedFunctions);
                    break;
                case SignalRExtensionProtocolConstants.InvocationType:
                    _arrtibuteDictionary.TryGetValue(typeof(SignalRInvocationMessageTriggerAttribute),
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
                    TriggerValue = message
                };
                if (context != null)
                {
                    await context.Executor.TryExecuteAsync(triggeredInput, cts.Token);
                }
            }
        }
    }
}
