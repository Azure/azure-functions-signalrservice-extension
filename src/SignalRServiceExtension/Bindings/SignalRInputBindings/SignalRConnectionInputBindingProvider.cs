// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal class SignalRConnectionInputBindingProvider : IBindingProvider
    {
        private readonly ISecurityTokenValidator securityTokenValidator;
        private readonly SignalRConfigProvider signalRConfigProvider;
        private readonly ISignalRConnectionInfoConfigurer signalRConnectionInfoConfigurer;

        public SignalRConnectionInputBindingProvider(SignalRConfigProvider signalRConfigProvider, ISecurityTokenValidator securityTokenValidator, ISignalRConnectionInfoConfigurer signalRConnectionInfoConfigurer)
        {
            this.securityTokenValidator = securityTokenValidator;
            this.signalRConfigProvider = signalRConfigProvider;
            this.signalRConnectionInfoConfigurer = signalRConnectionInfoConfigurer;
        }

        public Task<IBinding> TryCreateAsync(BindingProviderContext context)
        {
            var parameterInfo = context.Parameter;
            foreach (var attr in parameterInfo.GetCustomAttributes())
            {
                switch (attr)
                {
                    case SignalRConnectionInfoAttribute connectionInfoAttribute:
                        var resolvedConnectionString = signalRConfigProvider.nameResolver.Resolve(connectionInfoAttribute.ConnectionStringSetting);
                        return Task.FromResult((IBinding)new SignalRConnectionInputBinding(connectionInfoAttribute, signalRConfigProvider.GetAzureSignalRClient(resolvedConnectionString, connectionInfoAttribute.HubName), securityTokenValidator, signalRConnectionInfoConfigurer));
                    case SecurityTokenValidationAttribute validationAttribute:
                        return Task.FromResult((IBinding) new SecurityTokenValidationInputBinding(securityTokenValidator));
                }
            }
            return Task.FromResult<IBinding>(null);
        }
    }
}