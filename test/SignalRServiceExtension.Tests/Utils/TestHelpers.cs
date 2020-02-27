// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.WebApiCompatShim;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SignalRServiceExtension.Tests.Utils
{
    internal static class TestHelpers
    {
        public static IHost NewHost(Type type, SignalRConfigProvider ext = null, Dictionary<string, string> configuration = null, ILoggerProvider loggerProvider = null)
        {
            var builder = new HostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddSingleton<ITypeLocator>(new FakeTypeLocator(type));
                    if (ext != null)
                    {
                        services.AddSingleton<IExtensionConfigProvider>(ext);
                    }
                    services.AddSingleton<IExtensionConfigProvider>(new TestExtensionConfig());
                })
                .ConfigureWebJobs(webJobsBuilder =>
                {
                    webJobsBuilder.AddSignalR();
                    webJobsBuilder.UseHostId(Guid.NewGuid().ToString("n"));
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddProvider(loggerProvider);
                });

            if (configuration != null)
            {
                builder.ConfigureAppConfiguration(b =>
                {
                    b.AddInMemoryCollection(configuration);
                });
            }

            return builder.Build();
        }

        public static JobHost GetJobHost(this IHost host)
        {
            return host.Services.GetService<IJobHost>() as JobHost;
        }

        public static HttpRequestMessage CreateHttpRequestMessage(string hub, string category, string @event, string connectionId, 
            string contentType = Constants.JsonContentType)
        {
            var context = new DefaultHttpContext();
            context.Request.ContentType = contentType;
            context.Request.Method = "Post";
            context.Request.Headers.Add(Constants.AsrsHubNameHeader, hub);
            context.Request.Headers.Add(Constants.AsrsCategory, category);
            context.Request.Headers.Add(Constants.AsrsEvent, @event);
            context.Request.Headers.Add(Constants.AsrsConnectionIdHeader, connectionId);

            return new HttpRequestMessageFeature(context).HttpRequestMessage;
        }
    }
}