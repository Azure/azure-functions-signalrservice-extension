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
        private readonly SignalRConnectionInfoAttribute attribute;
        private readonly IAccessTokenProvider accessTokenProvider;
        private readonly AzureSignalRClient azureSignalRClient;
        private readonly ISignalRConnectionInfoConfigurer signalRConnectionInfoConfigurer;

        public SignalRConnectionInputBinding(SignalRConnectionInfoAttribute attribute, AzureSignalRClient azureSignalRClient, IAccessTokenProvider accessTokenProvider, ISignalRConnectionInfoConfigurer signalRConnectionInfoConfigurer)
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

            if (signalRConnectionInfoConfigurer == null)
            {
                // todo: add callback
                if (tokenResult.Status == AccessTokenStatus.Valid)
                {
                    var info = azureSignalRClient.GetClientConnectionInfo(attribute.UserId, attribute.IdToken, attribute.ClaimTypeList);
                    return Task.FromResult((IValueProvider)new SignalRValueProvider(info));
                }
                else
                {
                    var info = new SignalRConnectionInfo
                    {
                        Url = null,
                        AccessToken = "Error while validating negotiate function token",
                    };
                    return Task.FromResult((IValueProvider)new SignalRValueProvider(info));
                }
            }

            var signalRConnectionDetail = new SignalRConnectionDetail
            {
                UserId = attribute.UserId,
                Claims = azureSignalRClient.GetCustomerClaims(attribute.IdToken, attribute.ClaimTypeList),
            };
            signalRConnectionInfoConfigurer.Configure(tokenResult, request, signalRConnectionDetail);
            if (signalRConnectionDetail.Error != null)
            {
                var info = new SignalRConnectionInfo
                {
                    Url = null,
                    AccessToken = "Error while validating negotiate function token",
                };
                return Task.FromResult((IValueProvider)new SignalRValueProvider(info));
            }
            var signalRConnectionInfo = azureSignalRClient.GetClientConnectionInfo(signalRConnectionDetail.UserId, signalRConnectionDetail.Claims);
            return Task.FromResult((IValueProvider)new SignalRValueProvider(signalRConnectionInfo));
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
