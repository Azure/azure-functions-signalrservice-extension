using System;
using FunctionApp;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

[assembly: FunctionsStartup(typeof(Startup))]
namespace FunctionApp
{
    /// <summary>
    /// Runs when the Azure Functions host starts. Microsoft.NET.Sdk.Functions package version 1.0.28 or later
    /// </summary>
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            // Get the configuration files for the OAuth token issuer
            //var issuerToken = Environment.GetEnvironmentVariable("IssuerToken");

            // only for sample
            var issuerToken = "bXlmdW5jdGlvbmF1dGh0ZXN0"; // base64 encoded for "myfunctionauthtest";

            // Register the access token provider as a singleton, customer can register one's own
            builder.Services.AddSingleton<IAccessTokenProvider>(s => new DefaultAccessTokenProvider(parameters =>
            {
                parameters.IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(issuerToken));
                // for sample only
                parameters.RequireSignedTokens = false;
                parameters.ValidateAudience = false;
                parameters.ValidateIssuer = false;
                parameters.ValidateIssuerSigningKey = false;
                parameters.ValidateLifetime = false;
            }));
        }
    }
}