// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;

namespace FunctionApp
{
    public class Functions
    {
        /* sample:
           
           Request: send a request with user id "myuserid" in bearer token

           url (GET):  http://localhost:7071/api/negotiate
           headers: [
             "Authorization": "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaHR0cDovL3NjaGVtYXMueG1sc29hcC5vcmcvd3MvMjAwNS8wNS9pZGVudGl0eS9jbGFpbXMvbmFtZSI6Im15dXNlcmlkIiwiaWF0IjoxNTE2MjM5MDIyfQ.5GK9ykQfNGEz07VU_Lwd2QneT9gxEP44o7Zs1y63mcI",
             "myheader": "testclaim"
           ]

           Expected Response:
           {
                "url": "<YOUR ASRS ENDPOINT>/client/?hub=simplechat",
                "accessToken": "<payload contains "nameid": "myuserid" and "myheader": "testclaim">"
            }
        */
        [FunctionName("negotiate")]
        public SignalRConnectionInfo GetSignalRInfo(
            [HttpTrigger(AuthorizationLevel.Anonymous)] HttpRequest req,
            [SignalRConnectionInfo(HubName = Constants.HubName)] SignalRConnectionInfo connectionInfo) // todo: make HubName optional
        {
            return connectionInfo;
        }

        public static class Constants
        {
            public const string HubName = "simplechat";
        }
    }
}
