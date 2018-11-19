using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs.Host.Listeners;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    public interface ISignalRTriggerListenerDispatcher
    {
        void RegisterFunction(string functionId, Type attributeType, string hubName,
            ListenerFactoryContext context, string target = null);

        Task DispatchListener(EventData input, CancellationToken token);
    }
}
