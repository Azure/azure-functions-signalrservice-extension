// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Xunit;

namespace SignalRServiceExtension.Tests
{
    public class SignalRConnectionInfoAttributeTests
    {
        [Fact]
        public void GetClaims_WithNoUserId_ReturnsEmptyClaims()
        {
            var attr = new SignalRConnectionInfoAttribute
            {
                UserId = null
            };

            var claims = attr.GetClaims();

            Assert.Empty(claims);
        }

        [Fact]
        public void GetClaims_WithUserId_ReturnsUserIdInClaims()
        {
            var attr = new SignalRConnectionInfoAttribute
            {
                UserId = "foo"
            };

            var claims = attr.GetClaims();

            Assert.Contains(claims, c => c.Type == ClaimTypes.NameIdentifier && c.Value == "foo");
        }
    }
}