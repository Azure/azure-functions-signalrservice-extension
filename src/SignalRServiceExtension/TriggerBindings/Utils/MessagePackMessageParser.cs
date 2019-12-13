using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    class MessagePackMessageParser : MessageParser
    {
        public override bool TryParseMessage(ref ReadOnlySequence<byte> buffer, out ISignalRServerlessMessage message)
        {
            throw new NotImplementedException();
        }
    }
}
