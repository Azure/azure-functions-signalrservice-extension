// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;
using System;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    public class SignalROutputConverter
    {
        private readonly JObjectToTypeConverter<SignalRMessage> messageConverter;
        private readonly JObjectToTypeConverter<SignalRGroupAction> groupActionConverter;

        public SignalROutputConverter()
        {
            messageConverter = new JObjectToTypeConverter<SignalRMessage>();
            groupActionConverter = new JObjectToTypeConverter<SignalRGroupAction>();
        }

        // We accept multiple output binding types and rely on them to determine rest api actions
        // But in non .NET language, it's not able to convert JObject to different types
        // So need a converter to accurate convert JObject to either SignalRMessage or SignalRGroupAction
        public object ConvertToSignalROutput(object input)
        {
            if (input.GetType() != typeof(JObject))
            {
                return input;
            }
            
            var jobject = input as JObject;

            SignalRMessage message = null;
            if (messageConverter.TryConvert(jobject, out message))
            {
                return message;
            }

            SignalRGroupAction groupAction = null;
            if (groupActionConverter.TryConvert(jobject, out groupAction))
            {
                return groupAction;
            }

            throw new ArgumentException("Unable to convert JObject to valid output binding type, check parameters.");
        }
    }
}
