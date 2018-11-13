using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService.Protocols
{
    public static class SignalRExtensionProtocolConstants
    {
        public const int OpenConnectionType = 1;
        public const int CloseConnectionType = 2;
        public const int InvocationType = 3;

        public const string ConnectionId = "ConnectionId";
        public const string Hub = "Hub";
        public const string Target = "Target";

        public const string MessageType = "MessageType";
    }
}
