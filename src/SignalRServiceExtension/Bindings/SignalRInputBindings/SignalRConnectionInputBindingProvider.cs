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
        private readonly SignalROptions options;
        private readonly ISignalRConnectionInfoConfigurer signalRConnectionInfoConfigurer;
        private readonly INameResolver nameResolver;

        public SignalRConnectionInputBindingProvider(INameResolver nameResolver, SignalROptions options, ISecurityTokenValidator securityTokenValidator, ISignalRConnectionInfoConfigurer signalRConnectionInfoConfigurer)
        {
            this.nameResolver = nameResolver;
            this.securityTokenValidator = securityTokenValidator;
            this.options = options;
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
                        var resolvedConnectionString = nameResolver.Resolve(connectionInfoAttribute.ConnectionStringSetting);
                        return Task.FromResult((IBinding)new SignalRConnectionInputBinding(connectionInfoAttribute, Utils.GetAzureSignalRClient(resolvedConnectionString, connectionInfoAttribute.HubName, options), securityTokenValidator, signalRConnectionInfoConfigurer));
                    case SecurityTokenValidationAttribute validationAttribute:
                        return Task.FromResult((IBinding) new SecurityTokenValidationInputBinding(securityTokenValidator));
                }
            }
            return Task.FromResult<IBinding>(null);
        }
    }
}