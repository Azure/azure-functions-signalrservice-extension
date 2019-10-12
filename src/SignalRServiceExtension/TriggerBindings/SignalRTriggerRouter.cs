using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    public class SignalRTriggerRouter
    {
        private readonly Dictionary<string, Dictionary<string, SignalRListener>> _listeners = new Dictionary<string, Dictionary<string, SignalRListener>>(StringComparer.OrdinalIgnoreCase);

        internal void AddListener((string hubName, string methodName) key, SignalRListener listener)
        {
            if (!_listeners.TryGetValue(key.hubName, out var dic))
            {
                dic = new Dictionary<string, SignalRListener>(StringComparer.OrdinalIgnoreCase);
                _listeners.Add(key.hubName, dic);
            }
            dic.Add(key.methodName, listener);
        }

        public async Task<HttpResponseMessage> ProcessAsync(HttpRequestMessage req)
        {
            var path = req.RequestUri.AbsolutePath;
            if (TryGetHubName(path, out var hubName))
            {
                if (_listeners.TryGetValue(hubName, out var hubListener))
                {
                    InvocationContext invocationContext;
                    try
                    {
                        var body = await req.Content.ReadAsStringAsync();
                        invocationContext = JsonConvert.DeserializeObject<InvocationContext>(body);
                    }
                    catch (Exception)
                    {
                        return new HttpResponseMessage(HttpStatusCode.BadRequest);
                    }

                    if (string.IsNullOrEmpty(invocationContext.Data?.Target))
                    {
                        return new HttpResponseMessage(HttpStatusCode.BadRequest);
                    }

                    if (hubListener.TryGetValue(invocationContext.Data?.Target, out var listener))
                    {
                        invocationContext.HubName = hubName;
                        var signalRTriggerEvent = new SignalRTriggerEvent
                        {
                            Context = invocationContext,
                        };

                        // TODO: select out listener that match the pattern

                        var result = await listener.Executor.TryExecuteAsync(new Host.Executors.TriggeredFunctionData
                        {
                            TriggerValue = signalRTriggerEvent
                        }, CancellationToken.None);

                        // TODO: Support invokeAsync later
                        return new HttpResponseMessage(HttpStatusCode.OK);
                    }
                    else
                    {
                        return new HttpResponseMessage(HttpStatusCode.NotFound);
                    }
                }
                else
                {
                    return new HttpResponseMessage(HttpStatusCode.NotFound);
                }
            }
            else
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }
        }

        private bool TryGetHubName(string path, out string hubName)
        {
            // The url should be /runtime/webhooks/signalr/{hub}
            var paths = path.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (!ValidateNegotiateUri(paths))
            {
                hubName = null;
                return false;
            }

            hubName = paths[3];
            return true;
        }

        private bool ValidateNegotiateUri(string[] paths)
        {
            if (paths.Length != 4)
            {
                return false;
            }

            if (paths[2] != "signalr")
            {
                return false;
            }

            return true;
        }
    }
}
