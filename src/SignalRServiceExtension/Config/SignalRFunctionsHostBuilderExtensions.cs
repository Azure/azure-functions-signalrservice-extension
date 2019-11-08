using System;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    // todo: add more DI
    public static class SignalRFunctionsHostBuilderExtensions
    {
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

        public static IFunctionsHostBuilder AddAuth(this IFunctionsHostBuilder builder, Action<TokenValidationParameters> configureTokenValidationParameters, Action<AccessTokenResult, HttpRequest, SignalRConnectionDetail> configurer)
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

        public static IFunctionsHostBuilder AddAuth(this IFunctionsHostBuilder builder, IAccessTokenProvider accessTokenProvider, Action<AccessTokenResult, HttpRequest, SignalRConnectionDetail> configurer)
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

    internal class InternalSignalRConnectionInfoConfigurer: ISignalRConnectionInfoConfigurer
    {
        public Action<AccessTokenResult, HttpRequest, SignalRConnectionDetail> Configure { get; set; }

        public InternalSignalRConnectionInfoConfigurer(Action<AccessTokenResult, HttpRequest, SignalRConnectionDetail> Configure)
        {
            this.Configure = Configure;
        }
    }
}
