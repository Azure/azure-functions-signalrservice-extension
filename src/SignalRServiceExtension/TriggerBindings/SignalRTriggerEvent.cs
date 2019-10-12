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
        /// SignalR Context that returns from Function method and being used to generate access token
        /// </summary>
        public TaskCompletionSource<InvocationContext> ContextTcs { get; set; }
    }
}
