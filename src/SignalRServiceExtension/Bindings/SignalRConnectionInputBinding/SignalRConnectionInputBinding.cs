// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Protocols;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal class SignalRConnectionInputBinding : IBinding
    {
        private const string HttpRequestName = "$request";
        private readonly SignalRConnectionInfoV2Attribute attribute;
        private readonly IAccessTokenProvider accessTokenProvider;
        private readonly AzureSignalRClient azureSignalRClient;
        private readonly ISignalRConnectionInfoConfigurer signalRConnectionInfoConfigurer;

        public SignalRConnectionInputBinding(SignalRConnectionInfoV2Attribute attribute, AzureSignalRClient azureSignalRClient, IAccessTokenProvider accessTokenProvider, ISignalRConnectionInfoConfigurer signalRConnectionInfoConfigurer)
        {
            this.accessTokenProvider = accessTokenProvider;
            this.azureSignalRClient = azureSignalRClient;
            this.attribute = attribute;
            this.signalRConnectionInfoConfigurer = signalRConnectionInfoConfigurer;
        }

        public Task<IValueProvider> BindAsync(object value, ValueBindingContext context)
        {
            var request = ((BindingContext)value).BindingData[HttpRequestName] as HttpRequest;

            if (request == null)
            {
                throw new NotSupportedException();
            }

            if (accessTokenProvider == null)
            {
                var info = azureSignalRClient.GetClientConnectionInfo(attribute.UserId, attribute.IdToken, attribute.ClaimTypeList);
                return Task.FromResult((IValueProvider)new SignalRValueProvider(info));
            }

            var tokenResult = accessTokenProvider.ValidateToken(request);

            if (tokenResult.Status != AccessTokenStatus.Valid)
            {
                return Task.FromResult((IValueProvider)new SignalRValueProvider(new SignalRConnectionInfoV2(null, tokenResult.Exception)));
            }

            if (signalRConnectionInfoConfigurer == null)
            {
                var info = azureSignalRClient.GetClientConnectionInfoV2(attribute.UserId, attribute.IdToken, attribute.ClaimTypeList);
                return Task.FromResult((IValueProvider)new SignalRValueProvider(info));
            }

            var signalRConnectionDetail = new SignalRConnectionDetail
            {
                UserId = attribute.UserId,
                Claims = azureSignalRClient.GetCustomerClaims(attribute.IdToken, attribute.ClaimTypeList),
            };
            signalRConnectionInfoConfigurer.Configure(tokenResult, request, signalRConnectionDetail);
            var customizedInfo = azureSignalRClient.GetClientConnectionInfoV2(signalRConnectionDetail.UserId, signalRConnectionDetail.Claims);
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

        public bool FromAttribute { get; }
    }

    internal class SignalRValueProvider : IValueProvider
    {
        private object value;

        public SignalRValueProvider(object value)
        {
            this.value = value;
        }

        public Task<object> GetValueAsync()
        {
            return Task.FromResult(value);
        }

        public string ToInvokeString()
        {
            return value.ToString();
        }

        public Type Type { get; }
    }
}