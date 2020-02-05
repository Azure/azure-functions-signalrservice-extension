// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    public class SignalRConnectionInfoV2
    {
        [JsonProperty("negotiateResponse")]
        public SignalRConnectionInfo NegotiateResponse;

        [JsonProperty("accessTokenResult")]
        public AccessTokenResult AccessTokenResult;

        public SignalRConnectionInfoV2(SignalRConnectionInfo negotiateResponse, AccessTokenResult accessTokenResult)
        {
            NegotiateResponse = negotiateResponse;
            AccessTokenResult = accessTokenResult;
        }
    }
}