using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    public class SignalRConnectionDetail
    {
        public string UserId { get; set; }
        public IList<Claim> Claims { get; set; }
    }
}