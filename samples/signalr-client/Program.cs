// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Azure.SignalR;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;

namespace SignalRClient
{
    static class Program
    {
        private const string Hub = "simplechat";
        private static string[] Targets = { "newMessage", "Target" };
        private const string SectionName = "AzureSignalRConnectionString";
        static void Main(string[] args)
        {
            var app = new CommandLineApplication
            {
                FullName = "Azure SignalR Management Sample: SignalR Client Tool"
            };
            var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
            var serviceEndpoints = configuration.GetEndpoints(SectionName).ToArray();

            app.OnExecute(async () =>
            {
                Console.WriteLine($"{serviceEndpoints.Length} service endpoints loaded.");
                foreach (var endpoint in serviceEndpoints)
                {
                    Console.WriteLine(endpoint.ToString());
                }

                var connections = serviceEndpoints.Select(e => CreateHubConnection($"user connected to {e.Endpoint}", e.ConnectionString));

                await Task.WhenAll(from conn in connections
                                   select conn.StartAsync());

                Console.WriteLine($"{serviceEndpoints.Length} Client(s) started...");
                Console.ReadLine();

                await Task.WhenAll(from conn in connections
                                   select conn.StopAsync());
                return 0;
            });

            app.Execute(args);
        }



        static HubConnection CreateHubConnection(string userId, string connectionString)
        {
            var serviceManager = new ServiceManagerBuilder().WithOptions(o => o.ConnectionString = connectionString).Build();
            var connection = new HubConnectionBuilder().WithUrl(serviceManager.GetClientEndpoint(Hub), Options =>
            {
                Options.AccessTokenProvider = () => Task.FromResult(serviceManager.GenerateClientAccessToken(Hub));
            })
            .Build();
            foreach (var target in Targets)
            {
                connection.On(target, (object message) => Console.WriteLine($"{userId}: gets message from service: '{message}'"));
            }


            connection.Closed += ex =>
            {
                Console.WriteLine(ex);
                Environment.Exit(1);
                return Task.CompletedTask;
            };

            return connection;
        }

        /// <param name="configuration"></param>
        /// <param name="sectionName"></param>
        public static IEnumerable<ServiceEndpoint> GetEndpoints(this IConfiguration configuration, string sectionName)
        {
            if (configuration[sectionName] != null)
            {
                yield return new ServiceEndpoint(configuration[sectionName]);
            }
            var suffixedEndpoints = configuration.GetSection(sectionName).AsEnumerable(true)
                               .Where(entry => !string.IsNullOrEmpty(entry.Value))
                               .Select(entry => new ServiceEndpoint(entry.Key, entry.Value));
            foreach (var endpoint in suffixedEndpoints)
            {
                yield return endpoint;
            }
        }
    }
}