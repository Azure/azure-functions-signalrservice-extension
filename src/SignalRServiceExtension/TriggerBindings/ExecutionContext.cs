using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.WebJobs.Host.Executors;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal class ExecutionContext
    {
        public ITriggeredFunctionExecutor Executor { get; set; }

        public string AccessKey { get; set; }
    }
}
