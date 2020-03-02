using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{ 
    class SignalRTriggerParametersNotMatchException : SignalRTriggerException
    {
        public SignalRTriggerParametersNotMatchException(int excepted, int actual) : base(
            $"The function expected {excepted} parameters but got {actual}")
        {
        }
    }
}
