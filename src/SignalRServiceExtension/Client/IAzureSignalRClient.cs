using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal interface IAzureSignalRClient
    {
        Task SendMessage(string hubName, SignalRMessage message);
    }
}