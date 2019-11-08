using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    public interface ISignalRConnectionInfoConfigurer
    {
        Func<AccessTokenResult, HttpRequest, SignalRConnectionDetail, SignalRConnectionDetail> Configure { get; set; }
    }
}
