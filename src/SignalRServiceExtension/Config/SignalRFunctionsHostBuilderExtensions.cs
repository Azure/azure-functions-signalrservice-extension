using System;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    using SignalRConnectionInfoConfigureFunc = Func<AccessTokenResult, HttpRequest, SignalRConnectionDetail, SignalRConnectionDetail>;

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

    internal class InternalSignalRConnectionInfoConfigurer: ISignalRConnectionInfoConfigurer
    {
        public SignalRConnectionInfoConfigureFunc Configure { get; set; }

        public InternalSignalRConnectionInfoConfigurer(SignalRConnectionInfoConfigureFunc Configure)
        {
            this.Configure = Configure;
        }
    }
}
