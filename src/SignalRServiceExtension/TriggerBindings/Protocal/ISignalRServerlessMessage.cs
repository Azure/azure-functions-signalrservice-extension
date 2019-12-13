using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    public interface ISignalRServerlessMessage
    {
        int Type { get; set; }
    }
}
