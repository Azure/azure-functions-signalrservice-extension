using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    public static class ServerlessProtocolConstants
    {
        /// <summary>
        /// Represents the invocation message type.
        /// </summary>
        public const int InvocationMessageType = 1;

        // Reserve number in HubProtocolConstants

        /// <summary>
        /// Represents the open connection message type.
        /// </summary>
        public const int OpenConnectionMessageType = 10;

        /// <summary>
        /// Represents the close connection message type.
        /// </summary>
        public const int CloseConnectionMessageType = 11;
    }
}
