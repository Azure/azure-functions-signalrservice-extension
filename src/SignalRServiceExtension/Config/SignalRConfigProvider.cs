// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Azure.WebJobs.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    [Extension("SignalR", "signalr")]
    internal class SignalRConfigProvider : IExtensionConfigProvider, IAsyncConverter<HttpRequestMessage, HttpResponseMessage>
    {
        private readonly IConfiguration configuration;
        private readonly INameResolver nameResolver;
        private readonly ILogger logger;
        private readonly SignalROptions options;
        private readonly ILoggerFactory loggerFactory;
        private readonly ISignalRTriggerDispatcher _dispatcher;
        private readonly InputBindingProvider inputBindingProvider;

        public SignalRConfigProvider(
            IOptions<SignalROptions> options,
            INameResolver nameResolver,
            ILoggerFactory loggerFactory,
            IConfiguration configuration,
            ISecurityTokenValidator securityTokenValidator = null,
            ISignalRConnectionInfoConfigurer signalRConnectionInfoConfigurer = null)
        {
            this.options = options.Value;
            this.loggerFactory = loggerFactory;
            this.logger = loggerFactory.CreateLogger(LogCategories.CreateTriggerCategory("SignalR"));
            this.nameResolver = nameResolver;
            this.configuration = configuration;
            this._dispatcher = new SignalRTriggerDispatcher();
            inputBindingProvider = new InputBindingProvider(configuration, nameResolver, securityTokenValidator, signalRConnectionInfoConfigurer);
        }

        // GetWebhookHandler() need the Obsolete
        [Obsolete("preview")]
        public void Initialize(ExtensionConfigContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            if (string.IsNullOrEmpty(options.ConnectionString))
            {
                options.ConnectionString = nameResolver.Resolve(Constants.AzureSignalRConnectionStringName);
            }

            var serviceTransportTypeStr = nameResolver.Resolve(Constants.ServiceTransportTypeName);
            if (Enum.TryParse<ServiceTransportType>(serviceTransportTypeStr, out var transport))
            {
                options.AzureSignalRServiceTransportType = transport;
            }
            else
            {
                logger.LogWarning($"Unsupported service transport type: {serviceTransportTypeStr}. Use default {options.AzureSignalRServiceTransportType} instead.");
            }

            StaticServiceHubContextStore.ServiceManagerStore = new ServiceManagerStore(options.AzureSignalRServiceTransportType, configuration, loggerFactory);

            bool triggerEnabled = false;
            try
            {
                var url = context.GetWebhookHandler();
                logger.LogInformation($"Registered SignalR trigger Endpoint = {url?.GetLeftPart(UriPartial.Path)}");
                triggerEnabled = true;
            }
            catch(Exception ex)
            {
                logger.LogWarning("SignalR trigger requires 'AzureWebJobsStorage' connection string being set. All SignalR trigger functions will be suppressed. " + 
                    $"It's expected if you're using Azure Static Web Apps but not in other secnarios. {ex}");
            }
            
            context.AddConverter<string, JObject>(JObject.FromObject)
                   .AddConverter<SignalRConnectionInfo, JObject>(JObject.FromObject)
                   .AddConverter<JObject, SignalRMessage>(input => input.ToObject<SignalRMessage>())
                   .AddConverter<JObject, SignalRGroupAction>(input => input.ToObject<SignalRGroupAction>());

            // Trigger binding rule
            var triggerBindingRule = context.AddBindingRule<SignalRTriggerAttribute>();
            triggerBindingRule.AddConverter<InvocationContext, JObject>(JObject.FromObject);
            triggerBindingRule.BindToTrigger<InvocationContext>(new SignalRTriggerBindingProvider(_dispatcher, nameResolver, options, triggerEnabled));
                        
            // Non-trigger binding rule
            var signalRConnectionInfoAttributeRule = context.AddBindingRule<SignalRConnectionInfoAttribute>();
            signalRConnectionInfoAttributeRule.AddValidator(ValidateSignalRConnectionInfoAttributeBinding);
            signalRConnectionInfoAttributeRule.Bind(inputBindingProvider);

            var securityTokenValidationAttributeRule = context.AddBindingRule<SecurityTokenValidationAttribute>();
            securityTokenValidationAttributeRule.Bind(inputBindingProvider);

            var signalRAttributeRule = context.AddBindingRule<SignalRAttribute>();
            signalRAttributeRule.AddValidator(ValidateSignalRAttributeBinding);
            signalRAttributeRule.BindToCollector<SignalROpenType>(typeof(SignalRCollectorBuilder<>), options);

            logger.LogInformation("SignalRService binding initialized");
        }

        public Task<HttpResponseMessage> ConvertAsync(HttpRequestMessage input, CancellationToken cancellationToken)
        {
            return _dispatcher.ExecuteAsync(input, cancellationToken);
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
            var connectionString = Utils.FirstOrDefault(attributeConnectionString, options.ConnectionString);

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException(string.Format(ErrorMessages.EmptyConnectionStringErrorMessageFormat, attributeConnectionStringName));
            }
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