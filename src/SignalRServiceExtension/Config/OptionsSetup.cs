// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal class OptionsSetup : IConfigureOptions<ServiceManagerOptions>, IOptionsChangeTokenSource<ServiceManagerOptions>
    {
        private readonly IConfiguration _configuration;
        private readonly string _ConnectionStringKey;
        private readonly ILogger _logger;

        public OptionsSetup(IConfiguration configuration, ILoggerFactory loggerFactory, string connectionStringKey)
        {
            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            if (string.IsNullOrWhiteSpace(connectionStringKey))
            {
                throw new ArgumentException($"'{nameof(connectionStringKey)}' cannot be null or whitespace", nameof(connectionStringKey));
            }

            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _ConnectionStringKey = connectionStringKey;
            _logger = loggerFactory.CreateLogger<OptionsSetup>();
        }

        public string Name => Options.DefaultName;

        public void Configure(ServiceManagerOptions options)
        {
            options.ConnectionString = _configuration[_ConnectionStringKey];
            var serviceTransportTypeStr = _configuration[Constants.ServiceTransportTypeName];
            if (Enum.TryParse<ServiceTransportType>(serviceTransportTypeStr, out var transport))
            {
                options.ServiceTransportType = transport;
            }
            else
            {
                options.ServiceTransportType = ServiceTransportType.Transient;
                _logger.LogWarning($"Unsupported service transport type: {serviceTransportTypeStr}. Use default {ServiceTransportType.Transient} instead.");
            }
            options.ConnectionCount = 3;//make connection more stable
        }

        public IChangeToken GetChangeToken()
        {
            return _configuration.GetReloadToken();
        }
    }
}