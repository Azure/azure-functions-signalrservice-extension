// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService.Internal
{
    internal static class NullObjectTask
    {
        internal static readonly Task<object> Result = Task.FromResult<object>(null);
    }
}