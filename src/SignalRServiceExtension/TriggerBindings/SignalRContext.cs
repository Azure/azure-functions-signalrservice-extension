using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    public class SignalRContext
    {
        /// <summary>
        /// The Url to redirect
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Hub name
        /// </summary>
        public string HubName { get; set; }

        /// <summary>
        /// User id
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Claims from request context
        /// </summary>
        public Dictionary<string, string> Claims { get; set; }
    }
}
