// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;

namespace Microsoft.Azure.Webjobs.Extensions.SignalRService.E2ETests
{
    public class SimpleChatClient
    {
        private static readonly HttpClient HttpClient = new();

        public static async Task<SignalRConnectionInfo> Negotiate(string userId, string url)
        {
            const string path = "negotiate";
            var request = new HttpRequestMessage(HttpMethod.Post, $"{url}/api/{path}?userid={userId}");
            var responseMessage = await HttpClient.SendAsync(request);

            var connectionInfo = await responseMessage.Content.ReadFromJsonAsync<SignalRConnectionInfo>();
            return connectionInfo;
        }

        public static Task Send(string url, SignalRMessage message)
        {
            const string SendPath = "send";
            var content = JsonContent.Create(message);
            return HttpClient.PostAsync($"{url}/api/{SendPath}", content);
        }

        public static Task Group(string url,SignalRGroupAction action)
        {
            const string GroupPath = "group";
            var content = JsonContent.Create(action);
            return HttpClient.PostAsync($"{url}/api/{GroupPath}", content);
        }
    }
}