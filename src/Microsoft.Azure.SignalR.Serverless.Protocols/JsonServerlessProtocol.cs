// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.SignalR.Serverless.Protocols
{
    public class JsonServerlessProtocol : IServerlessProtocol
    {
        private const string TypePropertyName = "type";

        public int Version => 1;

        public bool TryParseMessage(ref ReadOnlySequence<byte> input, out ServerlessMessage message)
        {
            using (var inputStream = new ReadOnlySequenceStream(input))
            using (var streamReader = new StreamReader(inputStream))
            {
                var textReader = new JsonTextReader(streamReader);

                try
                {
                    var jObject = JObject.Load(textReader);

                    if (jObject.TryGetValue(TypePropertyName, StringComparison.OrdinalIgnoreCase, out var token))
                        switch (token.Value<int>())
                        {
                            case ServerlessProtocolConstants.InvocationMessageType:
                                message = jObject.ToObject<InvocationMessage>();
                                break;
                            case ServerlessProtocolConstants.OpenConnectionMessageType:
                                message = jObject.ToObject<OpenConnectionMessage>();
                                break;
                            case ServerlessProtocolConstants.CloseConnectionMessageType:
                                message = jObject.ToObject<CloseConnectionMessage>();
                                break;
                            default:
                                message = null;
                                break;
                        }
                    else
                        message = null;
                }
                catch
                {
                    message = null;
                }
                finally
                {
                    textReader.Close();
                }

                return message != null;   
            }
        }
    }
}