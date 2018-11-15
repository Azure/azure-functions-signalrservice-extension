// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Extensions.SignalRService.Protocols;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Azure.WebJobs.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    [Extension("SignalR")]
    internal class SignalRConfigProvider : IExtensionConfigProvider
    {
        internal const string AzureSignalRConnectionStringName = "AzureSignalRConnectionString";

        public IConfiguration Config;
        private readonly SignalROptions _options;
        private readonly INameResolver _nameResolver;
        private static readonly HttpClient HttpClient = new HttpClient();
        private readonly ILogger _logger;

        public SignalRConfigProvider(
            IConfiguration config,
            IOptions<SignalROptions> options, 
            INameResolver nameResolver, 
            ILoggerFactory loggerFactory)
        {
            Config = config;
            _options = options.Value;
            _logger = loggerFactory.CreateLogger("SignalR");
            _nameResolver = nameResolver;
        }
        
        public void Initialize(ExtensionConfigContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            if (string.IsNullOrEmpty(_options.ConnectionString))
            {
                _options.ConnectionString = _nameResolver.Resolve(AzureSignalRConnectionStringName);
            }

            context.AddConverter<string, JObject>(JObject.FromObject)
                   .AddConverter<SignalRConnectionInfo, JObject>(JObject.FromObject)
                   .AddConverter<JObject, SignalRMessage>(input => input.ToObject<SignalRMessage>())
                   .AddConverter<JObject, SignalRGroupAction>(input => input.ToObject<SignalRGroupAction>());

            // Register binding provider
            var signalRConnectionInfoAttributeRule = context.AddBindingRule<SignalRConnectionInfoAttribute>();
            signalRConnectionInfoAttributeRule.AddValidator(ValidateSignalRConnectionInfoAttributeBinding);
            signalRConnectionInfoAttributeRule.BindToInput<SignalRConnectionInfo>(GetClientConnectionInfo);

            var signalRAttributeRule = context.AddBindingRule<SignalRAttribute>();
            signalRAttributeRule.AddValidator(ValidateSignalRAttributeBinding);
            signalRAttributeRule.BindToCollector<SignalROpenType>(typeof(SignalRCollectorBuilder<>), this);

            // Register trigger binding provider
            var triggerBindingProvider = new SignalRTriggerAttributeBindingProvider(Config, _nameResolver, _options,_logger);
            var rule = context.AddBindingRule<SignalRInvocationMessageTriggerAttribute>();
            rule.BindToTrigger<SignalRExtensionMessage>(triggerBindingProvider);
            rule.AddConverter<string, SignalRExtensionMessage>(str => JsonConvert.DeserializeObject<SignalRExtensionMessage>(str));
            rule.AddConverter<SignalRExtensionMessage, string>(input => Encoding.UTF8.GetString(input.Body.Array));
            rule.AddConverter<SignalRExtensionMessage, JObject>(input => JObject.FromObject(input.Body));

            _logger.LogInformation("SignalRService binding initialized");
        }

        private void ValidateSignalRAttributeBinding(SignalRAttribute attribute, Type type)
        {
            ValidateConnectionString(
                attribute.ConnectionStringSetting, 
                $"{nameof(SignalRAttribute)}.{nameof(SignalRAttribute.ConnectionStringSetting)}");
        }

        private void ValidateSignalRConnectionInfoAttributeBinding(SignalRConnectionInfoAttribute attribute, Type type)
        {
            ValidateConnectionString(
                attribute.ConnectionStringSetting, 
                $"{nameof(SignalRConnectionInfoAttribute)}.{nameof(SignalRConnectionInfoAttribute.ConnectionStringSetting)}");
        }

        private void ValidateConnectionString(string attributeConnectionString, string attributeConnectionStringName)
        {
            var connectionString = FirstOrDefault(attributeConnectionString, _options.ConnectionString);

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException(
                    $"The SignalR Service connection string must be set either via an '{AzureSignalRConnectionStringName}' app setting, via an '{AzureSignalRConnectionStringName}' environment variable, or directly in code via {nameof(SignalROptions)}.{nameof(SignalROptions.ConnectionString)} or {attributeConnectionStringName}.");
            }
        }

        private SignalRConnectionInfo GetClientConnectionInfo(SignalRConnectionInfoAttribute attribute)
        {
            var signalR = new AzureSignalRClient(attribute.ConnectionStringSetting, HttpClient);
            var claims = attribute.GetClaims();
            return signalR.GetClientConnectionInfo(attribute.HubName, claims);
        }

        private string FirstOrDefault(params string[] values)
        {
            return values.FirstOrDefault(v => !string.IsNullOrEmpty(v));
        }

        internal AzureSignalRClient GetClient(SignalRAttribute attribute)
        {
            var connectionString = FirstOrDefault(attribute.ConnectionStringSetting, _options.ConnectionString);
            var hubName = FirstOrDefault(attribute.HubName, _options.HubName);
            return new AzureSignalRClient(connectionString, HttpClient);
        }

        private class SignalROpenType : OpenType.Poco
        {
            public override bool IsMatch(Type type, OpenTypeMatchContext context)
            {
                if (type.IsGenericType
                    && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    return false;
                }

                if (type.FullName == "System.Object")
                {
                    return true;
                }

                return base.IsMatch(type, context);
            }
        }
    }
}