using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.WebJobs.Extensions.SignalRService.Protocols;
using Microsoft.Azure.WebJobs.Host.Listeners;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    public class SignalRTriggerFunctionData
    {
        public ListenerFactoryContext Context { get; set; }

        public string Hub { get; set; }

        public string Target { get; set; }

        public SignalRTriggerFunctionData(ListenerFactoryContext context)
        {
            Context = context;
        }

        public static Func<SignalRTriggerFunctionData, SignalRExtensionMessage, bool> Filter = (data, message) =>
        {
            bool isMarch = true;
            if (!string.IsNullOrWhiteSpace(data.Hub))
            {
                isMarch = data.Hub == message.Hub;
            }

            if (!string.IsNullOrWhiteSpace(data.Target) &&
                message.MessageType == SignalRExtensionProtocolConstants.InvocationType)
            {
                isMarch = isMarch && data.Target == ((InvocationExtensionMessage) message).Target;
            }

            return isMarch;
        };
    }
}
