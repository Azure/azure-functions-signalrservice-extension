using System;
using System.Collections.Generic;
using System.Linq;
using Azure.Core;
using Microsoft.Azure.SignalR;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    /// <summary>
    /// A helper class to parse <see cref="ServiceEndpoint"/> from configuration.
    /// </summary>
    /// <remarks>
    /// Copyied from https://github.com/Azure/azure-signalr/blob/e5a69b20daf7f7aa2786ff59df61433c4050c6ab/src/Microsoft.Azure.SignalR.Common/Endpoints/IConfigurationExtension.cs.
    /// Although we have friend access to that internal class, referencing internal methods are not recommended as changes of internal methods might break this package.
    /// </remarks>
    internal static class IConfigurationExtensions
    {
        public static IEnumerable<ServiceEndpoint> GetEndpoints(this IConfiguration config, AzureComponentFactory azureComponentFactory)
        {
            foreach (IConfigurationSection child in config.GetChildren())
            {
                if (child.TryGetNamedEndpointFromIdentity(azureComponentFactory, out ServiceEndpoint endpoint))
                {
                    yield return endpoint;
                    continue;
                }

                foreach (ServiceEndpoint item in child.GetNamedEndpointsFromConnectionString())
                {
                    yield return item;
                }
            }
        }

        public static IEnumerable<ServiceEndpoint> GetNamedEndpointsFromConnectionString(this IConfigurationSection section)
        {
            string endpointName = section.Key;
            if (section.Value != null)
            {
                yield return new ServiceEndpoint(section.Key, section.Value);
            }

            if (section["primary"] != null)
            {
                yield return new ServiceEndpoint(section["primary"], EndpointType.Primary, endpointName);
            }

            if (section["secondary"] != null)
            {
                yield return new ServiceEndpoint(section["secondary"], EndpointType.Secondary, endpointName);
            }
        }

        public static bool TryGetNamedEndpointFromIdentity(this IConfigurationSection section, AzureComponentFactory azureComponentFactory, out ServiceEndpoint endpoint)
        {
            string text = section["ServiceUri"];
            if (text != null)
            {
                string key = section.Key;
                EndpointType value = section.GetValue("Type", EndpointType.Primary);
                TokenCredential credential = azureComponentFactory.CreateTokenCredential(section);
                endpoint = new ServiceEndpoint(new Uri(text), credential, value, key);
                return true;
            }

            endpoint = null;
            return false;
        }
    }
}
