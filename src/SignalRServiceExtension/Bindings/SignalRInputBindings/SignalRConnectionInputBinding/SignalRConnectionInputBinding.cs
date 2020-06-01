// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal class SignalRConnectionInputBinding : BindingBase<SignalRConnectionInfoAttribute>
    {
        private const string HttpRequestName = "$request";
        private readonly ISecurityTokenValidator securityTokenValidator;
        private readonly ISignalRConnectionInfoConfigurer signalRConnectionInfoConfigurer;
        private readonly Type userType;

        public SignalRConnectionInputBinding(
            BindingProviderContext context,
            IConfiguration configuration,
            INameResolver nameResolver,
            ISecurityTokenValidator securityTokenValidator,
            ISignalRConnectionInfoConfigurer signalRConnectionInfoConfigurer) : base(context, configuration, nameResolver)
        {
            this.securityTokenValidator = securityTokenValidator;
            this.signalRConnectionInfoConfigurer = signalRConnectionInfoConfigurer;
            this.userType = context.Parameter.ParameterType;
        }

        protected override Task<IValueProvider> BuildAsync(SignalRConnectionInfoAttribute attrResolved,
            IReadOnlyDictionary<string, object> bindingData)
        {
            var azureSignalRClient = Utils.GetAzureSignalRClient(attrResolved.ConnectionStringSetting, attrResolved.HubName);

            if (!bindingData.ContainsKey(HttpRequestName) || securityTokenValidator == null)
            {
                var info = azureSignalRClient.GetClientConnectionInfo(attrResolved.UserId, attrResolved.IdToken,
                    attrResolved.ClaimTypeList);
                return Task.FromResult<IValueProvider>(new SignalRConnectionInfoValueProvider(info, userType, ""));
            }

            var request = bindingData[HttpRequestName] as HttpRequest;

            var tokenResult = securityTokenValidator.ValidateToken(request);

            if (tokenResult.Status != SecurityTokenStatus.Valid)
            {
                return Task.FromResult<IValueProvider>(new SignalRConnectionInfoValueProvider(null, userType, ""));
            }

            if (signalRConnectionInfoConfigurer == null)
            {
                var info = azureSignalRClient.GetClientConnectionInfo(attrResolved.UserId, attrResolved.IdToken,
                    attrResolved.ClaimTypeList);
                return Task.FromResult<IValueProvider>(new SignalRConnectionInfoValueProvider(info, userType, ""));
            }

            var signalRConnectionDetail = new SignalRConnectionDetail
            {
                UserId = attrResolved.UserId,
                Claims = azureSignalRClient.GetCustomClaims(attrResolved.IdToken, attrResolved.ClaimTypeList),
            };
            signalRConnectionInfoConfigurer.Configure(tokenResult, request, signalRConnectionDetail);
            var customizedInfo = azureSignalRClient.GetClientConnectionInfo(signalRConnectionDetail.UserId,
                signalRConnectionDetail.Claims);
            return Task.FromResult<IValueProvider>(new SignalRConnectionInfoValueProvider(customizedInfo, userType, ""));
        }
    }
}