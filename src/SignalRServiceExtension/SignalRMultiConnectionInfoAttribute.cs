// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Azure.WebJobs.Description;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    /// <summary>
    /// An input binding attribute to bind a list of <see cref="EndpointConnectionInfo"/> to the function parameter.
    /// </summary>
    /// <remarks>Designed for function languages except C# to customize negotiation routing.</remarks>
    [AttributeUsage(AttributeTargets.Parameter)]
    [Binding]
    public class SignalRConnectionInfoListAttribute : Attribute
    {
        public string ConnectionStringSetting { get; set; } = Constants.AzureSignalRConnectionStringName;

        [AutoResolve]
        public string HubName { get; set; }

        [AutoResolve]
        public string UserId { get; set; }

        [AutoResolve]
        public string IdToken { get; set; }

        public string[] ClaimTypeList { get; set; }
    }
}