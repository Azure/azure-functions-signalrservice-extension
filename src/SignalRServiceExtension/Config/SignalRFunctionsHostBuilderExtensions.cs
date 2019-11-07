using System;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    public static class SignalRFunctionsHostBuilderExtensions
    {
        public static IFunctionsHostBuilder AddAuth(this IFunctionsHostBuilder builder, Action<TokenValidationParameters> configureTokenValidationParameters)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.AddSingleton<IAccessTokenProvider>(s =>
                new DefaultAccessTokenProvider(configureTokenValidationParameters));

            return builder;
        }

        public static IFunctionsHostBuilder AddAuth(this IFunctionsHostBuilder builder, IAccessTokenProvider accessTokenProvider)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.AddSingleton<IAccessTokenProvider>(s => accessTokenProvider);

            return builder;
        }
    }
}
