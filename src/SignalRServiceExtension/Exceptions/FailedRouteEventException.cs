namespace Microsoft.Azure.WebJobs.Extensions.SignalRService.Exceptions
{
    internal class FailedRouteEventException : SignalRTriggerException
    {
        public FailedRouteEventException(string message) : base(message)
        {
        }
    }
}
