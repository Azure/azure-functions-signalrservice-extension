using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService.Exceptions
{
    internal class SignalRBindingException : Exception
    {
        public SignalRBindingException() : base()
        {
        }

        public SignalRBindingException(string message) : base(message)
        {
        }
    }
}
