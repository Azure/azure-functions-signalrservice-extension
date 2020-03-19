// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    // this input binding provider doesn't support converter and pattern matcher
    internal class InputBindingProvider : IBindingProvider
    {
        private readonly ISecurityTokenValidator securityTokenValidator;
        private readonly ISignalRConnectionInfoConfigurer signalRConnectionInfoConfigurer;
        private readonly INameResolver nameResolver;
        private readonly IConfiguration configuration;

        public InputBindingProvider(IConfiguration configuration, INameResolver nameResolver, SignalROptions options, ISecurityTokenValidator securityTokenValidator, ISignalRConnectionInfoConfigurer signalRConnectionInfoConfigurer)
        {
            this.configuration = configuration;
            this.nameResolver = nameResolver;
            this.securityTokenValidator = securityTokenValidator;
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
                        var attributeSource = TypeUtility.GetResolvedAttribute<SignalRConnectionInfoAttribute>(parameterInfo);
                        var cloner = new AttributeCloner<SignalRConnectionInfoAttribute>(attributeSource, context.BindingDataContract, configuration, nameResolver);
                        return Task.FromResult((IBinding)new SignalRConnectionInputBinding(cloner, parameterInfo, securityTokenValidator, signalRConnectionInfoConfigurer));
                    case SecurityTokenValidationAttribute validationAttribute:
                        return Task.FromResult((IBinding) new SecurityTokenValidationInputBinding(securityTokenValidator));
                }
            }
            return Task.FromResult<IBinding>(null);
        }
    }
}