// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    using SignalRConnectionInfoConfigureFunc = Func<AccessTokenResult, HttpRequest, SignalRConnectionDetail, SignalRConnectionDetail>;

    /// <summary>
    /// Extensions to add access token provider and SignalR connection configuration
    /// </summary>
    public static class SignalRFunctionsHostBuilderExtensions
    {
        /// <summary>
        /// Add default access token provider
        /// </summary>
        /// <param name="builder">Azure function host builder</param>
        /// <param name="configureTokenValidationParameters">Token validation parameters</param>
        /// <returns><see cref="IFunctionsHostBuilder"/>Azure function host builder</returns>
        public static IFunctionsHostBuilder AddAuth(this IFunctionsHostBuilder builder, Action<TokenValidationParameters> configureTokenValidationParameters)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configureTokenValidationParameters == null)
            {
                throw new ArgumentNullException(nameof(configureTokenValidationParameters));
            }

            builder.Services.AddSingleton<IAccessTokenProvider>(s =>
                new DefaultAccessTokenProvider(configureTokenValidationParameters));

            return builder;
        }

        /// <summary>
        /// Add default access token provider
        /// </summary>
        /// <param name="builder">Azure function host builder</param>
        /// <param name="configureTokenValidationParameters">Token validation parameters</param>
        /// <param name="configurer">SignalR connection configuration</param>
        /// <returns><see cref="IFunctionsHostBuilder"/>Azure function host builder</returns>
        public static IFunctionsHostBuilder AddAuth(this IFunctionsHostBuilder builder, Action<TokenValidationParameters> configureTokenValidationParameters, SignalRConnectionInfoConfigureFunc configurer)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configureTokenValidationParameters == null)
            {
                throw new ArgumentNullException(nameof(configureTokenValidationParameters));
            }

            if (configurer == null)
            {
                throw new ArgumentNullException(nameof(configurer));
            }

            var internalSignalRConnectionInfoConfigurer = new InternalSignalRConnectionInfoConfigurer(configurer);

            builder.Services
                .AddSingleton<IAccessTokenProvider>(s =>
                    new DefaultAccessTokenProvider(configureTokenValidationParameters))
                .AddSingleton<ISignalRConnectionInfoConfigurer>(s => internalSignalRConnectionInfoConfigurer);

            return builder;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="builder">Azure function host builder</param>
        /// <param name="accessTokenProvider">Access token provider that implements <see cref="IAccessTokenProvider"/></param>
        /// <param name="configurer">SignalR connection configuration</param>
        /// <returns><see cref="IFunctionsHostBuilder"/>Azure function host builder</returns>
        public static IFunctionsHostBuilder AddAuth(this IFunctionsHostBuilder builder, IAccessTokenProvider accessTokenProvider, SignalRConnectionInfoConfigureFunc configurer)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (accessTokenProvider == null)
            {
                throw new ArgumentNullException(nameof(accessTokenProvider));
            }

            if (configurer == null)
            {
                throw new ArgumentNullException(nameof(configurer));
            }

            var internalSignalRConnectionInfoConfigurer = new InternalSignalRConnectionInfoConfigurer(configurer);

            builder.Services
                .AddSingleton<IAccessTokenProvider>(s => accessTokenProvider)
                .AddSingleton<ISignalRConnectionInfoConfigurer>(s => internalSignalRConnectionInfoConfigurer);
            return builder;
        }
    }

    internal class InternalSignalRConnectionInfoConfigurer : ISignalRConnectionInfoConfigurer
    {
        public SignalRConnectionInfoConfigureFunc Configure { get; set; }

        public InternalSignalRConnectionInfoConfigurer(SignalRConnectionInfoConfigureFunc Configure)
        {
            this.Configure = Configure;
        }
    }
}