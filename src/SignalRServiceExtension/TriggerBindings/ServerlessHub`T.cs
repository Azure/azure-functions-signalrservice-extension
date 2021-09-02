using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.SignalR.Management;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    public abstract class ServerlessHub<T> where T : class
    {
        private static readonly Lazy<JwtSecurityTokenHandler> JwtSecurityTokenHandler = new Lazy<JwtSecurityTokenHandler>(() => new JwtSecurityTokenHandler());
        protected ServiceHubContext<T> HubContext { get; }

        public ServerlessHub(ServiceHubContext<T> hubContext)
        {
            HubContext = hubContext;
        }

        public ServerlessHub()
        {
            HubContext = (StaticServiceHubContextStore.Get() as IInternalServiceHubContextStore).GetAsync(GetType(), typeof(T));
            Clients = HubContext.Clients;
            Groups = HubContext.Groups;
            UserGroups = HubContext.UserGroups;
            ClientManager = HubContext?.ClientManager;
        }

        /// <summary>
        /// Gets client endpoint access information object for SignalR hub connections to connect to Azure SignalR Service
        /// </summary>
        protected async ValueTask<SignalRConnectionInfo> NegotiateAsync(NegotiationOptions options)
        {
            var negotiateResponse = await HubContext.NegotiateAsync(options);
            return new SignalRConnectionInfo
            {
                Url = negotiateResponse.Url,
                AccessToken = negotiateResponse.AccessToken
            };
        }

        /// <summary>
        /// Get claim list from a JWT.
        /// </summary>
        protected IList<Claim> GetClaims(string jwt)
        {
            if (jwt.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                jwt = jwt.Substring("Bearer ".Length).Trim();
            }
            return JwtSecurityTokenHandler.Value.ReadJwtToken(jwt).Claims.ToList();
        }

        /// <summary>
        /// Gets an object that can be used to invoke methods on the clients connected to this hub.
        /// </summary>
        public IHubClients<T> Clients { get; }

        /// <summary>
        /// Get the group manager of this hub.
        /// </summary>
        public IGroupManager Groups { get; }

        /// <summary>
        /// Get the user group manager of this hub.
        /// </summary>
        public IUserGroupManager UserGroups { get; }

        /// <summary>
        /// Get the client manager of this hub.
        /// </summary>
        public ClientManager ClientManager { get; }
    }
}
