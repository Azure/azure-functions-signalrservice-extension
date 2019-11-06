namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    public enum AccessTokenStatus
    {
        Valid,
        Expired,
        Error,
        NoToken
    }
}