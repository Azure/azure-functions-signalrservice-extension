using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService.Exceptions
{
    internal class SignalRTriggerException : Exception
    {
        public SignalRTriggerException() : base()
        {
        }

        public SignalRTriggerException(string message) : base(message)
        {
        }
    }
}
