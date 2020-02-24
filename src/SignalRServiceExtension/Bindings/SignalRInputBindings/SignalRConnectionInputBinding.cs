// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Protocols;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal class SignalRConnectionInputBinding : IBinding
    {
        private const string HttpRequestName = "$request";
        private readonly SignalRConnectionInfoAttribute attribute;
        private readonly ISecurityTokenValidator securityTokenValidator;
        private readonly AzureSignalRClient azureSignalRClient;
        private readonly ISignalRConnectionInfoConfigurer signalRConnectionInfoConfigurer;

        public bool FromAttribute => true;

        public SignalRConnectionInputBinding(SignalRConnectionInfoAttribute attribute, AzureSignalRClient azureSignalRClient, ISecurityTokenValidator securityTokenValidator, ISignalRConnectionInfoConfigurer signalRConnectionInfoConfigurer)
        {
            this.securityTokenValidator = securityTokenValidator;
            this.azureSignalRClient = azureSignalRClient;
            this.attribute = attribute;
            this.signalRConnectionInfoConfigurer = signalRConnectionInfoConfigurer;
        }

        public Task<IValueProvider> BindAsync(object value, ValueBindingContext context)
        {
            var bindingData = ((BindingContext)value).BindingData;

            if (!bindingData.ContainsKey(HttpRequestName) || securityTokenValidator == null)
            {
                var info = azureSignalRClient.GetClientConnectionInfo(attribute.UserId, attribute.IdToken, attribute.ClaimTypeList);
                return Task.FromResult((IValueProvider)new SignalRValueProvider(info));
            }

            var request = bindingData[HttpRequestName] as HttpRequest;

            var tokenResult = securityTokenValidator.ValidateToken(request);

            if (tokenResult.Status != SecurityTokenStatus.Valid)
            {
                return Task.FromResult((IValueProvider)new SignalRValueProvider(null));
            }

            if (signalRConnectionInfoConfigurer == null)
            {
                var info = azureSignalRClient.GetClientConnectionInfo(attribute.UserId, attribute.IdToken, attribute.ClaimTypeList);
                return Task.FromResult((IValueProvider)new SignalRValueProvider(info));
            }

            var signalRConnectionDetail = new SignalRConnectionDetail
            {
                UserId = attribute.UserId,
                Claims = azureSignalRClient.GetCustomClaims(attribute.IdToken, attribute.ClaimTypeList),
            };
            signalRConnectionInfoConfigurer.Configure(tokenResult, request, signalRConnectionDetail);
            var customizedInfo = azureSignalRClient.GetClientConnectionInfo(signalRConnectionDetail.UserId, signalRConnectionDetail.Claims);
            return Task.FromResult((IValueProvider)new SignalRValueProvider(customizedInfo));
        }

        public Task<IValueProvider> BindAsync(BindingContext context)
        {
            return BindAsync(context, null);
        }

        public ParameterDescriptor ToParameterDescriptor()
        {
            return new ParameterDescriptor();
        }
    }
}