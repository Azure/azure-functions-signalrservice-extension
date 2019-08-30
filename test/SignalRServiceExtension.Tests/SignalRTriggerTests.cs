using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Security.Claims;
using Xunit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.WebApiCompatShim;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;

namespace SignalRServiceExtension.Tests
{
    public class SignalRTriggerTests
    {
        private readonly JwtSecurityTokenHandler _jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
        private readonly string _userId = "userA";
        private readonly ClaimsPrincipal _userClaims;

        public SignalRTriggerTests()
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, _userId),
                new Claim(ClaimTypes.AuthenticationMethod, "Bearer"),
                new Claim(ClaimTypes.Country, "CH"),
            };

            _userClaims = new ClaimsPrincipal(new ClaimsIdentity(claims));
        }

        [Fact]
        public void GetUserIdFromHeaderTest()
        {
            var httpRequst = new HttpRequestMessage();
            httpRequst.Headers.Add("x-ms-client-principal-name", _userId);
            Assert.Equal(_userId, NegotiateUtils.GetUserId(httpRequst));
        }

        [Fact]
        public void GetUserIdFromClaimsTest()
        {
            var context = GetHttpContext();
            Assert.Equal(_userId, NegotiateUtils.GetUserId(new HttpRequestMessageFeature(context).HttpRequestMessage));
        }

        [Fact]
        public void GetClaimsTest()
        {
            var context = GetHttpContext();
            var claims = NegotiateUtils.GetClaims(new HttpRequestMessageFeature(context).HttpRequestMessage);
            Assert.Equal(new Dictionary<string, string>()
            {
                [ClaimTypes.Country] = "CH",
                [ClaimTypes.AuthenticationMethod] = "Bearer"
            }, claims);
        }

        private HttpContext GetHttpContext()
        {
            var context = new DefaultHttpContext();
            context.User = _userClaims;
            context.Request.Method = "Get";
            return context;
        }
    }
}
