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
        public IConfiguration Configuration { get; }

        internal readonly INameResolver nameResolver;

        private readonly ILogger logger;
        private readonly SignalROptions options;
        private readonly ILoggerFactory loggerFactory;
        private readonly ISecurityTokenValidator securityTokenValidator;
        private readonly ISignalRConnectionInfoConfigurer signalRConnectionInfoConfigurer;
        private readonly ISignalRTriggerDispatcher _dispatcher;

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
            Configuration = configuration;
            this.securityTokenValidator = securityTokenValidator;
            this.signalRConnectionInfoConfigurer = signalRConnectionInfoConfigurer;
            this._dispatcher = new SignalRTriggerDispatcher();
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

            StaticServiceHubContextStore.ServiceManagerStore = new ServiceManagerStore(options.AzureSignalRServiceTransportType, Configuration, loggerFactory);

            var url = context.GetWebhookHandler();
            logger.LogInformation($"Registered SignalR trigger Endpoint = {url?.GetLeftPart(UriPartial.Path)}");

            context.AddConverter<string, JObject>(JObject.FromObject)
                   .AddConverter<SignalRConnectionInfo, JObject>(JObject.FromObject)
                   .AddConverter<JObject, SignalRMessage>(input => input.ToObject<SignalRMessage>())
                   .AddConverter<JObject, SignalRGroupAction>(input => input.ToObject<SignalRGroupAction>());

            // Trigger binding rule
            var triggerBindingRule = context.AddBindingRule<SignalRTriggerAttribute>();
            triggerBindingRule.AddValidator(ValidateSignalRTriggerAttributeBinding);
            triggerBindingRule.BindToTrigger(new SignalRTriggerBindingProvider(_dispatcher));

            // Non-trigger binding rule
            var signalRConnectionInputBindingProvider = new SignalRConnectionInputBindingProvider(this, securityTokenValidator, signalRConnectionInfoConfigurer);

            var signalRConnectionInfoAttributeRule = context.AddBindingRule<SignalRConnectionInfoAttribute>();
            signalRConnectionInfoAttributeRule.AddValidator(ValidateSignalRConnectionInfoAttributeBinding);
            signalRConnectionInfoAttributeRule.Bind(signalRConnectionInputBindingProvider);

            var securityTokenValidationAttributeRule = context.AddBindingRule<SecurityTokenValidationAttribute>();
            securityTokenValidationAttributeRule.Bind(signalRConnectionInputBindingProvider);

            var signalRAttributeRule = context.AddBindingRule<SignalRAttribute>();
            signalRAttributeRule.AddValidator(ValidateSignalRAttributeBinding);
            signalRAttributeRule.BindToCollector<SignalROpenType>(typeof(SignalRCollectorBuilder<>), this);

            logger.LogInformation("SignalRService binding initialized");
        }

        public Task<HttpResponseMessage> ConvertAsync(HttpRequestMessage input, CancellationToken cancellationToken)
        {
            return _dispatcher.ExecuteAsync(input, cancellationToken);
        }

        public AzureSignalRClient GetAzureSignalRClient(string attributeConnectionString, string attributeHubName)
        {
            var connectionString = FirstOrDefault(attributeConnectionString, options.ConnectionString);
            var hubName = FirstOrDefault(attributeHubName, options.HubName);

            return new AzureSignalRClient(StaticServiceHubContextStore.ServiceManagerStore, connectionString, hubName);
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
                throw new InvalidOperationException(string.Format(ErrorMessages.EmptyConnectionStringErrorMessageFormat, attributeConnectionStringName));
            }
        }

        private void ValidateSignalRTriggerAttributeBinding(SignalRTriggerAttribute attribute, Type type)
        {
            ValidateConnectionString(attribute.ConnectionStringSetting,
                $"{nameof(SignalRTriggerAttribute)}.{nameof(SignalRConnectionInfoAttribute.ConnectionStringSetting)}");
            ValidateParameterNames(attribute.ParameterNames);
        }

        private void ValidateParameterNames(string[] parameterNames)
        {
            if (parameterNames == null || parameterNames.Length == 0)
            {
                return;
            }

            if (parameterNames.Length != parameterNames.Distinct(StringComparer.OrdinalIgnoreCase).Count())
            {
                throw new ArgumentException("Elements in ParameterNames should be ignore case unique.");
            }
        }

        private SignalRConnectionInfo GetClientConnectionInfo(SignalRConnectionInfoAttribute attribute)
        {
            var client = GetAzureSignalRClient(attribute.ConnectionStringSetting, attribute.HubName);
            return client.GetClientConnectionInfo(attribute.UserId, attribute.IdToken, attribute.ClaimTypeList);
        }

        private string FirstOrDefault(params string[] values)
        {
            return values.FirstOrDefault(v => !string.IsNullOrEmpty(v));
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