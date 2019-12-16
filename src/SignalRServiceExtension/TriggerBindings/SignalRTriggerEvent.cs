using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal class SignalRTriggerEvent
    {
        /// <summary>
        /// SignalR Context that gets from HTTP request and pass the Function parameters
        /// </summary>
        public InvocationContext Context { get; set; }

        /// <summary>
        /// A TaskCompletionSource will set result when the function invocation has finished.
        /// </summary>
        public TaskCompletionSource<object> TaskCompletionSource { get; set; }
    }
}
