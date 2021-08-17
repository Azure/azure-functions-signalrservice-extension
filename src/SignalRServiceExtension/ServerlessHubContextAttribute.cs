using System;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
  /// <summary>
  /// Customized settings to be passed into the serverless hub context.
  /// </summary>
  [AttributeUsage(AttributeTargets.Class)]
  class ServerlessHubContextAttribute : Attribute, IConnectionProvider
  {
    public ServerlessHubContextAttribute(string connectionStringSetting)
    {
      Connection = connectionStringSetting;
    }

    public string Connection { get; set; } = Constants.AzureSignalRConnectionStringName;
  }
}
