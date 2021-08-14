using System;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
  /// <summary>
  /// Customized settings to be passed into the serverless hub context.
  /// </summary>
  [AttributeUsage(AttributeTargets.Class)]
  class ServerlessHubContextAttribute : Attribute
  {
    public ServerlessHubContextAttribute(string connectionStringSetting)
    {
      ConnectionStringSetting = connectionStringSetting;
    }

    public string ConnectionStringSetting { get; set; } = Constants.AzureSignalRConnectionStringName;
  }
}
