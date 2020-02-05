// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    /// <summary>
    ///  A access token provider abstraction for validating access token provider.
    /// </summary>
    public interface IAccessTokenProvider
    {
        /// <summary>
        /// Validate access token from http request.
        /// </summary>
        /// <param name="request">Http request that sent to azure function</param>
        /// <returns></returns>
        AccessTokenResult ValidateToken(HttpRequest request);
    }
}