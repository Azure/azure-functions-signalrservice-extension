// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Protocols;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal class SignalRConnectionInputBinding : BindingBase<SignalRConnectionInfoAttribute>
    {
        private const string HttpRequestName = "$request";
        private readonly ISecurityTokenValidator securityTokenValidator;
        private readonly ISignalRConnectionInfoConfigurer signalRConnectionInfoConfigurer;

        public SignalRConnectionInputBinding(
            AttributeCloner<SignalRConnectionInfoAttribute> cloner,
            ParameterInfo param,
            ISecurityTokenValidator securityTokenValidator,
            ISignalRConnectionInfoConfigurer signalRConnectionInfoConfigurer) : base(cloner, param)
        {
            this.securityTokenValidator = securityTokenValidator;
            this.signalRConnectionInfoConfigurer = signalRConnectionInfoConfigurer;
        }

        protected override Task<IValueProvider> BuildAsync(SignalRConnectionInfoAttribute attrResolved,
            IReadOnlyDictionary<string, object> bindingData)
        {
            var azureSignalRClient = Utils.GetAzureSignalRClient(attrResolved.ConnectionStringSetting, attrResolved.HubName); // todo: add option

            var exactAttr = attrResolved as SignalRConnectionInfoAttribute;
            if (!bindingData.ContainsKey(HttpRequestName) || securityTokenValidator == null)
            {
                var info = azureSignalRClient.GetClientConnectionInfo(exactAttr.UserId, exactAttr.IdToken,
                    exactAttr.ClaimTypeList);
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
                var info = azureSignalRClient.GetClientConnectionInfo(exactAttr.UserId, exactAttr.IdToken,
                    exactAttr.ClaimTypeList);
                return Task.FromResult((IValueProvider)new SignalRValueProvider(info));
            }

            var signalRConnectionDetail = new SignalRConnectionDetail
            {
                UserId = exactAttr.UserId,
                Claims = azureSignalRClient.GetCustomClaims(exactAttr.IdToken, exactAttr.ClaimTypeList),
            };
            signalRConnectionInfoConfigurer.Configure(tokenResult, request, signalRConnectionDetail);
            var customizedInfo = azureSignalRClient.GetClientConnectionInfo(signalRConnectionDetail.UserId,
                signalRConnectionDetail.Claims);
            return Task.FromResult((IValueProvider)new SignalRValueProvider(customizedInfo));
        }
    }
}