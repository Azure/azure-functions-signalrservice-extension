using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Hosting;

[assembly: WebJobsStartup(typeof(SignalRWebJobsStartup))]
namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    public class SignalRWebJobsStartup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            builder.AddSignalR();
        }
    }
}