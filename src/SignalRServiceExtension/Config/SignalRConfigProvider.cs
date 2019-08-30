// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Extensions.SignalRService.TriggerBindings;
using System.Security.Claims;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    [Extension("SignalR", "signalr")]
    internal class SignalRConfigProvider : IExtensionConfigProvider, IAsyncConverter<HttpRequestMessage, HttpResponseMessage>
    {
        public IConfiguration Configuration { get; }

        private readonly SignalROptions options;
        private readonly INameResolver nameResolver;
        private readonly ILogger logger;
        private readonly ILoggerFactory loggerFactory;

        private readonly Dictionary<string, SignalRListener> _listeners = new Dictionary<string, SignalRListener>(StringComparer.OrdinalIgnoreCase);

        public SignalRConfigProvider(
            IOptions<SignalROptions> options,
            INameResolver nameResolver,
            ILoggerFactory loggerFactory,
            IConfiguration configuration)
        {
            this.options = options.Value;
            this.loggerFactory = loggerFactory;
            this.logger = loggerFactory.CreateLogger("SignalR");
            this.nameResolver = nameResolver;
            Configuration = configuration;
        }

        [Obsolete]
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
            logger.LogInformation($"Registered SignalR negotiate Endpoint = {url?.GetLeftPart(UriPartial.Path)}");

            context.AddConverter<string, JObject>(JObject.FromObject)
                   .AddConverter<SignalRConnectionInfo, JObject>(JObject.FromObject)
                   .AddConverter<JObject, SignalRMessage>(input => input.ToObject<SignalRMessage>())
                   .AddConverter<JObject, SignalRGroupAction>(input => input.ToObject<SignalRGroupAction>());

            // Trigger binding rule
            // TODO: Add more convert type
            context.AddBindingRule<SignalRTriggerAttribute>()
                .BindToTrigger<SignalRContext>(new SignalRTriggerBindingProvider(this));

            // Non-trigger binding rule
            var signalRConnectionInfoAttributeRule = context.AddBindingRule<SignalRConnectionInfoAttribute>();
            signalRConnectionInfoAttributeRule.AddValidator(ValidateSignalRConnectionInfoAttributeBinding);
            signalRConnectionInfoAttributeRule.BindToInput<SignalRConnectionInfo>(GetClientConnectionInfo);

            var signalRAttributeRule = context.AddBindingRule<SignalRAttribute>();
            signalRAttributeRule.AddValidator(ValidateSignalRAttributeBinding);
            signalRAttributeRule.BindToCollector<SignalROpenType>(typeof(SignalRCollectorBuilder<>), this);

            logger.LogInformation("SignalRService binding initialized");
        }

        internal void AddListener(string key, SignalRListener listener)
        {
            _listeners.Add(key, listener);
        }

        public async Task<HttpResponseMessage> ConvertAsync(HttpRequestMessage input, CancellationToken cancellationToken)
        {
            return await ProcessAsync(input);
        }

        private async Task<HttpResponseMessage> ProcessAsync(HttpRequestMessage req)
        {
            var path = req.RequestUri.AbsolutePath;
            if (TryGetHubName(path, out var hubName))
            {
                var signalRTriggerEvent = new SignalRTriggerEvent
                {
                    Context = new SignalRContext
                    {
                        HubName = hubName,
                        UserId = NegotiateUtils.GetUserId(req),
                        Claims = NegotiateUtils.GetClaims(req),
                    },
                    ContextTcs = new TaskCompletionSource<SignalRContext>(TaskCreationOptions.RunContinuationsAsynchronously),
                };

                // TODO: select out listener that match the pattern
                if (_listeners.TryGetValue(hubName, out var listener))
                {
                    var result = await listener.Executor.TryExecuteAsync(new Host.Executors.TriggeredFunctionData
                    {
                        TriggerValue = signalRTriggerEvent
                    }, CancellationToken.None);

                    var context = await signalRTriggerEvent.ContextTcs.Task;
                    var accessToken = GenerateClientAccessToken(req, serviceManager, context);
                    
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(JsonConvert.SerializeObject(new SignalRConnectionInfo()
                        {
                            Url = serviceManager.GetClientEndpoint(context.HubName),
                            AccessToken = accessToken,
                        }))
                    };
                }
                else
                {
                    return new HttpResponseMessage(HttpStatusCode.NotFound);
                }
            }
            else
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }
        }

        private string GenerateClientAccessToken(HttpRequestMessage req, IServiceManager serviceManager, SignalRContext context)
        {
            var claims = context.Claims?.Select(c => new Claim(c.Key, c.Value)).ToList();
            return serviceManager.GenerateClientAccessToken(context.HubName, context.UserId, NegotiateUtils.GetClaimsForJwtToken(req, claims, context.UserId));
        }

        private bool TryGetHubName(string path, out string hubName)
        {
            // The url should be /runtime/webhooks/signalr/{hub}/negotiate
            var paths = path.Split(new char[]{'/'}, StringSplitOptions.RemoveEmptyEntries);
            if (!ValidateNegotiateUri(paths))
            {
                hubName = null;
                return false;
            }

            hubName = paths[3];
            return true;
        }

        private bool ValidateNegotiateUri(string[] paths)
        {
            if (paths.Length != 5)
            {
                return false;
            }

            if (paths[2] != "signalr" || paths[4] != "negotiate")
            {
                return false;
            }

            return true;
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