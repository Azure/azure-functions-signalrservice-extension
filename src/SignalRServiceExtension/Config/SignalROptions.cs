// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    public class SignalROptions
    {
        public string ConnectionString { get; set; }

        public string HubName { get; set; }

        public EventProcessorOptions EventProcessorOptions { get; }

        private readonly Dictionary<string, ReceiverCreds> _receiverCreds = new Dictionary<string, ReceiverCreds>(StringComparer.OrdinalIgnoreCase);

        public const string LeaseContainerName = "azure-webjobs-signalr";

        // Use a tuple to identify a host: (EventHub, ConnectionString, ConsumerGroup)
        // As SignalROption is singleton, _hosts is singleton across the function app.
        private readonly ConcurrentDictionary<(string, string, string), EventProcessorHost> _hosts = new ConcurrentDictionary<(string, string, string), EventProcessorHost>();

        public SignalROptions()
        {
            EventProcessorOptions = EventProcessorOptions.DefaultOptions;
        }

        /// <summary>
        /// Add a connection for listening on events from an event hub. Connect via the connection string and use the supplied storage account
        /// </summary>
        /// <param name="eventHubName">name of the event hub</param>
        /// <param name="receiverConnectionString">connection string for receiving messages</param>
        /// <param name="storageConnectionString">storage connection string that the EventProcessorHost client will use to coordinate multiple listener instances. </param>
        public void AddReceiver(string eventHubName, string receiverConnectionString, string storageConnectionString)
        {
            if (eventHubName == null)
            {
                throw new ArgumentNullException("eventHubName");
            }
            if (receiverConnectionString == null)
            {
                throw new ArgumentNullException("receiverConnectionString");
            }
            if (storageConnectionString == null)
            {
                throw new ArgumentNullException("storageConnectionString");
            }

            this._receiverCreds[eventHubName] = new ReceiverCreds
            {
                EventHubConnectionString = receiverConnectionString,
                StorageConnectionString = storageConnectionString
            };
        }

        /// <summary>
        /// Add a connection for listening on events from an event hub. Connect via the connection string and use the SDK's built-in storage account.
        /// </summary>
        /// <param name="eventHubName">name of the event hub</param>
        /// <param name="receiverConnectionString">connection string for receiving messages. This can encapsulate other service bus properties like the namespace and endpoints.</param>
        public void AddReceiver(string eventHubName, string receiverConnectionString)
        {
            if (eventHubName == null)
            {
                throw new ArgumentNullException("eventHubName");
            }
            if (receiverConnectionString == null)
            {
                throw new ArgumentNullException("receiverConnectionString");
            }

            this._receiverCreds[eventHubName] = new ReceiverCreds
            {
                EventHubConnectionString = receiverConnectionString
            };
        }

        // Lookup a listener for receiving events given the name provided in the [EventHubTrigger] attribute. 
        internal EventProcessorHost GetEventProcessorHost(IConfiguration config, string eventHubName, string receiverConnectionString, string consumerGroup)
        {
            if (eventHubName == null)
            {
                throw new ArgumentNullException("eventHubName");
            }
            if (receiverConnectionString == null)
            {
                throw new ArgumentNullException("receiverConnectionString");
            }


            // Common case. Create a new EventProcessorHost instance to listen. 
            string eventProcessorHostName = Guid.NewGuid().ToString();

            if (consumerGroup == null)
            {
                consumerGroup = PartitionReceiver.DefaultConsumerGroupName;
            }

            string defaultStorageString = config.GetWebJobsConnectionString(ConnectionStringNames.Storage);
            string storageConnectionString = defaultStorageString;

            // If the connection string provides a hub name, that takes precedence. 
            // Note that connection strings *can't* specify a consumerGroup, so must always be passed in. 
            string actualPath = eventHubName;
            EventHubsConnectionStringBuilder sb = new EventHubsConnectionStringBuilder(receiverConnectionString);
            if (sb.EntityPath != null)
            {
                actualPath = sb.EntityPath;
                sb.EntityPath = null; // need to remove to use with EventProcessorHost
            }

            var @namespace = GetEventHubNamespace(sb);
            var blobPrefix = GetBlobPrefix(actualPath, @namespace);

            var host = _hosts.GetOrAdd((actualPath, sb.ToString(), consumerGroup), new EventProcessorHost(
                hostName: eventProcessorHostName,
                eventHubPath: actualPath,
                consumerGroupName: consumerGroup,
                eventHubConnectionString: sb.ToString(),
                storageConnectionString: storageConnectionString,
                leaseContainerName: LeaseContainerName,
                storageBlobPrefix: blobPrefix));
            return host;
        }

        private static string GetEventHubNamespace(EventHubsConnectionStringBuilder connectionString)
        {
            // EventHubs only have 1 endpoint. 
            var url = connectionString.Endpoint;
            var @namespace = url.Host;
            return @namespace;
        }

        public static string GetBlobPrefix(string eventHubName, string serviceBusNamespace)
        {
            if (eventHubName == null)
            {
                throw new ArgumentNullException("eventHubName");
            }
            if (serviceBusNamespace == null)
            {
                throw new ArgumentNullException("serviceBusNamespace");
            }

            string key = EscapeBlobPath(serviceBusNamespace) + "/" + EscapeBlobPath(eventHubName) + "/";
            return key;
        }

        // Escape a blob path.  
        // For diagnostics, we want human-readble strings that resemble the input. 
        // Inputs are most commonly alphanumeric with a fex extra chars (dash, underscore, dot). 
        // Escape character is a ':', which is also escaped. 
        // Blob names are case sensitive; whereas input is case insensitive, so normalize to lower.  
        private static string EscapeBlobPath(string path)
        {
            StringBuilder sb = new StringBuilder(path.Length);
            foreach (char c in path)
            {
                if (c >= 'a' && c <= 'z')
                {
                    sb.Append(c);
                }
                else if (c == '-' || c == '_' || c == '.')
                {
                    // Potentially common carahcters. 
                    sb.Append(c);
                }
                else if (c >= 'A' && c <= 'Z')
                {
                    sb.Append((char)(c - 'A' + 'a')); // ToLower
                }
                else if (c >= '0' && c <= '9')
                {
                    sb.Append(c);
                }
                else
                {
                    sb.Append(EscapeStorageCharacter(c));
                }
            }

            return sb.ToString();
        }

        private static string EscapeStorageCharacter(char character)
        {
            var ordinalValue = (ushort)character;
            if (ordinalValue < 0x100)
            {
                return string.Format(CultureInfo.InvariantCulture, ":{0:X2}", ordinalValue);
            }
            else
            {
                return string.Format(CultureInfo.InvariantCulture, "::{0:X4}", ordinalValue);
            }
        }

        private class ReceiverCreds
        {
            // Required.  
            public string EventHubConnectionString { get; set; }

            // Optional. If not found, use the stroage from JobHostConfiguration
            public string StorageConnectionString { get; set; }
        }
    }
}