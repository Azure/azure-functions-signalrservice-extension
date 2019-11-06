using Microsoft.AspNetCore.Http;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    public interface IAccessTokenProvider
    {
        AccessTokenResult ValidateToken(HttpRequest request);
    }
}