// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    [Extension("SignalR")]
    internal class SignalRConfigProvider : IExtensionConfigProvider
    {
        internal const string AzureSignalRConnectionStringName = "AzureSignalRConnectionString";

        private readonly SignalROptions options;
        private readonly INameResolver nameResolver;
        private static HttpClient httpClient = new HttpClient();
        private readonly ILogger logger;

        public SignalRConfigProvider(
            IOptions<SignalROptions> options, 
            INameResolver nameResolver, 
            ILoggerFactory loggerFactory)
        {
            this.options = options.Value;
            this.logger = loggerFactory.CreateLogger("SignalR");
            this.nameResolver = nameResolver;
        }
        
        public void Initialize(ExtensionConfigContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            if (string.IsNullOrEmpty(options.ConnectionString))
            {
                options.ConnectionString = nameResolver.Resolve(AzureSignalRConnectionStringName);
            }

            context.AddConverter<string, JObject>(JObject.FromObject)
                   .AddConverter<SignalRConnectionInfo, JObject>(JObject.FromObject)
                   .AddConverter<JObject, SignalRMessage>(input => input.ToObject<SignalRMessage>())
                   .AddConverter<JObject, SignalRGroupAction>(input => input.ToObject<SignalRGroupAction>());

            var signalRConnectionInfoAttributeRule = context.AddBindingRule<SignalRConnectionInfoAttribute>();
            signalRConnectionInfoAttributeRule.AddValidator(ValidateSignalRConnectionInfoAttributeBinding);
            signalRConnectionInfoAttributeRule.BindToInput<SignalRConnectionInfo>(GetClientConnectionInfo);

            var signalRAttributeRule = context.AddBindingRule<SignalRAttribute>();
            signalRAttributeRule.AddValidator(ValidateSignalRAttributeBinding);
            signalRAttributeRule.BindToCollector<SignalROpenType>(typeof(SignalRCollectorBuilder<>), this);
            
            logger.LogInformation("SignalRService binding initialized");
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
            var connectionString = FirstOrDefault(attributeConnectionString, options.ConnectionString);

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException(
                    $"The SignalR Service connection string must be set either via an '{AzureSignalRConnectionStringName}' app setting, via an '{AzureSignalRConnectionStringName}' environment variable, or directly in code via {nameof(SignalROptions)}.{nameof(SignalROptions.ConnectionString)} or {attributeConnectionStringName}.");
            }
        }

        private SignalRConnectionInfo GetClientConnectionInfo(SignalRConnectionInfoAttribute attribute)
        {
            var signalR = new AzureSignalRClient(attribute.ConnectionStringSetting, httpClient);
            var claims = attribute.GetClaims();
            return signalR.GetClientConnectionInfo(attribute.HubName, claims);
        }

        private string FirstOrDefault(params string[] values)
        {
            return values.FirstOrDefault(v => !string.IsNullOrEmpty(v));
        }

        internal AzureSignalRClient GetClient(SignalRAttribute attribute)
        {
            var connectionString = FirstOrDefault(attribute.ConnectionStringSetting, options.ConnectionString);
            var hubName = FirstOrDefault(attribute.HubName, options.HubName);
            return new AzureSignalRClient(connectionString, httpClient);
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