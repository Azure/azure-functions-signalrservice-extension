using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal interface IRequestResolver
    {
        bool ValidateContentType(HttpRequestMessage request);

        bool ValidateSignature(HttpRequestMessage request, string accessKey);

        bool TryGetInvocationContext(HttpRequestMessage request, out InvocationContext context);
    }
}
