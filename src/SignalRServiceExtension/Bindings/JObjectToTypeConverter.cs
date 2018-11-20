// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    public class JObjectToTypeConverter<TOutput> where TOutput : class
    {
        public bool TryConvert(JObject input, out TOutput output)
        {
            try
            {
                output = JsonConvert.DeserializeObject<TOutput>(input.ToString());
            }
            catch (Exception)
            {
                output = null;
                return false;
            }

            return true;
        }
    }
}
