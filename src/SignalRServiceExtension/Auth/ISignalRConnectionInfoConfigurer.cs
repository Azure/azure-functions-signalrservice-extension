using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    public interface ISignalRConnectionInfoConfigurer
    {
        Action<AccessTokenResult, HttpRequest, SignalRConnectionDetail> Configure { get; set; }
    }
}
