// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Azure.WebJobs.Host;

namespace FunctionApp
{
    [Obsolete]
    public class Functions : IFunctionInvocationFilter
    {
        public IAccessTokenProvider TokenProvider { get; }

        public Functions(IAccessTokenProvider tokenProvider)
        {
            TokenProvider = tokenProvider;
        }

        /* sample:
           
           Request: send a request with user id "myuserid" in bearer token

           url (GET):  http://localhost:7071/api/negotiate
           headers: [
             "Authorization": "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaHR0cDovL3NjaGVtYXMueG1sc29hcC5vcmcvd3MvMjAwNS8wNS9pZGVudGl0eS9jbGFpbXMvbmFtZSI6Im15dXNlcmlkIiwiaWF0IjoxNTE2MjM5MDIyfQ.5GK9ykQfNGEz07VU_Lwd2QneT9gxEP44o7Zs1y63mcI"
           ]

           Expected Response:
           {
                "url": "<YOUR ASRS ENDPOINT>/client/?hub=simplechat",
                "accessToken": "<payload contains "nameid": "myuserid">"
            }
        */
        [FunctionName("negotiate")]
        public SignalRConnectionInfo GetSignalRInfo(
            [HttpTrigger(AuthorizationLevel.Anonymous)] HttpRequest req,
            [SignalRConnectionInfo(HubName = Constants.HubName)] SignalRConnectionInfo connectionInfo) // todo: make HubName optional
        {
            return connectionInfo;
        }

        public Task OnExecutedAsync(FunctionExecutedContext executedContext, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        // gives customer a change to validate Function access token and add custom claims for ASRS access token.
        // OnExecutingAsync runs before negotiate function but after arguments binding
        public Task OnExecutingAsync(FunctionExecutingContext executingContext, CancellationToken cancellationToken)
        {
            var serviceManager = StaticServiceHubContextStore.Get().ServiceManager;
            var req = (HttpRequest)executingContext.Arguments["req"];
            var connectionInfo = (SignalRConnectionInfo)executingContext.Arguments["connectionInfo"];

            // validate token
            var result = TokenProvider.ValidateToken(req);
            
            if (result.Status == AccessTokenStatus.Valid)
            {
                // resolve the identity
                string identity = result.Principal.Identity.Name;

                // override connectionInfo argument
                connectionInfo.AccessToken = serviceManager.GenerateClientAccessToken(Constants.HubName, identity);
            }
            else
            {
                connectionInfo.AccessToken = "Error while validating negotiate function token"; // todo: return with detailed error message
            }

            return Task.CompletedTask;
        }

        private static string GetBase64EncodedString(string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return source;
            }

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(source));
        }

        public static class Constants
        {
            public const string HubName = "simplechat";
        }
    }
}
