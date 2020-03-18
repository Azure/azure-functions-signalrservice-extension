// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Protocols;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal class SignalRConnectionInputBinding <TAttribute>: BindingBase<TAttribute> where TAttribute : Attribute
    {
        private const string HttpRequestName = "$request";
        private readonly ISecurityTokenValidator securityTokenValidator;
        private readonly AzureSignalRClient azureSignalRClient;
        private readonly ISignalRConnectionInfoConfigurer signalRConnectionInfoConfigurer;

        public SignalRConnectionInputBinding(
            AttributeCloner<TAttribute> cloner, 
            ParameterDescriptor param, 
            AzureSignalRClient azureSignalRClient, 
            ISecurityTokenValidator securityTokenValidator, 
            ISignalRConnectionInfoConfigurer signalRConnectionInfoConfigurer) : base(cloner, param)
        {
            this.securityTokenValidator = securityTokenValidator;
            this.azureSignalRClient = azureSignalRClient;
            this.signalRConnectionInfoConfigurer = signalRConnectionInfoConfigurer;
        }

        public Task<IValueProvider> BindAsync(object value, ValueBindingContext context)
        {
            var bindingData = ((BindingContext)value).BindingData;

            if (!bindingData.ContainsKey(HttpRequestName) || securityTokenValidator == null)
            {
                var info = azureSignalRClient.GetClientConnectionInfo(cloner.UserId, cloner.IdToken, cloner.ClaimTypeList);
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
                var info = azureSignalRClient.GetClientConnectionInfo(cloner.UserId, cloner.IdToken, cloner.ClaimTypeList);
                return Task.FromResult((IValueProvider)new SignalRValueProvider(info));
            }

            var signalRConnectionDetail = new SignalRConnectionDetail
            {
                UserId = cloner.UserId,
                Claims = azureSignalRClient.GetCustomClaims(cloner.IdToken, cloner.ClaimTypeList),
            };
            signalRConnectionInfoConfigurer.Configure(tokenResult, request, signalRConnectionDetail);
            var customizedInfo = azureSignalRClient.GetClientConnectionInfo(signalRConnectionDetail.UserId, signalRConnectionDetail.Claims);
            return Task.FromResult((IValueProvider)new SignalRValueProvider(customizedInfo));
        }

        public Task<IValueProvider> BindAsync(BindingContext context)
        {
            return BindAsync(context, null);
        }

        protected override Task<IValueProvider> BuildAsync(TAttribute attrResolved, ValueBindingContext context)
        {
            throw new NotImplementedException();
        }
    }
}